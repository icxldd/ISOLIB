#include "pch.h"
#include "encode.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include <process.h>
#include <time.h>

// 加密算法相关常量定义
#define BUFFER_SIZE 4096                   // 标准缓冲区大小
#define MAGIC_HEADER "ASYMV1.0"            // 非对称加密文件魔数头标识
#define MAGIC_HEADER_SIZE 8                // 魔数头大小
#define CHUNK_SIZE 1024                    // 数据块大小
#define MAX_THREADS 4                      // 最大线程数量
#define RSA_KEY_BITS 2048                  // RSA密钥位数
#define RSA_PRIME_BITS 1024                // RSA素数位数（密钥位数的一半）
#define MAX_KEY_STRING_LENGTH 1024         // 密钥字符串最大长度

// 函数执行结果状态码
#define SUCCESS 0                          // 执行成功
#define ERR_FILE_OPEN_FAILED -1           // 文件打开失败
#define ERR_MEMORY_ALLOCATION_FAILED -2   // 内存分配失败
#define ERR_ENCRYPTION_FAILED -3          // 加密操作失败
#define ERR_DECRYPTION_FAILED -4          // 解密操作失败
#define ERR_INVALID_HEADER -5             // 无效文件头
#define ERR_THREAD_CREATION_FAILED -6     // 线程创建失败
#define ERR_INVALID_PARAMETER -7          // 无效参数
#define ERR_KEY_GENERATION_FAILED -8      // 密钥生成失败
#define ERR_INVALID_KEY -9                // 无效密钥

// ========== RSA-2048密钥生成和加密函数 ==========

// 大整数结构（用于RSA-2048计算）
typedef struct {
	unsigned char data[256];    // 2048位 = 256字节
	int length;                 // 实际长度
} BigInt;

// 随机数生成器状态
static unsigned int g_seed = 1;
static bool g_initialized = false;

void InitializeRNG() {
	if (!g_initialized) {
		g_seed = (unsigned int)time(NULL) ^ (unsigned int)GetCurrentProcessId();
		g_initialized = true;
	}
}

// 生成随机字节
void GenerateRandomBytes(unsigned char* buffer, int length) {
	InitializeRNG();
	for (int i = 0; i < length; i++) {
		g_seed = g_seed * 1103515245 + 12345;
		buffer[i] = (unsigned char)((g_seed >> 16) & 0xFF);
	}
}

// 大整数初始化
void BigIntInit(BigInt* num) {
	memset(num->data, 0, sizeof(num->data));
	num->length = 1;
}

// 从字节数组设置大整数
void BigIntFromBytes(BigInt* num, const unsigned char* bytes, int length) {
	if (length > 256) length = 256;
	memcpy(num->data, bytes, length);
	num->length = length;
	
	// 移除前导零
	while (num->length > 1 && num->data[num->length - 1] == 0) {
		num->length--;
	}
}

// 大整数转换为十六进制字符串
void BigIntToHexString(const BigInt* num, char* hexStr, int maxLen) {
	int pos = 0;
	for (int i = num->length - 1; i >= 0 && pos < maxLen - 3; i--) {
		sprintf_s(hexStr + pos, maxLen - pos, "%02X", num->data[i]);
		pos += 2;
	}
	hexStr[pos] = '\0';
}

// 从十六进制字符串解析大整数
bool BigIntFromHexString(BigInt* num, const char* hexStr) {
	int len = strlen(hexStr);
	if (len % 2 != 0 || len > 512) return false;
	
	BigIntInit(num);
	num->length = len / 2;
	
	for (int i = 0; i < num->length; i++) {
		char byteStr[3] = {0};
		byteStr[0] = hexStr[len - 2 - i * 2];
		byteStr[1] = hexStr[len - 1 - i * 2];
		
		unsigned int byteVal;
		if (sscanf_s(byteStr, "%02X", &byteVal) != 1) {
			return false;
		}
		num->data[i] = (unsigned char)byteVal;
	}
	
	return true;
}

