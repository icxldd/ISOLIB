#pragma once

#include "pch.h"
#include <stdint.h>

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif

// NTP时间同步结果状态码
#define NTP_SUCCESS 0                      // 成功
#define NTP_ERROR_NETWORK -1               // 网络错误
#define NTP_ERROR_TIMEOUT -2               // 请求超时
#define NTP_ERROR_INVALID_RESPONSE -3      // 无效响应
#define NTP_ERROR_ALL_SERVERS_FAILED -4    // 所有服务器失败

// x86架构兼容的64位整数类型定义
#ifdef _WIN64
    typedef long long timestamp_t;
#else
    typedef __int64 timestamp_t;
#endif

extern "C" {
    // 获取NTP网络时间同步的Unix时间戳（11位时间戳）
    // 返回值：成功时返回NTP_SUCCESS，失败时返回相应错误码
    // timestamp：输出参数，用于存储获取到的时间戳
    PDUDLL_API int __cdecl GetNTPTimestamp(timestamp_t* timestamp);
    
    // 从指定NTP服务器获取时间戳
    // server：NTP服务器地址（如：cn.pool.ntp.org）
    // timestamp：输出参数，用于存储获取到的时间戳
    // timeoutMs：网络超时时间（毫秒）
    int __cdecl GetNTPTimestampFromServer(const char* server, timestamp_t* timestamp, int timeoutMs);
    
    // 获取本地系统时间戳（备用方案，当NTP失败时使用）
    // 返回值：本地Unix时间戳
    timestamp_t __cdecl GetLocalTimestamp();
}

