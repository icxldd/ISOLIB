#include "pch.h"
#include "encode.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include <process.h>
#include <wincrypt.h>

// 加密算法相关常量定义
#define BUFFER_SIZE 4096                   // 标准缓冲区大小
#define MAGIC_HEADER "ENCV1.0"             // 加密文件魔数头标识
#define MAGIC_HEADER_SIZE 7                // 魔数头大小
#define CHUNK_SIZE 1024                    // 数据块大小
#define MAX_THREADS 30                     // 最大线程数量（修改为16）
#define DEFAULT_KEY_LENGTH 256             // 默认最大密钥长度
#define MIN_THREAD_COUNT 1                 // 最小线程数量
#define DEFAULT_THREAD_COUNT 10             // 默认线程数量
#define MIN_CHUNK_SIZE_FOR_THREADING (1024 * 1024)  // 最小启用多线程的块大小（1MB）

// 函数执行结果状态码
#define SUCCESS 0                          // 执行成功
#define ERR_FILE_OPEN_FAILED -1           // 文件打开失败
#define ERR_MEMORY_ALLOCATION_FAILED -2   // 内存分配失败
#define ERR_ENCRYPTION_FAILED -3          // 加密操作失败
#define ERR_DECRYPTION_FAILED -4          // 解密操作失败
#define ERR_INVALID_HEADER -5             // 无效文件头
#define ERR_THREAD_CREATION_FAILED -6     // 线程创建失败
#define ERR_INVALID_PARAMETER -7          // 无效参数
#define ERR_PRIVATE_KEY_NOT_SET -8        // 私钥未设置

// ========== 多线程相关数据结构 ==========

// 线程工作数据结构
typedef struct {
	unsigned char* inputBuffer;        // 输入数据缓冲区
	unsigned char* outputBuffer;       // 输出数据缓冲区
	size_t dataSize;                   // 数据块大小
	__int64 globalOffset;              // 在整个文件中的全局偏移量
	unsigned char* combinedKey;        // 组合密钥
	int combinedKeyLength;             // 组合密钥长度
	int threadIndex;                   // 线程索引
	int result;                        // 处理结果
	HANDLE completionEvent;            // 完成事件
} ThreadWorkData;

// 多线程处理上下文
typedef struct {
	ThreadWorkData* workData;          // 工作数据数组
	HANDLE* threadHandles;             // 线程句柄数组
	int threadCount;                   // 实际使用的线程数量
	size_t totalSize;                  // 总数据大小
	bool isEncryption;                 // 是否为加密操作（true=加密，false=解密）
} MultiThreadContext;

// ========== 多线程配置全局变量 ==========
static int g_threadCount = DEFAULT_THREAD_COUNT;
static CRITICAL_SECTION g_threadSection;
static bool g_threadInitialized = false;
static bool g_multiThreadingEnabled = true;  // 默认启用多线程（已修复问题）

// ========== 双密钥系统全局变量 ==========
static unsigned char* g_privateKey = nullptr;
static int g_privateKeyLength = 0;
static CRITICAL_SECTION g_keySection;
static bool g_keyInitialized = false;

// ========== 多线程配置函数实现 ==========

// 设置线程数量
int SetThreadCount(int threadCount) {
	// 参数验证
	if (threadCount < MIN_THREAD_COUNT || threadCount > MAX_THREADS) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化线程临界区（只初始化一次）
	if (!g_threadInitialized) {
		InitializeCriticalSection(&g_threadSection);
		g_threadInitialized = true;
	}

	EnterCriticalSection(&g_threadSection);
	g_threadCount = threadCount;
	LeaveCriticalSection(&g_threadSection);

	return SUCCESS;
}

// 获取当前线程数量
int GetThreadCount() {
	if (!g_threadInitialized) {
		return DEFAULT_THREAD_COUNT;
	}

	EnterCriticalSection(&g_threadSection);
	int count = g_threadCount;
	LeaveCriticalSection(&g_threadSection);

	return count;
}

// 检查是否应该使用多线程处理
bool ShouldUseMultiThreading(size_t dataSize, int threadCount) {
	return (dataSize >= MIN_CHUNK_SIZE_FOR_THREADING && threadCount > 1 && g_multiThreadingEnabled);
}

// ========== 多线程处理核心函数 ==========

// 线程工作函数 - 执行加密或解密操作
DWORD WINAPI ThreadWorkerFunction(LPVOID lpParam) {
	ThreadWorkData* workData = (ThreadWorkData*)lpParam;
	if (!workData) {
		return ERR_INVALID_PARAMETER;
	}

	// 执行加密/解密操作
	for (size_t i = 0; i < workData->dataSize; i++) {
		// 使用全局位置计算密钥索引，确保与单线程版本一致
		__int64 globalIndex = workData->globalOffset + i;
		unsigned char keyByte = workData->combinedKey[globalIndex % workData->combinedKeyLength];

		// 双层XOR + 半字节交换算法（加密和解密使用相同算法，因为是自逆操作）
		unsigned char a1 = workData->inputBuffer[i];
		unsigned char a2 = a1 ^ keyByte;
		unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
		unsigned char a4 = a3 ^ keyByte;
		workData->outputBuffer[i] = a4;
	}

	// 设置成功状态
	workData->result = SUCCESS;

	return SUCCESS;
}