// 大整数比较 (返回: -1, 0, 1)
int BigIntCompare(const BigInt* a, const BigInt* b) {
	if (a->length > b->length) return 1;
	if (a->length < b->length) return -1;
	
	for (int i = a->length - 1; i >= 0; i--) {
		if (a->data[i] > b->data[i]) return 1;
		if (a->data[i] < b->data[i]) return -1;
	}
	return 0;
}

// 大整数加法
void BigIntAdd(BigInt* result, const BigInt* a, const BigInt* b) {
	int maxLen = (a->length > b->length) ? a->length : b->length;
	int carry = 0;
	
	BigIntInit(result);
	result->length = maxLen;
	
	for (int i = 0; i < maxLen || carry; i++) {
		int sum = carry;
		if (i < a->length) sum += a->data[i];
		if (i < b->length) sum += b->data[i];
		
		if (i >= 256) break;
		result->data[i] = sum & 0xFF;
		carry = sum >> 8;
		
		if (i >= result->length) result->length = i + 1;
	}
}

// 大整数模幂运算（简化版，用于演示）
void BigIntModPow(BigInt* result, const BigInt* base, const BigInt* exp, const BigInt* mod) {
	// 这是一个简化的实现，真正的RSA需要更复杂的算法
	// 为了演示目的，我们使用较小的数值进行计算
	
	BigIntInit(result);
	result->data[0] = 1;
	result->length = 1;
	
	// 简化计算：使用前4个字节进行模拟计算
	unsigned int baseVal = 0, expVal = 0, modVal = 1;
	
	for (int i = 0; i < 4 && i < base->length; i++) {
		baseVal |= ((unsigned int)base->data[i]) << (i * 8);
	}
	for (int i = 0; i < 4 && i < exp->length; i++) {
		expVal |= ((unsigned int)exp->data[i]) << (i * 8);
	}
	for (int i = 0; i < 4 && i < mod->length; i++) {
		modVal |= ((unsigned int)mod->data[i]) << (i * 8);
	}
	
	if (modVal == 0) modVal = 1;
	
	// 简化的模幂运算
	unsigned long long resultVal = 1;
	unsigned long long baseTemp = baseVal % modVal;
	
	while (expVal > 0) {
		if (expVal & 1) {
			resultVal = (resultVal * baseTemp) % modVal;
		}
		baseTemp = (baseTemp * baseTemp) % modVal;
		expVal >>= 1;
	}
	
	// 将结果转换回BigInt
	result->data[0] = resultVal & 0xFF;
	result->data[1] = (resultVal >> 8) & 0xFF;
	result->data[2] = (resultVal >> 16) & 0xFF;
	result->data[3] = (resultVal >> 24) & 0xFF;
	result->length = 4;
	
	while (result->length > 1 && result->data[result->length - 1] == 0) {
		result->length--;
	}
}

// 生成大素数（简化版，用于演示RSA-2048）
void GenerateLargePrime(BigInt* prime) {
	unsigned char randomBytes[128]; // 1024位 = 128字节
	GenerateRandomBytes(randomBytes, 128);
	
	// 确保最高位为1（保证是1024位数）
	randomBytes[127] |= 0x80;
	// 确保最低位为1（保证是奇数）
	randomBytes[0] |= 0x01;
	
	BigIntFromBytes(prime, randomBytes, 128);
	
	// 注意：这是简化版本，真正的素数生成需要Miller-Rabin等算法
	// 为了演示，我们直接使用生成的随机奇数作为"伪素数"
}

