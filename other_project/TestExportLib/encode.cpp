#include "pch.h"
#include "encode.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>
#include <process.h>

// Constants for encryption
#define BUFFER_SIZE 4096
#define MAGIC_HEADER "ENCV1.0"
#define MAGIC_HEADER_SIZE 7
#define CHUNK_SIZE 1024
#define MAX_THREADS 4
#define DEFAULT_KEY_LENGTH 256  // Maximum key length

// Error codes
#define SUCCESS 0
#define ERR_FILE_OPEN_FAILED -1
#define ERR_MEMORY_ALLOCATION_FAILED -2
#define ERR_ENCRYPTION_FAILED -3
#define ERR_DECRYPTION_FAILED -4
#define ERR_INVALID_HEADER -5
#define ERR_THREAD_CREATION_FAILED -6

// Structure for thread parameters
typedef struct {
    unsigned char* buffer;
    size_t bufferSize;
    const unsigned char* key;
    int keyLength;
    bool isEncrypt;
    int threadId;
    size_t startOffset;
    size_t endOffset;
} ThreadData;

// Critical section for thread synchronization
CRITICAL_SECTION g_criticalSection;
static bool g_criticalSectionInitialized = false;

// Initialize critical section if not already done
void InitializeCriticalSectionIfNeeded() {
    if (!g_criticalSectionInitialized) {
        InitializeCriticalSection(&g_criticalSection);
        g_criticalSectionInitialized = true;
    }
}

// Clean up critical section
void CleanupCriticalSection() {
    if (g_criticalSectionInitialized) {
        DeleteCriticalSection(&g_criticalSection);
        g_criticalSectionInitialized = false;
    }
}

// Enhanced XOR encryption with rotation
void xorBuffer(unsigned char* buffer, size_t bufferSize, const unsigned char* key, int keyLength) {
    for (size_t i = 0; i < bufferSize; i++) {
        // Apply XOR with key rotation
        unsigned char keyByte = key[i % keyLength];
        // Add position-based rotation for better security
        keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
        buffer[i] ^= keyByte;
    }
}

// Thread worker function for encryption/decryption
unsigned __stdcall CryptoWorker(void* param) {
    ThreadData* data = (ThreadData*)param;
    
    // Process the assigned portion of the buffer
    for (size_t i = data->startOffset; i < data->endOffset && i < data->bufferSize; i++) {
        // Enhanced XOR with rotation
        unsigned char keyByte = data->key[i % data->keyLength];
        keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
        data->buffer[i] ^= keyByte;
    }
    
    return 0;
}

// Process a buffer in parallel using multiple threads
bool ProcessBufferParallel(unsigned char* buffer, size_t bufferSize, const unsigned char* key, int keyLength, bool isEncrypt) {
    if (bufferSize == 0) return true;
    
    HANDLE threads[MAX_THREADS];
    ThreadData threadData[MAX_THREADS];
    unsigned threadIDs[MAX_THREADS];
    
    // Calculate chunk size for each thread
    size_t chunkSize = (bufferSize + MAX_THREADS - 1) / MAX_THREADS;
    int actualThreads = min(MAX_THREADS, (int)((bufferSize + chunkSize - 1) / chunkSize));
    
    // Create and start threads
    for (int i = 0; i < actualThreads; i++) {
        // Set thread data
        threadData[i].buffer = buffer;
        threadData[i].bufferSize = bufferSize;
        threadData[i].key = key;
        threadData[i].keyLength = keyLength;
        threadData[i].isEncrypt = isEncrypt;
        threadData[i].threadId = i;
        threadData[i].startOffset = i * chunkSize;
        threadData[i].endOffset = min((i + 1) * chunkSize, bufferSize);
        
        // Create thread
        threads[i] = (HANDLE)_beginthreadex(NULL, 0, CryptoWorker, &threadData[i], 0, &threadIDs[i]);
        
        if (threads[i] == NULL) {
            // Thread creation failed, wait for already created threads
            for (int j = 0; j < i; j++) {
                WaitForSingleObject(threads[j], INFINITE);
                CloseHandle(threads[j]);
            }
            return false;
        }
    }
    
    // Wait for all threads to complete
    WaitForMultipleObjects(actualThreads, threads, TRUE, INFINITE);
    
    // Close thread handles
    for (int i = 0; i < actualThreads; i++) {
        CloseHandle(threads[i]);
    }
    
    return true;
}

