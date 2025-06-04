#include "pch.h"
#include "ntp.h"
#include <winsock2.h>
#include <ws2tcpip.h>
#include <time.h>
#include <vector>
#include <string>

#pragma comment(lib, "ws2_32.lib")

#define _WINSOCK_DEPRECATED_NO_WARNINGS


// NTP常量
#define NTP_PORT 123
#define NTP_PACKET_SIZE 48

// x86兼容的常量定义
#ifdef _WIN64
    #define NTP_UNIX_EPOCH_OFFSET 2208988800ULL
#else
    #define NTP_UNIX_EPOCH_OFFSET 2208988800UI64
#endif

// x86兼容的64位整数类型
typedef union {
    timestamp_t value;
    struct {
        uint32_t low;
        uint32_t high;
    } parts;
} uint64_union_t;

// NTP数据包结构 - x86对齐优化
#pragma pack(push, 1)
struct NTPPacket {
    unsigned char li_vn_mode;       // LI (2 bits) + VN (3 bits) + Mode (3 bits)
    unsigned char stratum;          // Stratum
    unsigned char poll;             // Poll interval
    unsigned char precision;        // Precision
    uint32_t root_delay;           // Root delay
    uint32_t root_dispersion;      // Root dispersion
    uint32_t reference_id;         // Reference identifier
    uint64_union_t reference_timestamp;  // Reference timestamp
    uint64_union_t origin_timestamp;     // Origin timestamp
    uint64_union_t receive_timestamp;    // Receive timestamp
    uint64_union_t transmit_timestamp;   // Transmit timestamp
};
#pragma pack(pop)

// 全球NTP服务器列表
static const char* NTP_SERVERS[] = {
    // 亚洲优先（中国用户）
    "cn.pool.ntp.org",
    "asia.pool.ntp.org", 
    "hk.pool.ntp.org",
    "tw.pool.ntp.org",
    "jp.pool.ntp.org",
    "kr.pool.ntp.org",
    "sg.pool.ntp.org",
    
    // 全球通用
    "pool.ntp.org",
    "time.windows.com",
    "time.nist.gov",
    
    // 欧洲
    "europe.pool.ntp.org",
    "de.pool.ntp.org",
    "uk.pool.ntp.org",
    "fr.pool.ntp.org",
    
    // 北美
    "north-america.pool.ntp.org",
    "us.pool.ntp.org",
    "ca.pool.ntp.org",
    
    // 其他地区
    "oceania.pool.ntp.org",
    "au.pool.ntp.org",
    "south-america.pool.ntp.org",
    "br.pool.ntp.org",
    "africa.pool.ntp.org"
};

// x86兼容的64位网络字节序转换
timestamp_t ntohl64(uint64_union_t net_value) {
    uint64_union_t result;
    result.parts.high = ntohl(net_value.parts.low);
    result.parts.low = ntohl(net_value.parts.high);
    return result.value;
}

uint64_union_t htonl64(timestamp_t host_value) {
    uint64_union_t net_value;
    uint64_union_t temp;
    temp.value = host_value;
    net_value.parts.high = htonl(temp.parts.low);
    net_value.parts.low = htonl(temp.parts.high);
    return net_value;
}

// 初始化Winsock
BOOL InitializeWinsock() {
    WSADATA wsaData;
    return WSAStartup(MAKEWORD(2, 2), &wsaData) == 0;
}

// 清理Winsock
void CleanupWinsock() {
    WSACleanup();
}

// 获取当前时间的NTP时间戳 - x86兼容版本
timestamp_t GetCurrentNTPTime() {
    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);
    
    // x86兼容的64位时间计算
    uint64_union_t time64;
    time64.parts.low = ft.dwLowDateTime;
    time64.parts.high = ft.dwHighDateTime;
    
    // 转换为Unix时间戳（秒）
    timestamp_t unixTime = (time64.value / 10000000) - 11644473600LL; // 1601年到1970年的秒数
    
    // 转换为NTP时间戳（从1900年开始）
    return unixTime + NTP_UNIX_EPOCH_OFFSET;
}

