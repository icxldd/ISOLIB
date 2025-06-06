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

	/// @brief 使用私钥初始化加密系统
	/// @param privateKey 私钥字符串（支持任意长度）
	/// @return 0表示成功，负数表示错误码
	PDUDLL_API int InitStreamFile(const char* privateKey);

	/// @brief 清理私钥，释放内存
	PDUDLL_API void ClearPrivateKey();

	/// @brief 检查私钥是否已设置
	/// @return 1表示已设置，0表示未设置
	PDUDLL_API int IsPrivateKeySet();

	// 流式加密文件函数（双密钥系统：需要预先设置私钥，此处传入公钥）
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// publicKey: 公钥（与私钥组合使用）
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback = nullptr);

	// 流式解密文件函数（双密钥系统：需要预先设置私钥，此处传入公钥）
	// filePath: 输入文件路径
	// outputPath: 输出文件路径
	// publicKey: 公钥（与私钥组合使用）
	// progressCallback: 进度回调函数（可为空）
	PDUDLL_API int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* publicKey, ProgressCallback progressCallback = nullptr);

	// 验证加密文件有效性（双密钥系统）
	// filePath: 加密文件路径
	// publicKey: 公钥（与预设私钥组合验证）
	// 返回值: 1表示有效，0表示无效
	PDUDLL_API int ValidateEncryptedFile(const char* filePath, const unsigned char* publicKey);
}