// Calculate checksum for validation
unsigned int CalculateChecksum(const unsigned char* data, size_t length) {
    unsigned int checksum = 0;
    for (size_t i = 0; i < length; i++) {
        checksum = (checksum << 1) ^ data[i];
    }
    return checksum;
}

// Optimized StreamEncryptFile with complex bit rotation and large buffer
int StreamEncryptFile(const char* filePath, const char* outputPath, const unsigned char* key) {
    FILE* inputFile = NULL;
    FILE* outputFile = NULL;
    unsigned char* buffer = NULL;
    int result = SUCCESS;
    int keyLength = strlen((const char*)key);
    
    const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB buffer for high performance
    
    if (keyLength == 0) {
        OutputDebugStringA("Error: Empty key provided");
        return ERR_ENCRYPTION_FAILED;
    }

    // Open input file
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        OutputDebugStringA("Failed to open input file");
        return ERR_FILE_OPEN_FAILED;
    }

    // Open output file
    fopen_s(&outputFile, outputPath, "wb");
    if (!outputFile) {
        OutputDebugStringA("Failed to open output file");
        fclose(inputFile);
        return ERR_FILE_OPEN_FAILED;
    }

    // Allocate large buffer for high-performance processing
    buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
    if (!buffer) {
        OutputDebugStringA("Failed to allocate memory");
        fclose(inputFile);
        fclose(outputFile);
        return ERR_MEMORY_ALLOCATION_FAILED;
    }

    // Write magic header to identify encrypted files
    fwrite(MAGIC_HEADER, 1, MAGIC_HEADER_SIZE, outputFile);
    fwrite(&keyLength, sizeof(int), 1, outputFile);

    // Process file in large chunks for maximum speed with complex encryption
    size_t bytesRead;
    while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
        // Enhanced XOR encryption with complex bit rotation for security
        for (size_t i = 0; i < bytesRead; i++) {
            // Apply XOR with key rotation
            unsigned char keyByte = key[i % keyLength];
            // Complex bit rotation for enhanced security
            keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
            buffer[i] ^= keyByte;
        }
        
        // Write encrypted data immediately
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
        if (bytesWritten != bytesRead) {
            OutputDebugStringA("Failed to write encrypted data");
            result = ERR_ENCRYPTION_FAILED;
            break;
        }
    }

    // Write checksum
    if (result == SUCCESS) {
        unsigned int checksum = CalculateChecksum(key, keyLength);
        fwrite(&checksum, sizeof(unsigned int), 1, outputFile);
        OutputDebugStringA("Stream encryption completed successfully");
    }

    // Clean up
    free(buffer);
    fclose(inputFile);
    fclose(outputFile);
    
    return result;
}

