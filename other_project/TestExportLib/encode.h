#pragma once

#include "pch.h"

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif


extern "C" {
	PDUDLL_API int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* key);
	PDUDLL_API int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* key);
	PDUDLL_API int ValidateEncryptedFile(const char* filePath, const unsigned char* key);
}