// 初始化多线程上下文
int InitializeMultiThreadContext(MultiThreadContext* context, size_t totalSize, int threadCount, bool isEncryption) {
	if (!context || totalSize == 0 || threadCount <= 0) {
		return ERR_INVALID_PARAMETER;
	}

	memset(context, 0, sizeof(MultiThreadContext));

	context->totalSize = totalSize;
	context->isEncryption = isEncryption;
	context->threadCount = threadCount;

	// 分配工作数据数组
	context->workData = (ThreadWorkData*)calloc(threadCount, sizeof(ThreadWorkData));
	if (!context->workData) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 分配线程句柄数组
	context->threadHandles = (HANDLE*)calloc(threadCount, sizeof(HANDLE));
	if (!context->threadHandles) {
		free(context->workData);
		context->workData = nullptr;
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 初始化工作数据
	for (int i = 0; i < threadCount; i++) {
		context->workData[i].completionEvent = NULL; // 不再使用事件
		context->workData[i].result = ERR_THREAD_CREATION_FAILED;
	}

	return SUCCESS;
}

// 清理多线程上下文
void CleanupMultiThreadContext(MultiThreadContext* context) {
	if (!context) return;

	// 线程句柄已在ExecuteMultiThreadOperation中清理，这里只需清理数组
	if (context->threadHandles) {
		free(context->threadHandles);
		context->threadHandles = nullptr;
	}

	// 清理工作数据
	if (context->workData) {
		// 不再需要清理事件，因为我们不再使用它们
		free(context->workData);
		context->workData = nullptr;
	}

	memset(context, 0, sizeof(MultiThreadContext));
}

// 执行多线程加密/解密操作
int ExecuteMultiThreadOperation(MultiThreadContext* context, unsigned char* inputData, unsigned char* outputData, unsigned char* combinedKey, int combinedKeyLength) {
	if (!context || !inputData || !outputData || !combinedKey || combinedKeyLength <= 0) {
		return ERR_INVALID_PARAMETER;
	}

	// 计算每个线程处理的数据块大小
	size_t chunkSize = context->totalSize / context->threadCount;
	size_t remainder = context->totalSize % context->threadCount;

	// 分配工作任务并创建线程
	__int64 currentOffset = 0;
	int actualThreadCount = 0;

	for (int i = 0; i < context->threadCount; i++) {
		ThreadWorkData* workData = &context->workData[i];

		// 计算当前线程的数据大小（最后一个线程处理剩余数据）
		size_t currentChunkSize = chunkSize;
		if (i == context->threadCount - 1) {
			currentChunkSize += remainder;
		}

		// 如果没有数据需要处理，跳过这个线程
		if (currentChunkSize == 0) {
			break;
		}

		// 设置工作数据
		workData->inputBuffer = inputData + currentOffset;
		workData->outputBuffer = outputData + currentOffset;
		workData->dataSize = currentChunkSize;
		workData->globalOffset = currentOffset;
		workData->combinedKey = combinedKey;
		workData->combinedKeyLength = combinedKeyLength;
		workData->threadIndex = i;
		workData->result = ERR_THREAD_CREATION_FAILED; // 初始状态

		// 创建线程
		context->threadHandles[i] = CreateThread(
			NULL,                           // 默认安全属性
			0,                              // 默认堆栈大小
			ThreadWorkerFunction,           // 线程函数
			workData,                       // 传递给线程的参数
			0,                              // 默认创建标志
			NULL                            // 不需要线程ID
		);

		if (!context->threadHandles[i]) {
			// 线程创建失败，等待已创建的线程完成
			for (int j = 0; j < actualThreadCount; j++) {
				if (context->threadHandles[j]) {
					WaitForSingleObject(context->threadHandles[j], INFINITE);
					CloseHandle(context->threadHandles[j]);
					context->threadHandles[j] = NULL;
				}
			}
			return ERR_THREAD_CREATION_FAILED;
		}

		actualThreadCount++;
		currentOffset += currentChunkSize;
	}

	// 简化的等待逻辑：直接等待所有线程完成
	for (int i = 0; i < actualThreadCount; i++) {
		if (context->threadHandles[i]) {
			DWORD waitResult = WaitForSingleObject(context->threadHandles[i], INFINITE);
			if (waitResult != WAIT_OBJECT_0) {
				// 等待失败，清理剩余线程
				for (int j = i; j < actualThreadCount; j++) {
					if (context->threadHandles[j]) {
						TerminateThread(context->threadHandles[j], 1);
						CloseHandle(context->threadHandles[j]);
						context->threadHandles[j] = NULL;
					}
				}
				return ERR_THREAD_CREATION_FAILED;
			}
			CloseHandle(context->threadHandles[i]);
			context->threadHandles[i] = NULL;
		}
	}

	// 检查所有线程的执行结果
	for (int i = 0; i < actualThreadCount; i++) {
		if (context->workData[i].result != SUCCESS) {
			return context->workData[i].result;
		}
	}

	return SUCCESS;
}

// ========== 双密钥系统函数实现 ==========

// 初始化私钥
int InitStreamFile(const char* privateKey) {
	if (!privateKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化临界区（只初始化一次）
	if (!g_keyInitialized) {
		InitializeCriticalSection(&g_keySection);
		g_keyInitialized = true;
	}

	// 初始化线程临界区（只初始化一次）
	if (!g_threadInitialized) {
		InitializeCriticalSection(&g_threadSection);
		g_threadInitialized = true;
	}

	EnterCriticalSection(&g_keySection);

	// 清理之前的私钥
	if (g_privateKey) {
		SecureZeroMemory(g_privateKey, g_privateKeyLength);
		free(g_privateKey);
		g_privateKey = nullptr;
		g_privateKeyLength = 0;
	}

	// 存储新私钥（支持任意长度）
	g_privateKeyLength = strlen(privateKey);
	if (g_privateKeyLength == 0) {
		LeaveCriticalSection(&g_keySection);
		return ERR_INVALID_PARAMETER;
	}

	g_privateKey = (unsigned char*)malloc(g_privateKeyLength + 1);
	if (!g_privateKey) {
		g_privateKeyLength = 0;
		LeaveCriticalSection(&g_keySection);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	memcpy(g_privateKey, privateKey, g_privateKeyLength);
	g_privateKey[g_privateKeyLength] = '\0';

	LeaveCriticalSection(&g_keySection);

	return SUCCESS;
}

// 清理私钥
void ClearPrivateKey() {
	if (!g_keyInitialized) return;

	EnterCriticalSection(&g_keySection);

	if (g_privateKey) {
		SecureZeroMemory(g_privateKey, g_privateKeyLength);
		free(g_privateKey);
		g_privateKey = nullptr;
		g_privateKeyLength = 0;
	}

	LeaveCriticalSection(&g_keySection);
}

// 检查私钥是否已设置
int IsPrivateKeySet() {
	if (!g_keyInitialized) return 0;

	EnterCriticalSection(&g_keySection);
	int result = (g_privateKey != nullptr && g_privateKeyLength > 0) ? 1 : 0;
	LeaveCriticalSection(&g_keySection);

	return result;
}

// 组合私钥和公钥生成最终加密密钥
unsigned char* CombineKeys(const unsigned char* publicKey, int* combinedLength) {
	if (!g_privateKey || !publicKey) return nullptr;

	int pubKeyLen = strlen((const char*)publicKey);
	if (pubKeyLen == 0) return nullptr;

	// 使用交错组合算法
	int totalLen = g_privateKeyLength + pubKeyLen;
	unsigned char* combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) return nullptr;

	// 交错组合两个密钥，增强安全性
	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			// 偶数位置使用私钥
			combinedKey[i] = g_privateKey[i / 2 % g_privateKeyLength];
		}
		else {
			// 奇数位置使用公钥
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		// 应用位运算增强混合效果
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}

	combinedKey[totalLen] = '\0';
	*combinedLength = totalLen;

	return combinedKey;
}

// 计算数据校验和用于文件完整性验证
unsigned int CalculateChecksum(const unsigned char* data, size_t length) {
	unsigned int checksum = 0;
	for (size_t i = 0; i < length; i++) {
		checksum = (checksum << 1) ^ data[i];
	}
	return checksum;
}

// 新增：计算更强的CRC32校验和，减少碰撞概率
unsigned int CalculateCRC32(const unsigned char* data, size_t length) {
	unsigned int crc = 0xFFFFFFFF;
	static const unsigned int crc_table[256] = {
		0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F,
		0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988,
		0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 0x1DB71064, 0x6AB020F2,
		0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7,
		0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
		0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172,
		0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C,
		0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
		0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423,
		0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
		0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190, 0x01DB7106,
		0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
		0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D,
		0x91646C97, 0xE6635C01, 0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E,
		0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
		0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
		0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7,
		0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0,
		0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA,
		0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
		0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81,
		0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A,
		0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 0xE3630B12, 0x94643B84,
		0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
		0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
		0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC,
		0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 0xD6D6A3E8, 0xA1D1937E,
		0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B,
		0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55,
		0x316E8EEF, 0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
		0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28,
		0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
		0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F,
		0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38,
		0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
		0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
		0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69,
		0x616BFFD3, 0x166CCF45, 0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2,
		0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC,
		0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
		0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693,
		0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94,
		0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
	};

	for (size_t i = 0; i < length; i++) {
		crc = crc_table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
	}
	return crc ^ 0xFFFFFFFF;
}

// 新增：计算公钥哈希值用于完整性验证
unsigned int CalculatePublicKeyHash(const unsigned char* publicKey) {
	if (!publicKey) return 0;

	size_t keyLen = strlen((const char*)publicKey);
	if (keyLen == 0) return 0;

	// 使用更复杂的哈希算法，结合CRC32和MD5风格的混合
	unsigned int hash1 = CalculateCRC32(publicKey, keyLen);
	unsigned int hash2 = 0;

	// 第二轮哈希：MD5风格的块处理
	for (size_t i = 0; i < keyLen; i++) {
		hash2 = ((hash2 << 5) + hash2) + publicKey[i];
		hash2 ^= (hash2 >> 11);
		hash2 += (hash2 << 3);
		hash2 ^= (hash2 >> 5);
		hash2 += (hash2 << 2);
		hash2 ^= (hash2 >> 15);
		hash2 += (hash2 << 10);
	}

	// 组合两个哈希值
	return hash1 ^ hash2;
}

// 优化的流式文件加密函数（支持双密钥系统和多线程加密）
int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	int combinedKeyLength = 0;
	MultiThreadContext mtContext = { 0 };

	const size_t STREAM_BUFFER_SIZE = 100 * 1024 * 1024;  // 4MB大缓冲区用于高性能处理

	// 检查私钥是否已设置
	if (!IsPrivateKeySet()) {
		return ERR_PRIVATE_KEY_NOT_SET;
	}

	if (!publicKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 进入临界区获取组合密钥
	EnterCriticalSection(&g_keySection);
	combinedKey = CombineKeys(publicKey, &combinedKeyLength);
	LeaveCriticalSection(&g_keySection);

	if (!combinedKey || combinedKeyLength == 0) {
		return ERR_ENCRYPTION_FAILED;
	}

	// 打开输入文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 获取文件大小用于进度计算
	_fseeki64(inputFile, 0, SEEK_END);
	__int64 totalFileSize = _ftelli64(inputFile);
	_fseeki64(inputFile, 0, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 写入魔数头用于标识加密文件
	fwrite(MAGIC_HEADER, 1, MAGIC_HEADER_SIZE, outputFile);
	fwrite(&combinedKeyLength, sizeof(int), 1, outputFile);

	// 新增：写入公钥哈希值用于完整性验证
	unsigned int publicKeyHash = CalculatePublicKeyHash(publicKey);
	fwrite(&publicKeyHash, sizeof(unsigned int), 1, outputFile);

	// 初始进度回调通知
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 分配大缓冲区用于高性能处理（输入和输出缓冲区）
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE * 2);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* inputBuffer = buffer;
	unsigned char* outputBuffer = buffer + STREAM_BUFFER_SIZE;

	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 大块处理文件以实现复杂加密的最高速度
	size_t bytesRead;
	__int64 totalProcessed = 0;

	while ((bytesRead = fread(inputBuffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 判断是否使用多线程处理
		if (ShouldUseMultiThreading(bytesRead, currentThreadCount)) {
			// 使用多线程处理
			result = InitializeMultiThreadContext(&mtContext, bytesRead, currentThreadCount, true);
			if (result == SUCCESS) {
				result = ExecuteMultiThreadOperation(&mtContext, inputBuffer, outputBuffer, combinedKey, combinedKeyLength);
				CleanupMultiThreadContext(&mtContext);
			}

			// 如果多线程失败，回退到单线程处理
			if (result != SUCCESS) {
				for (size_t i = 0; i < bytesRead; i++) {
					__int64 globalIndex = totalProcessed + i;
					unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
					unsigned char a1 = inputBuffer[i];
					unsigned char a2 = a1 ^ keyByte;
					unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
					unsigned char a4 = a3 ^ keyByte;
					outputBuffer[i] = a4;
				}
				result = SUCCESS; // 重置结果状态
			}
		}
		else {
			// 使用单线程处理（小文件或单线程配置）
			for (size_t i = 0; i < bytesRead; i++) {
				// 使用全局位置计算密钥索引，确保与解密一致
				__int64 globalIndex = totalProcessed + i;
				unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
				unsigned char a1 = inputBuffer[i];                                    // 原始字节
				unsigned char a2 = a1 ^ keyByte;                                 // 第一次XOR
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);    // 半字节交换
				unsigned char a4 = a3 ^ keyByte;                                 // 第二次XOR
				outputBuffer[i] = a4;                                                  // 最终加密结果
			}
		}

		// 立即写入加密数据
		size_t bytesWritten = fwrite(outputBuffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_ENCRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		// 进度回调 - 报告基于数据处理的真实进度
		if (progressCallback && totalFileSize > 0) {
			// 计算数据处理进度（0-98%），为写校验和预留2%
			double dataProgress = (double)totalProcessed / (double)totalFileSize;
			double adjustedProgress = dataProgress * 0.98; // 数据处理占98%
			progressCallback(filePath, adjustedProgress);
		}
	}

	// 写入校验和
	if (result == SUCCESS) {
		// 进度回调 - 写校验和阶段（98%-100%）
		if (progressCallback) {
			progressCallback(filePath, 0.99); // 99% - 开始写校验和
		}

		// 使用更强的CRC32校验和替代简单校验和
		unsigned int checksum = CalculateCRC32(combinedKey, combinedKeyLength);
		fwrite(&checksum, sizeof(unsigned int), 1, outputFile);

		// 最终进度回调 - 100%完成
		if (progressCallback) {
			progressCallback(filePath, 1.0);
		}
	}

	// 清理资源
	if (combinedKey) {
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(combinedKey);
	}
	CleanupMultiThreadContext(&mtContext);
	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	return result;
}

// 优化的流式文件解密函数（支持双密钥系统和多线程解密）
int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	char header[MAGIC_HEADER_SIZE + 1];
	int storedKeyLength = 0;
	int combinedKeyLength = 0;
	MultiThreadContext mtContext = { 0 };

	const size_t STREAM_BUFFER_SIZE = 100 * 1024 * 1024;  // 4MB大缓冲区

	// 检查私钥是否已设置
	if (!IsPrivateKeySet()) {
		return ERR_PRIVATE_KEY_NOT_SET;
	}

	if (!publicKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 进入临界区获取组合密钥
	EnterCriticalSection(&g_keySection);
	combinedKey = CombineKeys(publicKey, &combinedKeyLength);
	LeaveCriticalSection(&g_keySection);

	if (!combinedKey || combinedKeyLength == 0) {
		return ERR_DECRYPTION_FAILED;
	}

	// 打开输入文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 读取并验证文件头（早期格式检测）
	if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_INVALID_HEADER;
	}

	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_INVALID_HEADER;
	}

	// 读取存储的密钥长度
	if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_INVALID_HEADER;
	}

	// 新增：读取并验证公钥哈希值
	unsigned int storedPublicKeyHash;
	if (fread(&storedPublicKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_INVALID_HEADER;
	}

	// 新增：早期公钥完整性验证
	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_DECRYPTION_FAILED; // 公钥不匹配
	}

	// Validate key length (early validation)
	if (storedKeyLength != combinedKeyLength) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_DECRYPTION_FAILED;
	}

	// *** 新增：立即验证校验和，避免大文件错误解密 ***
	// 保存当前文件位置
	__int64 currentPos = _ftelli64(inputFile);

	// 跳到文件末尾读取校验和
	_fseeki64(inputFile, -(__int64)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		// 使用更强的CRC32校验和替代简单校验和
		unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
		if (storedChecksum != calculatedChecksum) {
			fclose(inputFile);
			free(combinedKey);
			return ERR_DECRYPTION_FAILED;
		}
	}
	else {
		fclose(inputFile);
		free(combinedKey);
		return ERR_INVALID_HEADER;
	}

	// 恢复文件位置到数据开始处
	_fseeki64(inputFile, currentPos, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 分配大缓冲区（输入和输出缓冲区）
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE * 2);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* inputBuffer = buffer;
	unsigned char* outputBuffer = buffer + STREAM_BUFFER_SIZE;

	// 获取文件大小并计算数据区大小
	_fseeki64(inputFile, 0, SEEK_END);
	__int64 fileSize = _ftelli64(inputFile);
	_fseeki64(inputFile, currentPos, SEEK_SET);
	__int64 dataSize = fileSize - currentPos - sizeof(unsigned int);

	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 初始进度回调通知
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 大块处理文件以实现复杂解密的最高速度
	size_t bytesRead;
	__int64 totalProcessed = 0;

	while ((bytesRead = fread(inputBuffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 处理包含校验和的最后数据块
		if (totalProcessed + bytesRead >= dataSize) {
			bytesRead = dataSize - totalProcessed;
			if (bytesRead <= 0) break;
		}

		// 判断是否使用多线程处理
		if (ShouldUseMultiThreading(bytesRead, currentThreadCount)) {
			// 使用多线程处理
			result = InitializeMultiThreadContext(&mtContext, bytesRead, currentThreadCount, false);
			if (result == SUCCESS) {
				result = ExecuteMultiThreadOperation(&mtContext, inputBuffer, outputBuffer, combinedKey, combinedKeyLength);
				CleanupMultiThreadContext(&mtContext);
			}

			// 如果多线程失败，回退到单线程处理
			if (result != SUCCESS) {
				for (size_t i = 0; i < bytesRead; i++) {
					__int64 globalIndex = totalProcessed + i;
					unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
					unsigned char a1 = inputBuffer[i];
					unsigned char a2 = a1 ^ keyByte;
					unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
					unsigned char a4 = a3 ^ keyByte;
					outputBuffer[i] = a4;
				}
				result = SUCCESS; // 重置结果状态
			}
		}
		else {
			// 使用单线程处理（小文件或单线程配置）
			for (size_t i = 0; i < bytesRead; i++) {
				// 使用全局位置计算密钥索引，确保与StreamEncryptFile一致
				__int64 globalIndex = totalProcessed + i;
				unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
				unsigned char a1 = inputBuffer[i];                                    // 加密字节
				unsigned char a2 = a1 ^ keyByte;                                 // 第一次XOR（逆向第二次XOR）
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);    // 半字节交换（自逆操作）
				unsigned char a4 = a3 ^ keyByte;                                 // 第二次XOR（逆向第一次XOR）
				outputBuffer[i] = a4;                                                  // 最终解密结果
			}
		}

		// 立即写入解密数据
		size_t bytesWritten = fwrite(outputBuffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_DECRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		// 进度回调 - 报告基于数据处理的真实进度
		if (progressCallback && dataSize > 0) {
			// 现在数据处理占100%，因为校验和已在开头验证
			double dataProgress = (double)totalProcessed / (double)dataSize;
			progressCallback(filePath, dataProgress);
		}
	}

	// 解密完成后的最终处理
	if (result == SUCCESS) {
		// 最终进度回调 - 100%完成
		if (progressCallback) {
			progressCallback(filePath, 1.0);
		}
	}

	// 清理资源
	if (combinedKey) {
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(combinedKey);
	}
	CleanupMultiThreadContext(&mtContext);
	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	if (result != SUCCESS) {
		remove(outputPath);  // 如果解密失败则删除输出文件
	}

	return result;
}

int ValidateEncryptedFile(const char* filePath, const unsigned char* publicKey) {
	FILE* inputFile = NULL;
	unsigned char* combinedKey = NULL;
	char header[MAGIC_HEADER_SIZE + 1];
	int storedKeyLength = 0;
	int combinedKeyLength = 0;
	bool isValid = false;

	// 检查私钥是否已设置
	if (!IsPrivateKeySet()) {
		return 0; // Invalid
	}

	if (!publicKey) {
		return 0; // Invalid
	}

	// 进入临界区获取组合密钥
	EnterCriticalSection(&g_keySection);
	combinedKey = CombineKeys(publicKey, &combinedKeyLength);
	LeaveCriticalSection(&g_keySection);

	if (!combinedKey || combinedKeyLength == 0) {
		return 0; // Invalid
	}

	// Open input file
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		free(combinedKey);
		return 0; // Invalid
	}

	// Read and validate header (early format detection)
	if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	// Read stored key length
	if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	// 新增：读取并验证公钥哈希值
	unsigned int storedPublicKeyHash;
	if (fread(&storedPublicKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	// 新增：早期公钥完整性验证
	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid - 公钥不匹配
	}

	// Validate key length (early validation)
	if (storedKeyLength != combinedKeyLength) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	// Validate checksum at the end of file
	_fseeki64(inputFile, -(__int64)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		// 使用更强的CRC32校验和替代简单校验和
		unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
		if (storedChecksum == calculatedChecksum) {
			isValid = true;
		}
	}

	// Clean up
	if (combinedKey) {
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(combinedKey);
	}
	fclose(inputFile);

	return isValid ? 1 : 0;
}

// 新增：字节数组加密函数（双密钥系统）
int StreamEncryptData(const unsigned char* inputData, size_t inputLength, const unsigned char* publicKey, unsigned char** outputData, size_t* outputLength) {
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	int combinedKeyLength = 0;

	// 检查输入参数
	if (!inputData || inputLength == 0 || !publicKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化输出参数
	*outputData = NULL;
	*outputLength = 0;

	// 检查私钥是否已设置
	if (!IsPrivateKeySet()) {
		return ERR_PRIVATE_KEY_NOT_SET;
	}

	// 进入临界区获取组合密钥
	EnterCriticalSection(&g_keySection);
	combinedKey = CombineKeys(publicKey, &combinedKeyLength);
	LeaveCriticalSection(&g_keySection);

	if (!combinedKey || combinedKeyLength == 0) {
		return ERR_ENCRYPTION_FAILED;
	}

	// 计算输出数据大小：魔数头 + 密钥长度 + 公钥哈希 + 原始数据 + CRC32校验和
	size_t headerSize = MAGIC_HEADER_SIZE + sizeof(int) + sizeof(unsigned int);
	size_t outputSize = headerSize + inputLength + sizeof(unsigned int);

	// 分配输出缓冲区
	*outputData = (unsigned char*)malloc(outputSize);
	if (!*outputData) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* outPtr = *outputData;

	// 写入魔数头
	memcpy(outPtr, MAGIC_HEADER, MAGIC_HEADER_SIZE);
	outPtr += MAGIC_HEADER_SIZE;

	// 写入组合密钥长度
	memcpy(outPtr, &combinedKeyLength, sizeof(int));
	outPtr += sizeof(int);

	// 写入公钥哈希值
	unsigned int publicKeyHash = CalculatePublicKeyHash(publicKey);
	memcpy(outPtr, &publicKeyHash, sizeof(unsigned int));
	outPtr += sizeof(unsigned int);

	// 加密数据（支持多线程）
	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 判断是否使用多线程处理
	if (ShouldUseMultiThreading(inputLength, currentThreadCount)) {
		// 使用多线程处理
		MultiThreadContext mtContext = { 0 };
		int result = InitializeMultiThreadContext(&mtContext, inputLength, currentThreadCount, true);
		if (result == SUCCESS) {
			result = ExecuteMultiThreadOperation(&mtContext, (unsigned char*)inputData, outPtr, combinedKey, combinedKeyLength);
			CleanupMultiThreadContext(&mtContext);
		}

		if (result != SUCCESS) {
			// 多线程失败，回退到单线程处理
			for (size_t i = 0; i < inputLength; i++) {
				unsigned char keyByte = combinedKey[i % combinedKeyLength];
				unsigned char a1 = inputData[i];                                    // 原始字节
				unsigned char a2 = a1 ^ keyByte;                                    // 第一次XOR
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);       // 半字节交换
				unsigned char a4 = a3 ^ keyByte;                                    // 第二次XOR
				outPtr[i] = a4;                                                     // 最终加密结果
			}
		}
	}
	else {
		// 使用单线程处理（小数据或单线程配置）
		for (size_t i = 0; i < inputLength; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = inputData[i];                                    // 原始字节
			unsigned char a2 = a1 ^ keyByte;                                    // 第一次XOR
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);       // 半字节交换
			unsigned char a4 = a3 ^ keyByte;                                    // 第二次XOR
			outPtr[i] = a4;                                                     // 最终加密结果
		}
	}
	outPtr += inputLength;

	// 写入CRC32校验和
	unsigned int checksum = CalculateCRC32(combinedKey, combinedKeyLength);
	memcpy(outPtr, &checksum, sizeof(unsigned int));

	*outputLength = outputSize;

	// 清理资源
	if (combinedKey) {
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(combinedKey);
	}

	return SUCCESS;
}

