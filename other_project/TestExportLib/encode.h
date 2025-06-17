#pragma once

#include "pch.h"

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif

// 密钥对结构（动态长度字符串）
typedef struct {
	char* publicKey;   // 公钥字符串（动态分配）
	char* privateKey;  // 私钥字符串（动态分配）
} KeyPair;

// 进度回调函数类型
// filePath: 当前正在处理的文件路径
// progress: 0.0 到 1.0 的进度值（1.0 表示 100% 完成）
typedef void (*ProgressCallback)(const char* filePath, double progress);

extern "C" {

	/// @brief 生成RSA密钥对
	/// @param count 要生成的密钥对数量
	/// @param keyPairs 输出参数：密钥对数组（调用者需要预先分配内存）
	/// @return 0表示成功，负数表示错误码
	PDUDLL_API int GenerateKeyPairs(int count, KeyPair* keyPairs);

	/// @brief 释放密钥对内存
	/// @param count 密钥对数量
	/// @param keyPairs 密钥对数组
	PDUDLL_API void FreeKeyPairs(int count, KeyPair* keyPairs);

	// 非对称加密文件函数（仅需要公钥）
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// publicKey: 公钥字符串（用于加密）
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamEncryptFile(const char* filePath, const char* outputPath, const char* publicKey, ProgressCallback progressCallback = nullptr);

	// 非对称解密文件函数（仅需要私钥）
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// privateKey: 私钥字符串（用于解密）
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamDecryptFile(const char* filePath, const char* outputPath, const char* privateKey, ProgressCallback progressCallback = nullptr);

	// 验证加密文件有效性（使用私钥验证）
	// filePath: 加密文件路径
	// privateKey: 私钥字符串（用于验证）
	// 返回值: 1表示有效，0表示无效
	PDUDLL_API int ValidateEncryptedFile(const char* filePath, const char* privateKey);

	// 字节数组加密函数（仅需要公钥）
	// inputData: 输入数据指针
	// inputLength: 输入数据长度
	// publicKey: 公钥字符串（用于加密）
	// outputData: 输出加密数据指针（由函数分配内存）
	// outputLength: 输出数据长度（由函数设置）
	// 返回值: 0表示成功，负数表示错误码
	// 注意: 调用者需要使用 FreeEncryptedData 释放 outputData 内存
	PDUDLL_API int StreamEncryptData(const unsigned char* inputData, size_t inputLength, const char* publicKey, unsigned char** outputData, size_t* outputLength);

	// 字节数组解密函数（仅需要私钥）
	// inputData: 输入加密数据指针
	// inputLength: 输入数据长度
	// privateKey: 私钥字符串（用于解密）
	// outputData: 输出解密数据指针（由函数分配内存）
	// outputLength: 输出数据长度（由函数设置）
	// 返回值: 0表示成功，负数表示错误码
	// 注意: 调用者需要使用 FreeDecryptedData 释放 outputData 内存
	PDUDLL_API int StreamDecryptData(const unsigned char* inputData, size_t inputLength, const char* privateKey, unsigned char** outputData, size_t* outputLength);

	// 释放加密数据内存
	// data: 由 StreamEncryptData 分配的内存指针
	PDUDLL_API void FreeEncryptedData(unsigned char* data);

	// 释放解密数据内存
	// data: 由 StreamDecryptData 分配的内存指针
	PDUDLL_API void FreeDecryptedData(unsigned char* data);

	// 计算CRC32校验和（内部函数，用于更强的校验）
	PDUDLL_API unsigned int CalculateCRC32(const unsigned char* data, size_t length);

	// 计算密钥哈希值（内部函数，用于密钥完整性验证）
	PDUDLL_API unsigned int CalculateKeyHash(const char* key);
}