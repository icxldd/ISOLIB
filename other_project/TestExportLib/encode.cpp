#include "pch.h"
#include "encode.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include <process.h>

// 加密算法相关常量定义
#define BUFFER_SIZE 4096                   // 标准缓冲区大小
#define MAGIC_HEADER "ENCV1.0"             // 加密文件魔数头标识
#define MAGIC_HEADER_SIZE 7                // 魔数头大小
#define CHUNK_SIZE 1024                    // 数据块大小
#define MAX_THREADS 4                      // 最大线程数量
#define DEFAULT_KEY_LENGTH 256             // 默认最大密钥长度

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

// ========== 双密钥系统全局变量 ==========
static unsigned char* g_privateKey = nullptr;
static int g_privateKeyLength = 0;
static CRITICAL_SECTION g_keySection;
static bool g_keyInitialized = false;

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

// 优化的流式文件加密函数（支持双密钥系统和复杂位旋转）
int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	int combinedKeyLength = 0;

	const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB大缓冲区用于高性能处理

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
	fseek(inputFile, 0, SEEK_END);
	long totalFileSize = ftell(inputFile);
	fseek(inputFile, 0, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 分配大缓冲区用于高性能处理
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 写入魔数头用于标识加密文件
	fwrite(MAGIC_HEADER, 1, MAGIC_HEADER_SIZE, outputFile);
	fwrite(&combinedKeyLength, sizeof(int), 1, outputFile);

	// 初始进度回调通知
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 大块处理文件以实现复杂加密的最高速度
	size_t bytesRead;
	long totalProcessed = 0;

	while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 高效双层XOR + 半字节交换加密算法（替代位旋转）
		for (size_t i = 0; i < bytesRead; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = buffer[i];                                    // 原始字节
			unsigned char a2 = a1 ^ keyByte;                                 // 第一次XOR
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);    // 半字节交换
			unsigned char a4 = a3 ^ keyByte;                                 // 第二次XOR
			buffer[i] = a4;                                                  // 最终加密结果
		}

		// 立即写入加密数据
		size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
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

		unsigned int checksum = CalculateChecksum(combinedKey, combinedKeyLength);
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
	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	return result;
}

// 优化的流式文件解密函数（支持双密钥系统和复杂位旋转）
int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	unsigned char* combinedKey = NULL;
	int result = SUCCESS;
	char header[MAGIC_HEADER_SIZE + 1];
	int storedKeyLength = 0;
	int combinedKeyLength = 0;

	const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB大缓冲区

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

	// 验证密钥长度（早期验证）
	if (storedKeyLength != combinedKeyLength) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_DECRYPTION_FAILED;
	}

	// *** 新增：立即验证校验和，避免大文件错误解密 ***
	// 保存当前文件位置
	long currentPos = ftell(inputFile);

	// 跳到文件末尾读取校验和
	fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		unsigned int calculatedChecksum = CalculateChecksum(combinedKey, combinedKeyLength);
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
	fseek(inputFile, currentPos, SEEK_SET);

	// 打开输出文件
	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		free(combinedKey);
		return ERR_FILE_OPEN_FAILED;
	}

	// 分配大缓冲区
	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		free(combinedKey);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 获取文件大小并计算数据区大小
	fseek(inputFile, 0, SEEK_END);
	long fileSize = ftell(inputFile);
	fseek(inputFile, MAGIC_HEADER_SIZE + sizeof(int), SEEK_SET);
	long dataSize = fileSize - MAGIC_HEADER_SIZE - sizeof(int) - sizeof(unsigned int);

	// 初始进度回调通知
	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 大块处理文件以实现复杂解密的最高速度
	size_t bytesRead;
	long totalProcessed = 0;

	while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 处理包含校验和的最后数据块
		if (totalProcessed + bytesRead >= dataSize) {
			bytesRead = dataSize - totalProcessed;
			if (bytesRead <= 0) break;
		}

		// 高效双层XOR + 半字节交换解密算法（与加密算法相同，自逆操作）
		for (size_t i = 0; i < bytesRead; i++) {
			unsigned char keyByte = combinedKey[i % combinedKeyLength];
			unsigned char a1 = buffer[i];                                    // 加密字节
			unsigned char a2 = a1 ^ keyByte;                                 // 第一次XOR（逆向第二次XOR）
			unsigned char a3 = ((a2 & 0x0F) << 4) | ((a2 & 0xF0) >> 4);    // 半字节交换（自逆操作）
			unsigned char a4 = a3 ^ keyByte;                                 // 第二次XOR（逆向第一次XOR）
			buffer[i] = a4;                                                  // 最终解密结果
		}

		// 立即写入解密数据
		size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
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

	// Validate key length (early validation)
	if (storedKeyLength != combinedKeyLength) {
		fclose(inputFile);
		free(combinedKey);
		return 0; // Invalid
	}

	// Validate checksum at the end of file
	fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
		unsigned int calculatedChecksum = CalculateChecksum(combinedKey, combinedKeyLength);
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