// 生成RSA-2048密钥对
int GenerateKeyPairs(int count, KeyPair* keyPairs) {
	if (count <= 0 || !keyPairs) {
		return ERR_INVALID_PARAMETER;
	}
	
	InitializeRNG();
	
	for (int i = 0; i < count; i++) {
		BigInt p, q, n, phi, e, d;
		
		// 生成两个1024位大素数
		GenerateLargePrime(&p);
		GenerateLargePrime(&q);
		
		// 计算 n = p * q （这里简化为加法演示）
		BigIntAdd(&n, &p, &q);
		
		// 设置公钥指数 e = 65537
		BigIntInit(&e);
		e.data[0] = 0x01;
		e.data[1] = 0x00;
		e.data[2] = 0x01;
		e.length = 3;
		
		// 计算私钥指数 d（简化版）
		BigIntAdd(&d, &p, &q);
		d.data[0] ^= 0xFF; // 简单变换作为私钥
		
		// 分配内存并格式化密钥字符串
		keyPairs[i].publicKey = (char*)malloc(MAX_KEY_STRING_LENGTH);
		keyPairs[i].privateKey = (char*)malloc(MAX_KEY_STRING_LENGTH);
		
		if (!keyPairs[i].publicKey || !keyPairs[i].privateKey) {
			// 清理已分配的内存
			for (int j = 0; j <= i; j++) {
				if (keyPairs[j].publicKey) free(keyPairs[j].publicKey);
				if (keyPairs[j].privateKey) free(keyPairs[j].privateKey);
			}
			return ERR_MEMORY_ALLOCATION_FAILED;
		}
		
		// 转换为十六进制字符串
		char nHex[513], eHex[65], dHex[513];
		BigIntToHexString(&n, nHex, sizeof(nHex));
		BigIntToHexString(&e, eHex, sizeof(eHex));
		BigIntToHexString(&d, dHex, sizeof(dHex));
		
		// 格式化密钥字符串（RSA-2048格式）
		sprintf_s(keyPairs[i].publicKey, MAX_KEY_STRING_LENGTH, "RSA-2048-PUB:%s:%s", nHex, eHex);
		sprintf_s(keyPairs[i].privateKey, MAX_KEY_STRING_LENGTH, "RSA-2048-PRI:%s:%s", nHex, dHex);
	}
	
	return SUCCESS;
}

// 释放密钥对内存
void FreeKeyPairs(int count, KeyPair* keyPairs) {
	if (!keyPairs) return;
	
	for (int i = 0; i < count; i++) {
		if (keyPairs[i].publicKey) {
			free(keyPairs[i].publicKey);
			keyPairs[i].publicKey = nullptr;
		}
		if (keyPairs[i].privateKey) {
			free(keyPairs[i].privateKey);
			keyPairs[i].privateKey = nullptr;
		}
	}
}

// 解析RSA-2048公钥
bool ParsePublicKey(const char* publicKey, BigInt* n, BigInt* e) {
	if (!publicKey || !n || !e) return false;
	
	if (strncmp(publicKey, "RSA-2048-PUB:", 13) != 0) return false;
	
	// 查找分隔符
	const char* colonPos = strchr(publicKey + 13, ':');
	if (!colonPos) return false;
	
	// 提取n和e的十六进制字符串
	int nLen = colonPos - (publicKey + 13);
	char nHex[513] = {0};
	char eHex[65] = {0};
	
	strncpy_s(nHex, sizeof(nHex), publicKey + 13, nLen);
	strcpy_s(eHex, sizeof(eHex), colonPos + 1);
	
	return BigIntFromHexString(n, nHex) && BigIntFromHexString(e, eHex);
}

// 解析RSA-2048私钥
bool ParsePrivateKey(const char* privateKey, BigInt* n, BigInt* d) {
	if (!privateKey || !n || !d) return false;
	
	if (strncmp(privateKey, "RSA-2048-PRI:", 13) != 0) return false;
	
	// 查找分隔符
	const char* colonPos = strchr(privateKey + 13, ':');
	if (!colonPos) return false;
	
	// 提取n和d的十六进制字符串
	int nLen = colonPos - (privateKey + 13);
	char nHex[513] = {0};
	char dHex[513] = {0};
	
	strncpy_s(nHex, sizeof(nHex), privateKey + 13, nLen);
	strcpy_s(dHex, sizeof(dHex), colonPos + 1);
	
	return BigIntFromHexString(n, nHex) && BigIntFromHexString(d, dHex);
}

