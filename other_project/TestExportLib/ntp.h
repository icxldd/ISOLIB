#pragma once

#include "pch.h"
#include <stdint.h>

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif

// NTP时间同步结果码
#define NTP_SUCCESS 0
#define NTP_ERROR_NETWORK -1
#define NTP_ERROR_TIMEOUT -2
#define NTP_ERROR_INVALID_RESPONSE -3
#define NTP_ERROR_ALL_SERVERS_FAILED -4

// x86兼容的64位整数类型
#ifdef _WIN64
    typedef long long timestamp_t;
#else
    typedef __int64 timestamp_t;
#endif

extern "C" {
    // 获取NTP同步时间戳（11位Unix时间戳）
    // 返回值：成功时返回NTP_SUCCESS，失败时返回错误码
    // timestamp：输出参数，存储获取到的时间戳
    PDUDLL_API int __cdecl GetNTPTimestamp(timestamp_t* timestamp);
    
    // 使用指定NTP服务器获取时间戳
    // server：NTP服务器地址
    // timestamp：输出参数，存储获取到的时间戳
    // timeoutMs：超时时间（毫秒）
    PDUDLL_API int __cdecl GetNTPTimestampFromServer(const char* server, timestamp_t* timestamp, int timeoutMs);
    
    // 获取本地时间戳（备用方案）
    PDUDLL_API timestamp_t __cdecl GetLocalTimestamp();
}

