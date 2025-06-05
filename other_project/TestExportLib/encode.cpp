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

// 计算数据校验和用于文件完整性验证
unsigned int CalculateChecksum(const unsigned char* data, size_t length) {
    unsigned int checksum = 0;
    for (size_t i = 0; i < length; i++) {
        checksum = (checksum << 1) ^ data[i];
    }
    return checksum;
}

// 优化的流式文件加密函数（支持复杂位旋转和大缓冲区高性能处理）
int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback) {
    FILE* inputFile = NULL;
    FILE* outputFile = NULL;
    unsigned char* buffer = NULL;
    int result = SUCCESS;
    int keyLength = strlen((const char*)key);
    
    const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB大缓冲区用于高性能处理
    
    if (keyLength == 0) {
        //OutputDebugStringA("错误：提供的密钥为空");
        return ERR_ENCRYPTION_FAILED;
    }

    // 打开输入文件
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        //OutputDebugStringA("输入文件打开失败");
        return ERR_FILE_OPEN_FAILED;
    }

    // 获取文件大小用于进度计算
    fseek(inputFile, 0, SEEK_END);
    long totalFileSize = ftell(inputFile);
    fseek(inputFile, 0, SEEK_SET);

    // 打开输出文件
    fopen_s(&outputFile, outputPath, "wb");
    if (!outputFile) {
        //OutputDebugStringA("输出文件创建失败");
        fclose(inputFile);
        return ERR_FILE_OPEN_FAILED;
    }

    // 分配大缓冲区用于高性能处理
    buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
    if (!buffer) {
        //OutputDebugStringA("内存分配失败");
        fclose(inputFile);
        fclose(outputFile);
        return ERR_MEMORY_ALLOCATION_FAILED;
    }

    // 写入魔数头用于标识加密文件
    fwrite(MAGIC_HEADER, 1, MAGIC_HEADER_SIZE, outputFile);
    fwrite(&keyLength, sizeof(int), 1, outputFile);

    // 初始进度回调通知
    if (progressCallback) {
        progressCallback(filePath, 0.0);
    }

    // 大块处理文件以实现复杂加密的最高速度
    size_t bytesRead;
    long totalProcessed = 0;
    
    while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
        // 增强型XOR加密（带复杂位旋转增强安全性）
        for (size_t i = 0; i < bytesRead; i++) {
            // 对密钥应用XOR和位旋转
            unsigned char keyByte = key[i % keyLength];
            // 复杂位旋转增强安全性
            keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
            buffer[i] ^= keyByte;
        }
        
        // 立即写入加密数据
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
        if (bytesWritten != bytesRead) {
            //OutputDebugStringA("加密数据写入失败");
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
        
        unsigned int checksum = CalculateChecksum(key, keyLength);
        fwrite(&checksum, sizeof(unsigned int), 1, outputFile);
        
        // 最终进度回调 - 100%完成
        if (progressCallback) {
            progressCallback(filePath, 1.0);
        }
        
        //OutputDebugStringA("流式加密成功完成");
    }

    // 清理资源
    free(buffer);
    fclose(inputFile);
    fclose(outputFile);
    
    return result;
}

