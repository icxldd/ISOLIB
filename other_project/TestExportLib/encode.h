#pragma once

#include "pch.h"

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif

// Progress callback function type
// filePath: current file being processed
// progress: 0.0 to 1.0 (1.0 = 100% complete)
typedef void (*ProgressCallback)(const char* filePath, double progress);

extern "C" {
	PDUDLL_API int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback = nullptr);
	PDUDLL_API int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback = nullptr);
	PDUDLL_API int ValidateEncryptedFile(const char* filePath, const unsigned char* key);
}