// 从单个NTP服务器获取时间
int __cdecl GetNTPTimestampFromServer(const char* server, timestamp_t* timestamp, int timeoutMs) {
    if (!timestamp) {
        return NTP_ERROR_INVALID_RESPONSE;
    }

    SOCKET sock = INVALID_SOCKET;
    struct sockaddr_in serverAddr;
    NTPPacket packet;
    int result = NTP_ERROR_NETWORK;

    // 初始化Winsock
    if (!InitializeWinsock()) {
        OutputDebugStringA("Failed to initialize Winsock");
        return NTP_ERROR_NETWORK;
    }

    do {
        // 创建UDP套接字
        sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
        if (sock == INVALID_SOCKET) {
            OutputDebugStringA("Failed to create socket");
            break;
        }

        // 设置超时
        DWORD timeout = (DWORD)timeoutMs;
        setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, (char*)&timeout, sizeof(timeout));
        setsockopt(sock, SOL_SOCKET, SO_SNDTIMEO, (char*)&timeout, sizeof(timeout));

        // 解析服务器地址
        ZeroMemory(&serverAddr, sizeof(serverAddr));
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(NTP_PORT);
        
        // 尝试直接转换IP地址
        serverAddr.sin_addr.s_addr = inet_addr(server);
        if (serverAddr.sin_addr.s_addr == INADDR_NONE) {
            // 如果不是IP地址，进行DNS解析
            struct hostent* host = gethostbyname(server);
            if (!host) {
                char debugMsg[256];
                sprintf_s(debugMsg, sizeof(debugMsg), "Failed to resolve hostname: %s", server);
                OutputDebugStringA(debugMsg);
                result = NTP_ERROR_NETWORK;
                break;
            }
            CopyMemory(&serverAddr.sin_addr, host->h_addr_list[0], host->h_length);
        }

        // 准备NTP请求包
        ZeroMemory(&packet, sizeof(packet));
        packet.li_vn_mode = 0x1B; // LI=0, VN=3, Mode=3 (client)
        
        // 设置传输时间戳
        timestamp_t currentTime = GetCurrentNTPTime();
        packet.transmit_timestamp = htonl64(currentTime);

        // 发送NTP请求
        if (sendto(sock, (char*)&packet, sizeof(packet), 0, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
            OutputDebugStringA("Failed to send NTP request");
            result = NTP_ERROR_NETWORK;
            break;
        }

        // 接收NTP响应
        struct sockaddr_in fromAddr;
        int fromLen = sizeof(fromAddr);
        int bytesReceived = recvfrom(sock, (char*)&packet, sizeof(packet), 0, (struct sockaddr*)&fromAddr, &fromLen);
        
        if (bytesReceived == SOCKET_ERROR) {
            int error = WSAGetLastError();
            if (error == WSAETIMEDOUT) {
                OutputDebugStringA("NTP request timeout");
                result = NTP_ERROR_TIMEOUT;
            } else {
                OutputDebugStringA("Failed to receive NTP response");
                result = NTP_ERROR_NETWORK;
            }
            break;
        }

        if (bytesReceived != sizeof(packet)) {
            OutputDebugStringA("Invalid NTP response size");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        // 验证响应
        if ((packet.li_vn_mode & 0x07) != 4) { // Mode应该是4 (server)
            OutputDebugStringA("Invalid NTP response mode");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        // 提取服务器时间戳
        timestamp_t serverTime = ntohl64(packet.transmit_timestamp);
        
        // 转换为Unix时间戳
        if (serverTime < NTP_UNIX_EPOCH_OFFSET) {
            OutputDebugStringA("Invalid NTP timestamp");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        *timestamp = serverTime - NTP_UNIX_EPOCH_OFFSET;
        
        char debugMsg[256];
        sprintf_s(debugMsg, sizeof(debugMsg), "NTP sync successful from %s, timestamp: %I64d", server, *timestamp);
        OutputDebugStringA(debugMsg);
        
        result = NTP_SUCCESS;

    } while (FALSE);

    // 清理
    if (sock != INVALID_SOCKET) {
        closesocket(sock);
    }
    CleanupWinsock();

    return result;
}

// 轮询多个NTP服务器获取时间戳
int __cdecl GetNTPTimestamp(timestamp_t* timestamp) {
    if (!timestamp) {
        return NTP_ERROR_INVALID_RESPONSE;
    }

    const int serverCount = sizeof(NTP_SERVERS) / sizeof(NTP_SERVERS[0]);
    
    OutputDebugStringA("Starting NTP synchronization...");
    
    // 轮询服务器列表
    for (int i = 0; i < serverCount; i++) {
        char debugMsg[256];
        sprintf_s(debugMsg, sizeof(debugMsg), "Trying NTP server: %s (%d/%d)", NTP_SERVERS[i], i + 1, serverCount);
        OutputDebugStringA(debugMsg);
        
        int result = GetNTPTimestampFromServer(NTP_SERVERS[i], timestamp, 3000); // 3秒超时
        
        if (result == NTP_SUCCESS) {
            sprintf_s(debugMsg, sizeof(debugMsg), "NTP synchronization successful with server: %s", NTP_SERVERS[i]);
            OutputDebugStringA(debugMsg);
            return NTP_SUCCESS;
        }
        
        // 记录失败原因
        const char* errorMsg = "Unknown error";
        switch (result) {
            case NTP_ERROR_NETWORK: errorMsg = "Network error"; break;
            case NTP_ERROR_TIMEOUT: errorMsg = "Timeout"; break;
            case NTP_ERROR_INVALID_RESPONSE: errorMsg = "Invalid response"; break;
        }
        
        sprintf_s(debugMsg, sizeof(debugMsg), "NTP server %s failed: %s", NTP_SERVERS[i], errorMsg);
        OutputDebugStringA(debugMsg);
    }
    
    OutputDebugStringA("All NTP servers failed, using local time as fallback");
    
    // 所有服务器都失败，使用本地时间作为备用
    *timestamp = 0;
    return NTP_ERROR_ALL_SERVERS_FAILED;
}

// 获取本地时间戳（备用方案）
timestamp_t __cdecl GetLocalTimestamp() {
    return (timestamp_t)time(NULL);
}