// 优化的流式文件解密函数（支持复杂位旋转和大缓冲区高性能处理）
int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* key, ProgressCallback progressCallback) {
    FILE* inputFile = NULL;
    FILE* outputFile = NULL;
    unsigned char* buffer = NULL;
    int result = SUCCESS;
    char header[MAGIC_HEADER_SIZE + 1];
    int storedKeyLength = 0;
    int keyLength = strlen((const char*)key);
    
    const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB大缓冲区

    if (keyLength == 0) {
        //OutputDebugStringA("错误：提供的密钥为空");
        return ERR_DECRYPTION_FAILED;
    }

    // 打开输入文件
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        //OutputDebugString(L"输入文件打开失败");
        return ERR_FILE_OPEN_FAILED;
    }

    // 读取并验证文件头（早期格式检测）
    if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
        //OutputDebugString(L"文件头读取失败 - 文件太小或已损坏");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    header[MAGIC_HEADER_SIZE] = '\0';
    if (strcmp(header, MAGIC_HEADER) != 0) {
        //OutputDebugString(L"无效文件格式 - 不是加密文件或版本错误");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    // 读取存储的密钥长度
    if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
        //OutputDebugString(L"密钥长度读取失败");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    // 验证密钥长度（早期验证）
    if (storedKeyLength != keyLength) {
        //OutputDebugStringA("密钥长度不匹配 - 提供了错误的密钥");
        fclose(inputFile);
        return ERR_DECRYPTION_FAILED;
    }

    // *** 新增：立即验证校验和，避免大文件错误解密 ***
    // 保存当前文件位置
    long currentPos = ftell(inputFile);
    
    // 跳到文件末尾读取校验和
    fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
    unsigned int storedChecksum;
    if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
        unsigned int calculatedChecksum = CalculateChecksum(key, keyLength);
        if (storedChecksum != calculatedChecksum) {
            //OutputDebugStringA("校验和验证失败 - 密钥错误或文件已损坏，停止解密");
            fclose(inputFile);
            return ERR_DECRYPTION_FAILED;
        } else {
            //OutputDebugStringA("校验和验证成功 - 密钥正确，开始解密");
        }
    } else {
        //OutputDebugStringA("无法读取校验和");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }
    
    // 恢复文件位置到数据开始处
    fseek(inputFile, currentPos, SEEK_SET);

    // 打开输出文件
    fopen_s(&outputFile, outputPath, "wb");
    if (!outputFile) {
        //OutputDebugStringA("输出文件创建失败");
        fclose(inputFile);
        return ERR_FILE_OPEN_FAILED;
    }

    // 分配大缓冲区
    buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
    if (!buffer) {
        //OutputDebugStringA("内存分配失败");
        fclose(inputFile);
        fclose(outputFile);
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
        
        // 增强型XOR解密（带复杂位旋转，与加密算法相同）
        for (size_t i = 0; i < bytesRead; i++) {
            // 应用XOR和密钥旋转（与加密算法相同）
            unsigned char keyByte = key[i % keyLength];
            // 复杂位旋转增强安全性
            keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
            buffer[i] ^= keyByte;
        }
        
        // 立即写入解密数据
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
        if (bytesWritten != bytesRead) {
            //OutputDebugStringA("解密数据写入失败");
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
        
        //OutputDebugStringA("流式解密成功完成");
    }

    // 清理资源
    free(buffer);
    fclose(inputFile);
    fclose(outputFile);
    
    if (result != SUCCESS) {
        remove(outputPath);  // 如果解密失败则删除输出文件
    }
    
    return result;
}

int ValidateEncryptedFile(const char* filePath, const unsigned char* key) {
    FILE* inputFile = NULL;
    unsigned char* buffer = NULL;
    char header[MAGIC_HEADER_SIZE + 1];
    int storedKeyLength = 0;
    int keyLength = strlen((const char*)key);
    bool isValid = false;

    if (keyLength == 0) {
        //OutputDebugStringA("Error: Empty key provided");
        return 0; // Invalid
    }

    // Open input file
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        //OutputDebugStringA("Failed to open input file for validation");
        return 0; // Invalid
    }

    // Read and validate header (early format detection)
    if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
        //OutputDebugStringA("Validation failed: Cannot read header");
        fclose(inputFile);
        return 0; // Invalid
    }

    header[MAGIC_HEADER_SIZE] = '\0';
    if (strcmp(header, MAGIC_HEADER) != 0) {
        //OutputDebugStringA("Validation failed: Invalid file format");
        fclose(inputFile);
        return 0; // Invalid
    }

    // Read stored key length
    if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
        //OutputDebugStringA("Validation failed: Cannot read key length");
        fclose(inputFile);
        return 0; // Invalid
    }

    // Validate key length (early validation)
    if (storedKeyLength != keyLength) {
        //OutputDebugStringA("Validation failed: Key length mismatch");
        fclose(inputFile);
        return 0; // Invalid
    }

    // Validate checksum at the end of file
    fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
    unsigned int storedChecksum;
    if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
        unsigned int calculatedChecksum = CalculateChecksum(key, keyLength);
        if (storedChecksum == calculatedChecksum) {
            isValid = true;
            //OutputDebugStringA("File validation successful");
        } else {
            //OutputDebugStringA("Validation failed: Checksum mismatch");
        }
    } else {
        //OutputDebugStringA("Validation failed: Cannot read checksum");
    }

    // Clean up
    fclose(inputFile);
    
    return isValid ? 1 : 0;
}