// 新增：字节数组解密函数（双密钥系统）
int StreamDecryptData(const unsigned char* inputData, size_t inputLength, const unsigned char* publicKey, unsigned char** outputData, size_t* outputLength) {
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	char header[MAGIC_HEADER_SIZE + 1];
	int storedKeyLength = 0;
	int combinedKeyLength = 0;

	// 检查输入参数
	if (!inputData || inputLength == 0 || !publicKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化输出参数
	*outputData = NULL;
	*outputLength = 0;

	// 检查数据最小长度
	size_t minSize = MAGIC_HEADER_SIZE + sizeof(int) + sizeof(unsigned int) + sizeof(unsigned int);
	if (inputLength < minSize) {
		return ERR_INVALID_HEADER;
	}

	// 检查私钥是否已设置
	if (!IsPrivateKeySet()) {
		return ERR_PRIVATE_KEY_NOT_SET;
	}

	// 进入临界区获取组合密钥
	EnterCriticalSection(&g_keySection);
	combinedKey = CombineKeys(publicKey, &combinedKeyLength);
	LeaveCriticalSection(&g_keySection);

	if (!combinedKey || combinedKeyLength == 0) {
		return ERR_DECRYPTION_FAILED;
	}

	const unsigned char* inPtr = inputData;

	// 验证魔数头
	memcpy(header, inPtr, MAGIC_HEADER_SIZE);
	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_INVALID_HEADER;
	}
	inPtr += MAGIC_HEADER_SIZE;

	// 读取存储的密钥长度
	memcpy(&storedKeyLength, inPtr, sizeof(int));
	inPtr += sizeof(int);

	// 读取并验证公钥哈希值
	unsigned int storedPublicKeyHash;
	memcpy(&storedPublicKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	// 验证公钥完整性
	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_DECRYPTION_FAILED; // 公钥不匹配
	}

	// 验证密钥长度
	if (storedKeyLength != combinedKeyLength) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_DECRYPTION_FAILED;
	}

	// 计算数据区大小
	size_t headerSize = MAGIC_HEADER_SIZE + sizeof(int) + sizeof(unsigned int);
	size_t dataSize = inputLength - headerSize - sizeof(unsigned int);

	// 验证校验和（从末尾读取）
	unsigned int storedChecksum;
	memcpy(&storedChecksum, inputData + inputLength - sizeof(unsigned int), sizeof(unsigned int));
	unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
	if (storedChecksum != calculatedChecksum) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_DECRYPTION_FAILED;
	}

	// 分配输出缓冲区
	*outputData = (unsigned char*)malloc(dataSize);
	if (!*outputData) {
		if (combinedKey) {
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(combinedKey);
		}
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 解密数据（支持多线程）
	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 判断是否使用多线程处理
	if (ShouldUseMultiThreading(dataSize, currentThreadCount)) {
		// 使用多线程处理
		MultiThreadContext mtContext = { 0 };
		int result = InitializeMultiThreadContext(&mtContext, dataSize, currentThreadCount, false);
		if (result == SUCCESS) {
			result = ExecuteMultiThreadOperation(&mtContext, (unsigned char*)inPtr, *outputData, combinedKey, combinedKeyLength);
			CleanupMultiThreadContext(&mtContext);
		}

		if (result != SUCCESS) {
			// 多线程失败，回退到单线程处理
			for (size_t i = 0; i < dataSize; i++) {
				unsigned char keyByte = combinedKey[i % combinedKeyLength];
				unsigned char a1 = inPtr[i];                                        // 加密字节
				unsigned char a2 = a1 ^ keyByte;                                    // 第一次XOR（逆向第二次XOR）
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);       // 半字节交换（自逆操作）
				unsigned char a4 = a3 ^ keyByte;                                    // 第二次XOR（逆向第一次XOR）
				(*outputData)[i] = a4;                                              // 最终解密结果
			}
		}
	}
	else {
		// 使用单线程处理（小数据或单线程配置）
		for (size_t i = 0; i < dataSize; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = inPtr[i];                                        // 加密字节
			unsigned char a2 = a1 ^ keyByte;                                    // 第一次XOR（逆向第二次XOR）
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);       // 半字节交换（自逆操作）
			unsigned char a4 = a3 ^ keyByte;                                    // 第二次XOR（逆向第一次XOR）
			(*outputData)[i] = a4;                                              // 最终解密结果
		}
	}

	*outputLength = dataSize;

	// 清理资源
	if (combinedKey) {
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(combinedKey);
	}

	return SUCCESS;
}