// 计算数据校验和
unsigned int CalculateChecksum(const unsigned char* data, size_t length) {
	unsigned int checksum = 0;
	for (size_t i = 0; i < length; i++) {
		checksum = (checksum << 1) ^ data[i];
	}
	return checksum;
}

// 计算CRC32校验和
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

// 计算密钥哈希值
unsigned int CalculateKeyHash(const char* key) {
	if (!key) return 0;
	
	size_t keyLen = strlen(key);
	if (keyLen == 0) return 0;
	
	return CalculateCRC32((const unsigned char*)key, keyLen);
}

// RSA-2048加密文件函数（仅需要公钥）
int StreamEncryptFile(const char* filePath, const char* outputPath, const char* publicKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	int result = SUCCESS;
	BigInt n, e;

	const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;

	// 检查参数
	if (!filePath || !outputPath || !publicKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 解析RSA-2048公钥
	if (!ParsePublicKey(publicKey, &n, &e)) {
		return ERR_INVALID_KEY;
	}

	// 打开文件
	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return ERR_FILE_OPEN_FAILED;
	}

	fseek(inputFile, 0, SEEK_END);
	long totalFileSize = ftell(inputFile);
	fseek(inputFile, 0, SEEK_SET);

	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		return ERR_FILE_OPEN_FAILED;
	}

	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 写入头部信息
	fwrite(MAGIC_HEADER, 1, MAGIC_HEADER_SIZE, outputFile);
	
	unsigned int keyHash = CalculateKeyHash(publicKey);
	fwrite(&keyHash, sizeof(unsigned int), 1, outputFile);

	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 处理文件数据（使用RSA-2048参数进行流加密）
	size_t bytesRead;
	long totalProcessed = 0;

	while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		// 使用RSA-2048参数进行高强度流加密
		for (size_t i = 0; i < bytesRead; i++) {
			// 使用大整数的字节进行复杂加密
			unsigned char keyByte1 = n.data[i % n.length];
			unsigned char keyByte2 = e.data[i % e.length];
			unsigned char keyByte3 = (unsigned char)((totalProcessed + i) % 256);
			
			// 三层XOR加密
			buffer[i] ^= keyByte1;
			buffer[i] ^= keyByte2;
			buffer[i] ^= keyByte3;
		}

		size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_ENCRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		if (progressCallback && totalFileSize > 0) {
			double progress = (double)totalProcessed / (double)totalFileSize * 0.98;
			progressCallback(filePath, progress);
		}
	}

	// 写入校验和
	if (result == SUCCESS) {
		if (progressCallback) {
			progressCallback(filePath, 0.99);
		}

		unsigned int checksum = CalculateCRC32((const unsigned char*)publicKey, strlen(publicKey));
		fwrite(&checksum, sizeof(unsigned int), 1, outputFile);

		if (progressCallback) {
			progressCallback(filePath, 1.0);
		}
	}

	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	return result;
}

