#pragma once

#include "pch.h"

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif

// 进度回调函数类型
// filePath: 当前正在处理的文件路径
// progress: 0.0 到 1.0 的进度值（1.0 表示 100% 完成）
typedef void (*ProgressCallback)(const char* filePath, double progress);

extern "C" {
	// 流式加密文件函数
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// key: 加密密钥
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback = nullptr);
	
	// 流式解密文件函数
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// key: 解密密钥
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback = nullptr);
	
	// 验证加密文件有效性
	// filePath: 加密文件路径
	// key: 验证密钥
	// 返回值: 1表示有效，0表示无效
	PDUDLL_API int ValidateEncryptedFile(const char* filePath, const unsigned char* key);
}