// 新增：释放加密数据内存
void FreeEncryptedData(unsigned char* data) {
	if (data) {
		free(data);
	}
}

// 新增：释放解密数据内存
void FreeDecryptedData(unsigned char* data) {
	if (data) {
		free(data);
	}
}

// ========== 自包含式加密/解密系统实现 ==========

#define SELF_CONTAINED_MAGIC_HEADER "SELFV1.0"     // 自包含式加密文件魔数头
#define SELF_CONTAINED_MAGIC_SIZE 8                // 自包含式魔数头大小
#define PRIVATE_KEY_SIZE_2048_BITS 256             // 2048位私钥大小（256字节）

// 生成2048位随机私钥
int Generate2048BitPrivateKey(unsigned char* privateKey, int* keyLength) {
	if (!privateKey || !keyLength) {
		return ERR_INVALID_PARAMETER;
	}

	// 使用Windows CryptoAPI生成高质量随机数
	HCRYPTPROV hProv = 0;
	if (!CryptAcquireContext(&hProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT)) {
		return ERR_ENCRYPTION_FAILED;
	}

	// 生成256字节（2048位）的随机私钥
	if (!CryptGenRandom(hProv, PRIVATE_KEY_SIZE_2048_BITS, privateKey)) {
		CryptReleaseContext(hProv, 0);
		return ERR_ENCRYPTION_FAILED;
	}

	CryptReleaseContext(hProv, 0);
	*keyLength = PRIVATE_KEY_SIZE_2048_BITS;

	return SUCCESS;
}