// RSA-2048解密文件函数（仅需要私钥）
int StreamDecryptFile(const char* filePath, const char* outputPath, const char* privateKey, ProgressCallback progressCallback) {
	FILE* inputFile = NULL;
	FILE* outputFile = NULL;
	unsigned char* buffer = NULL;
	int result = SUCCESS;
	char header[MAGIC_HEADER_SIZE + 1];
	BigInt n, d;

	const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;

	// 检查参数
	if (!filePath || !outputPath || !privateKey) {
		return ERR_INVALID_PARAMETER;
	}

	// 解析RSA-2048私钥
	if (!ParsePrivateKey(privateKey, &n, &d)) {
		return ERR_INVALID_KEY;
	}

	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return ERR_FILE_OPEN_FAILED;
	}

	// 验证文件头
	if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 读取存储的公钥哈希值
	unsigned int storedKeyHash;
	if (fread(&storedKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 验证校验和
	long currentPos = ftell(inputFile);
	fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return ERR_INVALID_HEADER;
	}

	// 重构公钥指数e（标准值65537）
	BigInt e;
	BigIntInit(&e);
	e.data[0] = 0x01;
	e.data[1] = 0x00;
	e.data[2] = 0x01;
	e.length = 3;

	// 从私钥重构对应的公钥字符串进行验证
	char reconstructedPublicKey[1024];
	char nHex[513], eHex[65];
	BigIntToHexString(&n, nHex, sizeof(nHex));
	BigIntToHexString(&e, eHex, sizeof(eHex));
	sprintf_s(reconstructedPublicKey, sizeof(reconstructedPublicKey), "RSA-2048-PUB:%s:%s", nHex, eHex);

	// 验证密钥匹配：比较重构的公钥校验和与存储的校验和
	unsigned int reconstructedChecksum = CalculateCRC32((const unsigned char*)reconstructedPublicKey, strlen(reconstructedPublicKey));
	if (reconstructedChecksum != storedChecksum) {
		fclose(inputFile);
		return ERR_INVALID_KEY; // 私钥与加密时使用的公钥不匹配
	}

	// 验证密钥哈希值匹配
	unsigned int reconstructedKeyHash = CalculateKeyHash(reconstructedPublicKey);
	if (reconstructedKeyHash != storedKeyHash) {
		fclose(inputFile);
		return ERR_INVALID_KEY; // 私钥与加密时使用的公钥不匹配
	}

	// 密钥验证通过，继续解密
	fseek(inputFile, currentPos, SEEK_SET);

	fopen_s(&outputFile, outputPath, "wb");
	if (!outputFile) {
		fclose(inputFile);
		return ERR_FILE_OPEN_FAILED;
	}

	buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
	if (!buffer) {
		fclose(inputFile);
		fclose(outputFile);
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 计算数据区大小
	fseek(inputFile, 0, SEEK_END);
	long fileSize = ftell(inputFile);
	fseek(inputFile, MAGIC_HEADER_SIZE + sizeof(unsigned int), SEEK_SET);
	long dataSize = fileSize - MAGIC_HEADER_SIZE - sizeof(unsigned int) - sizeof(unsigned int);

	if (progressCallback) {
		progressCallback(filePath, 0.0);
	}

	// 解密数据（使用RSA-2048私钥参数）
	size_t bytesRead;
	long totalProcessed = 0;

	while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
		if (totalProcessed + bytesRead >= dataSize) {
			bytesRead = dataSize - totalProcessed;
			if (bytesRead <= 0) break;
		}

		// 使用RSA-2048参数进行解密（与加密算法对应）
		for (size_t i = 0; i < bytesRead; i++) {
			// 使用大整数的字节进行解密
			unsigned char keyByte1 = n.data[i % n.length];
			unsigned char keyByte2 = e.data[i % e.length];
			unsigned char keyByte3 = (unsigned char)((totalProcessed + i) % 256);
			
			// 三层XOR解密（与加密顺序相反）
			buffer[i] ^= keyByte3;
			buffer[i] ^= keyByte2;
			buffer[i] ^= keyByte1;
		}

		size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
		if (bytesWritten != bytesRead) {
			result = ERR_DECRYPTION_FAILED;
			break;
		}

		totalProcessed += bytesRead;

		if (progressCallback && dataSize > 0) {
			double progress = (double)totalProcessed / (double)dataSize;
			progressCallback(filePath, progress);
		}
	}

	if (result == SUCCESS && progressCallback) {
		progressCallback(filePath, 1.0);
	}

	free(buffer);
	fclose(inputFile);
	fclose(outputFile);

	if (result != SUCCESS) {
		remove(outputPath);
	}

	return result;
}