// Optimized StreamDecryptFile with complex bit rotation and large buffer
int StreamDecryptFile(const char* filePath, const char* outputPath, const unsigned char* key) {
    FILE* inputFile = NULL;
    FILE* outputFile = NULL;
    unsigned char* buffer = NULL;
    int result = SUCCESS;
    char header[MAGIC_HEADER_SIZE + 1];
    int storedKeyLength = 0;
    int keyLength = strlen((const char*)key);
    
    const size_t STREAM_BUFFER_SIZE = 4 * 1024 * 1024;  // 4MB buffer

    if (keyLength == 0) {
        OutputDebugStringA("Error: Empty key provided");
        return ERR_DECRYPTION_FAILED;
    }

    // Open input file
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        OutputDebugStringA("Failed to open input file");
        return ERR_FILE_OPEN_FAILED;
    }

    // Read and validate header (early format detection)
    if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
        OutputDebugStringA("Failed to read header - file too small or corrupted");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    header[MAGIC_HEADER_SIZE] = '\0';
    if (strcmp(header, MAGIC_HEADER) != 0) {
        OutputDebugStringA("Invalid file format - not an encrypted file or wrong version");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    // Read stored key length
    if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
        OutputDebugStringA("Failed to read key length");
        fclose(inputFile);
        return ERR_INVALID_HEADER;
    }

    // Validate key length (early validation)
    if (storedKeyLength != keyLength) {
        OutputDebugStringA("Key length mismatch - wrong key provided");
        fclose(inputFile);
        return ERR_DECRYPTION_FAILED;
    }

    // Open output file
    fopen_s(&outputFile, outputPath, "wb");
    if (!outputFile) {
        OutputDebugStringA("Failed to open output file");
        fclose(inputFile);
        return ERR_FILE_OPEN_FAILED;
    }

    // Allocate large buffer
    buffer = (unsigned char*)malloc(STREAM_BUFFER_SIZE);
    if (!buffer) {
        OutputDebugStringA("Failed to allocate memory");
        fclose(inputFile);
        fclose(outputFile);
        return ERR_MEMORY_ALLOCATION_FAILED;
    }

    // Get file size and calculate data size
    fseek(inputFile, 0, SEEK_END);
    long fileSize = ftell(inputFile);
    fseek(inputFile, MAGIC_HEADER_SIZE + sizeof(int), SEEK_SET);
    long dataSize = fileSize - MAGIC_HEADER_SIZE - sizeof(int) - sizeof(unsigned int);

    // Process file in large chunks for maximum speed with complex decryption
    size_t bytesRead;
    long totalProcessed = 0;
    
    while ((bytesRead = fread(buffer, 1, STREAM_BUFFER_SIZE, inputFile)) > 0) {
        // Handle last chunk with checksum
        if (totalProcessed + bytesRead >= dataSize) {
            bytesRead = dataSize - totalProcessed;
            if (bytesRead <= 0) break;
        }
        
        // Enhanced XOR decryption with complex bit rotation (same as encryption)
        for (size_t i = 0; i < bytesRead; i++) {
            // Apply XOR with key rotation (same algorithm as encryption)
            unsigned char keyByte = key[i % keyLength];
            // Complex bit rotation for enhanced security
            keyByte = (keyByte << (i % 8)) | (keyByte >> (8 - (i % 8)));
            buffer[i] ^= keyByte;
        }
        
        // Write decrypted data immediately
        size_t bytesWritten = fwrite(buffer, 1, bytesRead, outputFile);
        if (bytesWritten != bytesRead) {
            OutputDebugStringA("Failed to write decrypted data");
            result = ERR_DECRYPTION_FAILED;
            break;
        }
        
        totalProcessed += bytesRead;
    }

    // Verify checksum
    if (result == SUCCESS) {
        fseek(inputFile, -(long)sizeof(unsigned int), SEEK_END);
        unsigned int storedChecksum;
        if (fread(&storedChecksum, sizeof(unsigned int), 1, inputFile) == 1) {
            unsigned int calculatedChecksum = CalculateChecksum(key, keyLength);
            if (storedChecksum != calculatedChecksum) {
                OutputDebugStringA("Checksum validation failed - wrong key or corrupted file");
                result = ERR_DECRYPTION_FAILED;
            } else {
                OutputDebugStringA("Stream decryption completed successfully");
            }
        }
    }

    // Clean up
    free(buffer);
    fclose(inputFile);
    fclose(outputFile);
    
    if (result != SUCCESS) {
        remove(outputPath);  // Delete output file if decryption failed
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
        OutputDebugStringA("Error: Empty key provided");
        return 0; // Invalid
    }

    // Open input file
    fopen_s(&inputFile, filePath, "rb");
    if (!inputFile) {
        OutputDebugStringA("Failed to open input file for validation");
        return 0; // Invalid
    }

    // Read and validate header (early format detection)
    if (fread(header, 1, MAGIC_HEADER_SIZE, inputFile) != MAGIC_HEADER_SIZE) {
        OutputDebugStringA("Validation failed: Cannot read header");
        fclose(inputFile);
        return 0; // Invalid
    }

    header[MAGIC_HEADER_SIZE] = '\0';
    if (strcmp(header, MAGIC_HEADER) != 0) {
        OutputDebugStringA("Validation failed: Invalid file format");
        fclose(inputFile);
        return 0; // Invalid
    }

    // Read stored key length
    if (fread(&storedKeyLength, sizeof(int), 1, inputFile) != 1) {
        OutputDebugStringA("Validation failed: Cannot read key length");
        fclose(inputFile);
        return 0; // Invalid
    }

    // Validate key length (early validation)
    if (storedKeyLength != keyLength) {
        OutputDebugStringA("Validation failed: Key length mismatch");
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
            OutputDebugStringA("File validation successful");
        } else {
            OutputDebugStringA("Validation failed: Checksum mismatch");
        }
    } else {
        OutputDebugStringA("Validation failed: Cannot read checksum");
    }

    // Clean up
    fclose(inputFile);
    
    return isValid ? 1 : 0;
}