// 计算私钥哈希值用于完整性验证
unsigned int CalculatePrivateKeyHash(const unsigned char* privateKey, int keyLength) {
	if (!privateKey || keyLength <= 0) {
		return 0;
	}

	// 使用双重哈希确保私钥完整性
	unsigned int hash1 = CalculateCRC32(privateKey, keyLength);
	unsigned int hash2 = 0;

	// 第二轮哈希：增强安全性
	for (int i = 0; i < keyLength; i++) {
		hash2 = ((hash2 << 7) + hash2) + privateKey[i];
		hash2 ^= (hash2 >> 13);
		hash2 += (hash2 << 5);
		hash2 ^= (hash2 >> 7);
		hash2 += (hash2 << 3);
		hash2 ^= (hash2 >> 17);
		hash2 += (hash2 << 11);
	}

	// 组合两个哈希值，增加复杂度
	return hash1 ^ hash2 ^ 0xABCDEF01;
}

// 自包含式文件加密函数
int SelfContainedEncryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* privateKey = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	int privateKeyLength = 0;
	int combinedKeyLength = 0;

	const size_t STREAM_BUFFER_SIZE = 100 * 1024 * 1024;  // 4MB大缓冲区

	if (!filePath || !outputPath || !publicKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 分配私钥缓冲区
	privateKey = (unsigned char*)malloc(PRIVATE_KEY_SIZE_2048_BITS);
	if (!privateKey) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 生成2048位随机私钥
	result = Generate2048BitPrivateKey(privateKey, &privateKeyLength);
	if (result != SUCCESS) {
		free(privateKey);
		return result;
	}

	// 使用生成的私钥组合公钥
	int pubKeyLen = strlen((const char*)publicKey);
	if (pubKeyLen == 0) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_INVALID_PARAMETER;
	}

	// 组合私钥和公钥（使用相同的交错算法）
	int totalLen = privateKeyLength + pubKeyLen;
	combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 交错组合两个密钥
	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			combinedKey[i] = privateKey[i / 2 % privateKeyLength];
		}
		else {
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}
	combinedKey[totalLen] = '\0';
	combinedKeyLength = totalLen;

	// 打开输入文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 获取文件大小
	_fseeki64(inputFile, 0, SEEK_END);
	__int64 totalFileSize = _ftelli64(inputFile);
	_fseeki64(inputFile, 0, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 分配缓冲区
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 写入自包含式文件头
	fwrite(SELF_CONTAINED_MAGIC_HEADER, 1, SELF_CONTAINED_MAGIC_SIZE, outputFile);
	fwrite(&combinedKeyLength, sizeof(int), 1, outputFile);

	// 写入公钥哈希值
	unsigned int publicKeyHash = CalculatePublicKeyHash(publicKey);
	fwrite(&publicKeyHash, sizeof(unsigned int), 1, outputFile);

	// 写入私钥长度和私钥数据
	fwrite(&privateKeyLength, sizeof(int), 1, outputFile);
	fwrite(privateKey, 1, privateKeyLength, outputFile);

	// 写入私钥哈希值用于完整性验证
	unsigned int privateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	fwrite(&privateKeyHash, sizeof(unsigned int), 1, outputFile);

	// 进度回调初始化
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 重新分配缓冲区支持多线程（输入和输出缓冲区）
	free(buffer);
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE * 2);
	if (!buffer) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		fclose(outputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* inputBuffer = buffer;
	unsigned char* outputBuffer = buffer + STREAM_BUFFER_SIZE;

	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();
	MultiThreadContext mtContext = { 0 };

	// 加密文件数据
	size_t bytesRead;
	__int64 totalProcessed = 0;

	while ((bytesRead = fread(inputBuffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 判断是否使用多线程处理
		if (ShouldUseMultiThreading(bytesRead, currentThreadCount)) {
			// 使用多线程处理
			result = InitializeMultiThreadContext(&mtContext, bytesRead, currentThreadCount, true);
			if (result == SUCCESS) {
				result = ExecuteMultiThreadOperation(&mtContext, inputBuffer, outputBuffer, combinedKey, combinedKeyLength);
				CleanupMultiThreadContext(&mtContext);
			}

			// 如果多线程失败，回退到单线程处理
			if (result != SUCCESS) {
				for (size_t i = 0; i < bytesRead; i++) {
					__int64 globalIndex = totalProcessed + i;
					unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
					unsigned char a1 = inputBuffer[i];
					unsigned char a2 = a1 ^ keyByte;
					unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
					unsigned char a4 = a3 ^ keyByte;
					outputBuffer[i] = a4;
				}
				result = SUCCESS; // 重置结果状态
			}
		}
		else {
			// 使用单线程处理
			for (size_t i = 0; i < bytesRead; i++) {
				// 使用全局位置计算密钥索引，确保一致性
				__int64 globalIndex = totalProcessed + i;
				unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
				unsigned char a1 = inputBuffer[i];
				unsigned char a2 = a1 ^ keyByte;
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
				unsigned char a4 = a3 ^ keyByte;
				outputBuffer[i] = a4;
			}
		}

		size_t bytesWritten = fwrite(outputBuffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_ENCRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		// 进度回调
		if (progressCallback && totalFileSize > 0) {
			double dataProgress = (double)totalProcessed / (double)totalFileSize;
			double adjustedProgress = dataProgress * 0.98;
			progressCallback(filePath, adjustedProgress);
		}
	}

	// 清理多线程上下文
	CleanupMultiThreadContext(&mtContext);

	// 写入校验和
	if (result == SUCCESS) {
		if (progressCallback) {
			progressCallback(filePath, 0.99);
		}

		unsigned int checksum = CalculateCRC32(combinedKey, combinedKeyLength);
		fwrite(&checksum, sizeof(unsigned int), 1, outputFile);

		if (progressCallback) {
			progressCallback(filePath, 1.0);
		}
	}

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	SecureZeroMemory(combinedKey, combinedKeyLength);
	free(privateKey);
	free(combinedKey);
	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	return result;
}

// 自包含式文件解密函数
int SelfContainedDecryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* privateKey = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	char header[SELF_CONTAINED_MAGIC_SIZE + 1];
	int storedCombinedKeyLength = 0;
	int privateKeyLength = 0;
	int combinedKeyLength = 0;

	const size_t STREAM_BUFFER_SIZE = 100 * 1024 * 1024;

	if (!filePath || !outputPath || !publicKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 打开输入文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return ERR_FILE_OPEN_FAILED;
	}

	// 读取并验证文件头
	if (fread(header, 1, SELF_CONTAINED_MAGIC_SIZE, inputFile) != SELF_CONTAINED_MAGIC_SIZE) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	header[SELF_CONTAINED_MAGIC_SIZE] = '\0';
	if (strcmp(header, SELF_CONTAINED_MAGIC_HEADER) != 0) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取组合密钥长度
	if (fread(&storedCombinedKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取并验证公钥哈希值
	unsigned int storedPublicKeyHash;
	if (fread(&storedPublicKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED; // 公钥不匹配
	}

	// 读取私钥长度
	if (fread(&privateKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	if (privateKeyLength != PRIVATE_KEY_SIZE_2048_BITS) {
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥
	privateKey = (unsigned char*)malloc(privateKeyLength);
	if (!privateKey) {
		fclose(inputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	if (fread(privateKey, 1, privateKeyLength, inputFile) != privateKeyLength) {
		free(privateKey);
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取并验证私钥哈希值
	unsigned int storedPrivateKeyHash;
	if (fread(&storedPrivateKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	unsigned int currentPrivateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	if (storedPrivateKeyHash != currentPrivateKeyHash) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED; // 私钥被篡改
	}

	// 重新组合私钥和公钥
	int pubKeyLen = strlen((const char*)publicKey);
	int totalLen = privateKeyLength + pubKeyLen;
	combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			combinedKey[i] = privateKey[i / 2 % privateKeyLength];
		}
		else {
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}
	combinedKey[totalLen] = '\0';
	combinedKeyLength = totalLen;

	// 验证组合密钥长度
	if (storedCombinedKeyLength != combinedKeyLength) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED;
	}

	// 立即验证校验和
	__int64 currentPos = _ftelli64(inputFile);
	_fseeki64(inputFile, -(__int64)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
		if (storedChecksum != calculatedChecksum) {
			SecureZeroMemory(privateKey, privateKeyLength);
			SecureZeroMemory(combinedKey, combinedKeyLength);
			free(privateKey);
			free(combinedKey);
			fclose(inputFile);
			return ERR_DECRYPTION_FAILED;
		}
	}
	else {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 恢复文件位置到数据开始处
	_fseeki64(inputFile, currentPos, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		return ERR_FILE_OPEN_FAILED;
	}

	// 分配缓冲区
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		fclose(outputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 计算数据区大小
	_fseeki64(inputFile, 0, SEEK_END);
	__int64 fileSize = _ftelli64(inputFile);
	_fseeki64(inputFile, currentPos, SEEK_SET);
	__int64 dataSize = fileSize - currentPos - sizeof(unsigned int);

	// 进度回调初始化
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 重新分配缓冲区支持多线程（输入和输出缓冲区）
	free(buffer);
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE * 2);
	if (!buffer) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		fclose(outputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* inputBuffer = buffer;
	unsigned char* outputBuffer = buffer + STREAM_BUFFER_SIZE;

	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();
	MultiThreadContext mtContext = { 0 };

	// 解密文件数据
	size_t bytesRead;
	__int64 totalProcessed = 0;

	while ((bytesRead = fread(inputBuffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 处理包含校验和的最后数据块
		if (totalProcessed + bytesRead >= dataSize) {
			bytesRead = dataSize - totalProcessed;
			if (bytesRead <= 0) break;
		}

		// 判断是否使用多线程处理
		if (ShouldUseMultiThreading(bytesRead, currentThreadCount)) {
			// 使用多线程处理
			result = InitializeMultiThreadContext(&mtContext, bytesRead, currentThreadCount, false);
			if (result == SUCCESS) {
				result = ExecuteMultiThreadOperation(&mtContext, inputBuffer, outputBuffer, combinedKey, combinedKeyLength);
				CleanupMultiThreadContext(&mtContext);
			}

			// 如果多线程失败，回退到单线程处理
			if (result != SUCCESS) {
				for (size_t i = 0; i < bytesRead; i++) {
					__int64 globalIndex = totalProcessed + i;
					unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
					unsigned char a1 = inputBuffer[i];
					unsigned char a2 = a1 ^ keyByte;
					unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
					unsigned char a4 = a3 ^ keyByte;
					outputBuffer[i] = a4;
				}
				result = SUCCESS; // 重置结果状态
			}
		}
		else {
			// 使用单线程处理
			for (size_t i = 0; i < bytesRead; i++) {
				// 使用全局位置计算密钥索引，确保一致性
				__int64 globalIndex = totalProcessed + i;
				unsigned char keyByte = combinedKey[globalIndex % combinedKeyLength];
				unsigned char a1 = inputBuffer[i];
				unsigned char a2 = a1 ^ keyByte;
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
				unsigned char a4 = a3 ^ keyByte;
				outputBuffer[i] = a4;
			}
		}

		size_t bytesWritten = fwrite(outputBuffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_DECRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		// 进度回调
		if (progressCallback && dataSize > 0) {
			double dataProgress = (double)totalProcessed / (double)dataSize;
			progressCallback(filePath, dataProgress);
		}
	}

	// 清理多线程上下文
	CleanupMultiThreadContext(&mtContext);

	// 解密完成
	if (result == SUCCESS) {
		if (progressCallback) {
			progressCallback(filePath, 1.0);
		}
	}

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	SecureZeroMemory(combinedKey, combinedKeyLength);
	free(privateKey);
	free(combinedKey);
	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	if (result != SUCCESS) {
		remove(outputPath);
	}

	return result;
}

// 自包含式数据加密函数
int SelfContainedEncryptData(const unsigned char* inputData, size_t inputLength, const unsigned char* publicKey, unsigned char** outputData, size_t* outputLength) {
	unsigned char* privateKey = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	int privateKeyLength = 0;
	int combinedKeyLength = 0;

	if (!inputData || inputLength == 0 || !publicKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	*outputData = NULL;
	*outputLength = 0;

	// 分配私钥缓冲区并生成私钥
	privateKey = (unsigned char*)malloc(PRIVATE_KEY_SIZE_2048_BITS);
	if (!privateKey) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	result = Generate2048BitPrivateKey(privateKey, &privateKeyLength);
	if (result != SUCCESS) {
		free(privateKey);
		return result;
	}

	// 组合私钥和公钥
	int pubKeyLen = strlen((const char*)publicKey);
	if (pubKeyLen == 0) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_INVALID_PARAMETER;
	}

	int totalLen = privateKeyLength + pubKeyLen;
	combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			combinedKey[i] = privateKey[i / 2 % privateKeyLength];
		}
		else {
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}
	combinedKey[totalLen] = '\0';
	combinedKeyLength = totalLen;

	// 计算输出大小
	size_t headerSize = SELF_CONTAINED_MAGIC_SIZE + sizeof(int) + sizeof(unsigned int) + sizeof(int) + privateKeyLength + sizeof(unsigned int);
	size_t outputSize = headerSize + inputLength + sizeof(unsigned int);

	// 分配输出缓冲区
	*outputData = (unsigned char*)malloc(outputSize);
	if (!*outputData) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* outPtr = *outputData;

	// 写入文件头
	memcpy(outPtr, SELF_CONTAINED_MAGIC_HEADER, SELF_CONTAINED_MAGIC_SIZE);
	outPtr += SELF_CONTAINED_MAGIC_SIZE;

	memcpy(outPtr, &combinedKeyLength, sizeof(int));
	outPtr += sizeof(int);

	unsigned int publicKeyHash = CalculatePublicKeyHash(publicKey);
	memcpy(outPtr, &publicKeyHash, sizeof(unsigned int));
	outPtr += sizeof(unsigned int);

	memcpy(outPtr, &privateKeyLength, sizeof(int));
	outPtr += sizeof(int);

	memcpy(outPtr, privateKey, privateKeyLength);
	outPtr += privateKeyLength;

	unsigned int privateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	memcpy(outPtr, &privateKeyHash, sizeof(unsigned int));
	outPtr += sizeof(unsigned int);

	// 加密数据（支持多线程）
	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 判断是否使用多线程处理
	if (ShouldUseMultiThreading(inputLength, currentThreadCount)) {
		// 使用多线程处理
		MultiThreadContext mtContext = { 0 };
		int mtResult = InitializeMultiThreadContext(&mtContext, inputLength, currentThreadCount, true);
		if (mtResult == SUCCESS) {
			mtResult = ExecuteMultiThreadOperation(&mtContext, (unsigned char*)inputData, outPtr, combinedKey, combinedKeyLength);
			CleanupMultiThreadContext(&mtContext);
		}

		if (mtResult != SUCCESS) {
			// 多线程失败，回退到单线程处理
			for (size_t i = 0; i < inputLength; i++) {
				unsigned char keyByte = combinedKey[i % combinedKeyLength];
				unsigned char a1 = inputData[i];
				unsigned char a2 = a1 ^ keyByte;
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
				unsigned char a4 = a3 ^ keyByte;
				outPtr[i] = a4;
			}
		}
	}
	else {
		// 使用单线程处理（小数据或单线程配置）
		for (size_t i = 0; i < inputLength; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = inputData[i];
			unsigned char a2 = a1 ^ keyByte;
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
			unsigned char a4 = a3 ^ keyByte;
			outPtr[i] = a4;
		}
	}
	outPtr += inputLength;

	// 写入校验和
	unsigned int checksum = CalculateCRC32(combinedKey, combinedKeyLength);
	memcpy(outPtr, &checksum, sizeof(unsigned int));

	*outputLength = outputSize;

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	SecureZeroMemory(combinedKey, combinedKeyLength);
	free(privateKey);
	free(combinedKey);

	return SUCCESS;
}

// 自包含式数据解密函数
int SelfContainedDecryptData(const unsigned char* inputData, size_t inputLength, const unsigned char* publicKey, unsigned char** outputData, size_t* outputLength) {
	unsigned char* privateKey = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	char header[SELF_CONTAINED_MAGIC_SIZE + 1];
	int storedCombinedKeyLength = 0;
	int privateKeyLength = 0;
	int combinedKeyLength = 0;

	if (!inputData || inputLength == 0 || !publicKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	*outputData = NULL;
	*outputLength = 0;

	// 检查最小长度
	size_t minSize = SELF_CONTAINED_MAGIC_SIZE + sizeof(int) + sizeof(unsigned int) + sizeof(int) + PRIVATE_KEY_SIZE_2048_BITS + sizeof(unsigned int) + sizeof(unsigned int);
	if (inputLength < minSize) {
		return ERR_INVALID_HEADER;
	}

	const unsigned char* inPtr = inputData;

	// 验证文件头
	memcpy(header, inPtr, SELF_CONTAINED_MAGIC_SIZE);
	header[SELF_CONTAINED_MAGIC_SIZE] = '\0';
	if (strcmp(header, SELF_CONTAINED_MAGIC_HEADER) != 0) {
		return ERR_INVALID_HEADER;
	}
	inPtr += SELF_CONTAINED_MAGIC_SIZE;

	// 读取组合密钥长度
	memcpy(&storedCombinedKeyLength, inPtr, sizeof(int));
	inPtr += sizeof(int);

	// 验证公钥哈希
	unsigned int storedPublicKeyHash;
	memcpy(&storedPublicKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥长度
	memcpy(&privateKeyLength, inPtr, sizeof(int));
	inPtr += sizeof(int);

	if (privateKeyLength != PRIVATE_KEY_SIZE_2048_BITS) {
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥
	privateKey = (unsigned char*)malloc(privateKeyLength);
	if (!privateKey) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	memcpy(privateKey, inPtr, privateKeyLength);
	inPtr += privateKeyLength;

	// 验证私钥哈希
	unsigned int storedPrivateKeyHash;
	memcpy(&storedPrivateKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	unsigned int currentPrivateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	if (storedPrivateKeyHash != currentPrivateKeyHash) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_DECRYPTION_FAILED;
	}

	// 重新组合密钥
	int pubKeyLen = strlen((const char*)publicKey);
	int totalLen = privateKeyLength + pubKeyLen;
	combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			combinedKey[i] = privateKey[i / 2 % privateKeyLength];
		}
		else {
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}
	combinedKey[totalLen] = '\0';
	combinedKeyLength = totalLen;

	// 验证组合密钥长度
	if (storedCombinedKeyLength != combinedKeyLength) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_DECRYPTION_FAILED;
	}

	// 验证校验和
	unsigned int storedChecksum;
	memcpy(&storedChecksum, inputData + inputLength - sizeof(unsigned int), sizeof(unsigned int));
	unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
	if (storedChecksum != calculatedChecksum) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_DECRYPTION_FAILED;
	}

	// 计算数据大小
	size_t headerSize = SELF_CONTAINED_MAGIC_SIZE + sizeof(int) + sizeof(unsigned int) + sizeof(int) + privateKeyLength + sizeof(unsigned int);
	size_t dataSize = inputLength - headerSize - sizeof(unsigned int);

	// 分配输出缓冲区
	*outputData = (unsigned char*)malloc(dataSize);
	if (!*outputData) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 解密数据（支持多线程）
	// 获取当前线程配置
	int currentThreadCount = GetThreadCount();

	// 判断是否使用多线程处理
	if (ShouldUseMultiThreading(dataSize, currentThreadCount)) {
		// 使用多线程处理
		MultiThreadContext mtContext = { 0 };
		int mtResult = InitializeMultiThreadContext(&mtContext, dataSize, currentThreadCount, false);
		if (mtResult == SUCCESS) {
			mtResult = ExecuteMultiThreadOperation(&mtContext, (unsigned char*)inPtr, *outputData, combinedKey, combinedKeyLength);
			CleanupMultiThreadContext(&mtContext);
		}

		if (mtResult != SUCCESS) {
			// 多线程失败，回退到单线程处理
			for (size_t i = 0; i < dataSize; i++) {
				unsigned char keyByte = combinedKey[i % combinedKeyLength];
				unsigned char a1 = inPtr[i];
				unsigned char a2 = a1 ^ keyByte;
				unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
				unsigned char a4 = a3 ^ keyByte;
				(*outputData)[i] = a4;
			}
		}
	}
	else {
		// 使用单线程处理（小数据或单线程配置）
		for (size_t i = 0; i < dataSize; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = inPtr[i];
			unsigned char a2 = a1 ^ keyByte;
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);
			unsigned char a4 = a3 ^ keyByte;
			(*outputData)[i] = a4;
		}
	}

	*outputLength = dataSize;

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	SecureZeroMemory(combinedKey, combinedKeyLength);
	free(privateKey);
	free(combinedKey);

	return SUCCESS;
}

// 验证自包含式加密文件有效性
int ValidateSelfContainedFile(const char* filePath, const unsigned char* publicKey) {
	FILE* inputFile = NULL;
	unsigned char* privateKey = NULL;
	unsigned char* combinedKey = NULL;
	char header[SELF_CONTAINED_MAGIC_SIZE + 1];
	int storedCombinedKeyLength = 0;
	int privateKeyLength = 0;
	int combinedKeyLength = 0;
	bool isValid = false;

	if (!filePath || !publicKey) {
		return 0;
	}

	// 打开文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return 0;
	}

	// 验证文件头
	if (fread(header, 1, SELF_CONTAINED_MAGIC_SIZE, inputFile) != SELF_CONTAINED_MAGIC_SIZE) {
		fclose(inputFile);
		return 0;
	}

	header[SELF_CONTAINED_MAGIC_SIZE] = '\0';
	if (strcmp(header, SELF_CONTAINED_MAGIC_HEADER) != 0) {
		fclose(inputFile);
		return 0;
	}

	// 读取组合密钥长度
	if (fread(&storedCombinedKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return 0;
	}

	// 验证公钥哈希
	unsigned int storedPublicKeyHash;
	if (fread(&storedPublicKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return 0;
	}

	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		fclose(inputFile);
		return 0;
	}

	// 读取私钥长度
	if (fread(&privateKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return 0;
	}

	if (privateKeyLength != PRIVATE_KEY_SIZE_2048_BITS) {
		fclose(inputFile);
		return 0;
	}

	// 读取私钥
	privateKey = (unsigned char*)malloc(privateKeyLength);
	if (!privateKey) {
		fclose(inputFile);
		return 0;
	}

	if (fread(privateKey, 1, privateKeyLength, inputFile) != privateKeyLength) {
		free(privateKey);
		fclose(inputFile);
		return 0;
	}

	// 验证私钥哈希
	unsigned int storedPrivateKeyHash;
	if (fread(&storedPrivateKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return 0;
	}

	unsigned int currentPrivateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	if (storedPrivateKeyHash != currentPrivateKeyHash) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return 0;
	}

	// 重新组合密钥
	int pubKeyLen = strlen((const char*)publicKey);
	int totalLen = privateKeyLength + pubKeyLen;
	combinedKey = (unsigned char*)malloc(totalLen + 1);
	if (!combinedKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return 0;
	}

	for (int i = 0; i < totalLen; i++) {
		if (i % 2 == 0) {
			combinedKey[i] = privateKey[i / 2 % privateKeyLength];
		}
		else {
			combinedKey[i] = publicKey[i / 2 % pubKeyLen];
		}
		combinedKey[i] ^= (unsigned char)(i * 7 + 13);
	}
	combinedKey[totalLen] = '\0';
	combinedKeyLength = totalLen;

	// 验证组合密钥长度
	if (storedCombinedKeyLength != combinedKeyLength) {
		SecureZeroMemory(privateKey, privateKeyLength);
		SecureZeroMemory(combinedKey, combinedKeyLength);
		free(privateKey);
		free(combinedKey);
		fclose(inputFile);
		return 0;
	}

	// 验证校验和
	_fseeki64(inputFile, -(__int64)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		unsigned int calculatedChecksum = CalculateCRC32(combinedKey, combinedKeyLength);
		if (storedChecksum == calculatedChecksum) {
			isValid = true;
		}
	}

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	SecureZeroMemory(combinedKey, combinedKeyLength);
	free(privateKey);
	free(combinedKey);
	fclose(inputFile);

	return isValid ? 1 : 0;
}

// ========== 私钥提取函数实现 ==========

// 从自包含式加密文件中提取私钥
int ExtractPrivateKeyFromFile(const char* filePath, const unsigned char* publicKey, char** extractedPrivateKey) {
	FILE* inputFile = NULL;
	unsigned char* privateKey = NULL;
	char header[SELF_CONTAINED_MAGIC_SIZE + 1];
	int storedCombinedKeyLength = 0;
	int privateKeyLength = 0;

	if (!filePath || !publicKey || !extractedPrivateKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化输出参数
	*extractedPrivateKey = NULL;

	// 打开输入文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return ERR_FILE_OPEN_FAILED;
	}

	// 读取并验证文件头
	if (fread(header, 1, SELF_CONTAINED_MAGIC_SIZE, inputFile) != SELF_CONTAINED_MAGIC_SIZE) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	header[SELF_CONTAINED_MAGIC_SIZE] = '\0';
	if (strcmp(header, SELF_CONTAINED_MAGIC_HEADER) != 0) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取组合密钥长度
	if (fread(&storedCombinedKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取并验证公钥哈希值
	unsigned int storedPublicKeyHash;
	if (fread(&storedPublicKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED; // 公钥不匹配
	}

	// 读取私钥长度
	if (fread(&privateKeyLength, sizeof(int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	if (privateKeyLength != PRIVATE_KEY_SIZE_2048_BITS) {
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥
	privateKey = (unsigned char*)malloc(privateKeyLength);
	if (!privateKey) {
		fclose(inputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	if (fread(privateKey, 1, privateKeyLength, inputFile) != privateKeyLength) {
		free(privateKey);
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取并验证私钥哈希值
	unsigned int storedPrivateKeyHash;
	if (fread(&storedPrivateKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	unsigned int currentPrivateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	if (storedPrivateKeyHash != currentPrivateKeyHash) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_DECRYPTION_FAILED; // 私钥被篡改
	}

	// 提取私钥为十六进制字符串
	*extractedPrivateKey = (char*)malloc(privateKeyLength * 2 + 1);
	if (!*extractedPrivateKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		fclose(inputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 将二进制私钥转换为十六进制字符串
	for (int i = 0; i < privateKeyLength; i++) {
		sprintf_s(*extractedPrivateKey + i * 2, 3, "%02X", privateKey[i]);
	}
	(*extractedPrivateKey)[privateKeyLength * 2] = '\0';

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	free(privateKey);
	fclose(inputFile);

	return SUCCESS;
}

// 从自包含式加密数据中提取私钥
int ExtractPrivateKeyFromData(const unsigned char* inputData, size_t inputLength, const unsigned char* publicKey, char** extractedPrivateKey) {
	unsigned char* privateKey = NULL;
	char header[SELF_CONTAINED_MAGIC_SIZE + 1];
	int storedCombinedKeyLength = 0;
	int privateKeyLength = 0;

	if (!inputData || inputLength == 0 || !publicKey || !extractedPrivateKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 初始化输出参数
	*extractedPrivateKey = NULL;

	// 检查最小长度
	size_t minSize = SELF_CONTAINED_MAGIC_SIZE + sizeof(int) + sizeof(unsigned int) + sizeof(int) + PRIVATE_KEY_SIZE_2048_BITS + sizeof(unsigned int);
	if (inputLength < minSize) {
		return ERR_INVALID_HEADER;
	}

	const unsigned char* inPtr = inputData;

	// 验证文件头
	memcpy(header, inPtr, SELF_CONTAINED_MAGIC_SIZE);
	header[SELF_CONTAINED_MAGIC_SIZE] = '\0';
	if (strcmp(header, SELF_CONTAINED_MAGIC_HEADER) != 0) {
		return ERR_INVALID_HEADER;
	}
	inPtr += SELF_CONTAINED_MAGIC_SIZE;

	// 读取组合密钥长度
	memcpy(&storedCombinedKeyLength, inPtr, sizeof(int));
	inPtr += sizeof(int);

	// 验证公钥哈希
	unsigned int storedPublicKeyHash;
	memcpy(&storedPublicKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	unsigned int currentPublicKeyHash = CalculatePublicKeyHash(publicKey);
	if (storedPublicKeyHash != currentPublicKeyHash) {
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥长度
	memcpy(&privateKeyLength, inPtr, sizeof(int));
	inPtr += sizeof(int);

	if (privateKeyLength != PRIVATE_KEY_SIZE_2048_BITS) {
		return ERR_DECRYPTION_FAILED;
	}

	// 读取私钥
	privateKey = (unsigned char*)malloc(privateKeyLength);
	if (!privateKey) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	memcpy(privateKey, inPtr, privateKeyLength);
	inPtr += privateKeyLength;

	// 验证私钥哈希
	unsigned int storedPrivateKeyHash;
	memcpy(&storedPrivateKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	unsigned int currentPrivateKeyHash = CalculatePrivateKeyHash(privateKey, privateKeyLength);
	if (storedPrivateKeyHash != currentPrivateKeyHash) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_DECRYPTION_FAILED;
	}

	// 提取私钥为十六进制字符串
	*extractedPrivateKey = (char*)malloc(privateKeyLength * 2 + 1);
	if (!*extractedPrivateKey) {
		SecureZeroMemory(privateKey, privateKeyLength);
		free(privateKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 将二进制私钥转换为十六进制字符串
	for (int i = 0; i < privateKeyLength; i++) {
		sprintf_s(*extractedPrivateKey + i * 2, 3, "%02X", privateKey[i]);
	}
	(*extractedPrivateKey)[privateKeyLength * 2] = '\0';

	// 清理资源
	SecureZeroMemory(privateKey, privateKeyLength);
	free(privateKey);

	return SUCCESS;
}

// ========== 库清理函数 ==========

// 清理库资源（在程序退出或DLL卸载时调用）
 void CleanupEncryptionLibrary() {
	// 清理私钥
	ClearPrivateKey();

	// 清理私钥临界区
	if (g_keyInitialized) {
		DeleteCriticalSection(&g_keySection);
		g_keyInitialized = false;
	}

	// 清理线程临界区和重置多线程状态
	if (g_threadInitialized) {
		DeleteCriticalSection(&g_threadSection);
		g_threadInitialized = false;
		g_multiThreadingEnabled = true; // 重置为默认启用状态
	}
}