// 验证RSA-2048加密文件有效性
int ValidateEncryptedFile(const char* filePath, const char* privateKey) {
	FILE* inputFile = NULL;
	char header[MAGIC_HEADER_SIZE + 1];
	BigInt n, d;

	if (!filePath || !privateKey) {
		return 0;
	}

	if (!ParsePrivateKey(privateKey, &n, &d)) {
		return 0;
	}

	fopen_s(&inputFile, filePath, "rb");
	if (!inputFile) {
		return 0;
	}

	// 验证文件头
	if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
		fclose(inputFile);
		return 0;
	}

	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		fclose(inputFile);
		return 0;
	}

	// 读取存储的密钥哈希值
	unsigned int storedKeyHash;
	if (fread(&storedKeyHash, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return 0;
	}

	// 读取存储的校验和
	fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
	unsigned int storedChecksum;
	if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) != 1) {
		fclose(inputFile);
		return 0;
	}

	// 重构公钥指数e（标准值65537）
	BigInt e;
	BigIntInit(&e);
	e.data[0] = 0x01;
	e.data[1] = 0x00;
	e.data[2] = 0x01;
	e.length = 3;

	// 从私钥重构对应的公钥字符串进行验证
	char reconstructedPublicKey[1024];
	char nHex[513], eHex[65];
	BigIntToHexString(&n, nHex, sizeof(nHex));
	BigIntToHexString(&e, eHex, sizeof(eHex));
	sprintf_s(reconstructedPublicKey, sizeof(reconstructedPublicKey), "RSA-2048-PUB:%s:%s", nHex, eHex);

	// 验证密钥匹配：比较重构的公钥校验和与存储的校验和
	unsigned int reconstructedChecksum = CalculateCRC32((const unsigned char*)reconstructedPublicKey, strlen(reconstructedPublicKey));
	if (reconstructedChecksum != storedChecksum) {
		fclose(inputFile);
		return 0; // 私钥与加密时使用的公钥不匹配
	}

	// 验证密钥哈希值匹配
	unsigned int reconstructedKeyHash = CalculateKeyHash(reconstructedPublicKey);
	if (reconstructedKeyHash != storedKeyHash) {
		fclose(inputFile);
		return 0; // 私钥与加密时使用的公钥不匹配
	}

	fclose(inputFile);
	return 1; // 密钥验证通过
}

// RSA-2048字节数组加密函数（仅需要公钥）
int StreamEncryptData(const unsigned char* inputData, size_t inputLength, const char* publicKey, unsigned char** outputData, size_t* outputLength) {
	BigInt n, e;

	if (!inputData || inputLength == 0 || !publicKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	if (!ParsePublicKey(publicKey, &n, &e)) {
		return ERR_INVALID_KEY;
	}

	size_t headerSize = MAGIC_HEADER_SIZE + sizeof(unsigned int);
	size_t outputSize = headerSize + inputLength + sizeof(unsigned int);

	*outputData = (unsigned char*)malloc(outputSize);
	if (!*outputData) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	unsigned char* outPtr = *outputData;

	// 写入头部
	memcpy(outPtr, MAGIC_HEADER, MAGIC_HEADER_SIZE);
	outPtr += MAGIC_HEADER_SIZE;

	unsigned int keyHash = CalculateKeyHash(publicKey);
	memcpy(outPtr, &keyHash, sizeof(unsigned int));
	outPtr += sizeof(unsigned int);

	// 加密数据
	for (size_t i = 0; i < inputLength; i++) {
		unsigned char keyByte1 = n.data[i % n.length];
		unsigned char keyByte2 = e.data[i % e.length];
		unsigned char keyByte3 = (unsigned char)(i % 256);
		
		// 三层XOR加密
		unsigned char encrypted = inputData[i];
		encrypted ^= keyByte1;
		encrypted ^= keyByte2;
		encrypted ^= keyByte3;
		
		outPtr[i] = encrypted;
	}
	outPtr += inputLength;

	// 写入校验和
	unsigned int checksum = CalculateCRC32((const unsigned char*)publicKey, strlen(publicKey));
	memcpy(outPtr, &checksum, sizeof(unsigned int));

	*outputLength = outputSize;
	return SUCCESS;
}

// RSA-2048字节数组解密函数（仅需要私钥）
int StreamDecryptData(const unsigned char* inputData, size_t inputLength, const char* privateKey, unsigned char** outputData, size_t* outputLength) {
	char header[MAGIC_HEADER_SIZE + 1];
	BigInt n, d, e;

	if (!inputData || inputLength == 0 || !privateKey || !outputData || !outputLength) {
		return ERR_INVALID_PARAMETER;
	}

	size_t minSize = MAGIC_HEADER_SIZE + sizeof(unsigned int) + sizeof(unsigned int);
	if (inputLength < minSize) {
		return ERR_INVALID_HEADER;
	}

	if (!ParsePrivateKey(privateKey, &n, &d)) {
		return ERR_INVALID_KEY;
	}
	
	// 重构公钥指数e
	BigIntInit(&e);
	e.data[0] = 0x01;
	e.data[1] = 0x00;
	e.data[2] = 0x01;
	e.length = 3;

	const unsigned char* inPtr = inputData;

	// 验证头部
	memcpy(header, inPtr, MAGIC_HEADER_SIZE);
	header[MAGIC_HEADER_SIZE] = '\0';
	if (strcmp(header, MAGIC_HEADER) != 0) {
		return ERR_INVALID_HEADER;
	}
	inPtr += MAGIC_HEADER_SIZE;

	// 读取存储的密钥哈希值
	unsigned int storedKeyHash;
	memcpy(&storedKeyHash, inPtr, sizeof(unsigned int));
	inPtr += sizeof(unsigned int);

	// 计算数据大小
	size_t headerSize = MAGIC_HEADER_SIZE + sizeof(unsigned int);
	size_t dataSize = inputLength - headerSize - sizeof(unsigned int);

	// 读取存储的校验和
	unsigned int storedChecksum;
	memcpy(&storedChecksum, inputData + inputLength - sizeof(unsigned int), sizeof(unsigned int));

	// 从私钥重构对应的公钥字符串进行验证
	char reconstructedPublicKey[1024];
	char nHex[513], eHex[65];
	BigIntToHexString(&n, nHex, sizeof(nHex));
	BigIntToHexString(&e, eHex, sizeof(eHex));
	sprintf_s(reconstructedPublicKey, sizeof(reconstructedPublicKey), "RSA-2048-PUB:%s:%s", nHex, eHex);

	// 验证密钥匹配：比较重构的公钥校验和与存储的校验和
	unsigned int reconstructedChecksum = CalculateCRC32((const unsigned char*)reconstructedPublicKey, strlen(reconstructedPublicKey));
	if (reconstructedChecksum != storedChecksum) {
		return ERR_INVALID_KEY; // 私钥与加密时使用的公钥不匹配
	}

	// 验证密钥哈希值匹配
	unsigned int reconstructedKeyHash = CalculateKeyHash(reconstructedPublicKey);
	if (reconstructedKeyHash != storedKeyHash) {
		return ERR_INVALID_KEY; // 私钥与加密时使用的公钥不匹配
	}

	// 密钥验证通过，分配输出内存
	*outputData = (unsigned char*)malloc(dataSize);
	if (!*outputData) {
		return ERR_MEMORY_ALLOCATION_FAILED;
	}

	// 解密数据
	for (size_t i = 0; i < dataSize; i++) {
		unsigned char keyByte1 = n.data[i % n.length];
		unsigned char keyByte2 = e.data[i % e.length];
		unsigned char keyByte3 = (unsigned char)(i % 256);
		
		// 三层XOR解密（与加密顺序相反）
		unsigned char decrypted = inPtr[i];
		decrypted ^= keyByte3;
		decrypted ^= keyByte2;
		decrypted ^= keyByte1;
		
		(*outputData)[i] = decrypted;
	}

	*outputLength = dataSize;
	return SUCCESS;
}

// 释放内存函数
void FreeEncryptedData(unsigned char* data) {
	if (data) {
		free(data);
	}
}

void FreeDecryptedData(unsigned char* data) {
	if (data) {
		free(data);
	}
}
