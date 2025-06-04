#include "pch.h"
#include "ntp.h"
#include <winsock2.h>
#include <ws2tcpip.h>
#include <time.h>
#include <vector>
#include <string>

#pragma comment(lib, "ws2_32.lib")

#define _WINSOCK_DEPRECATED_NO_WARNINGS

// NTP协议相关常量定义
#define NTP_PORT 123                       // NTP标准端口号
#define NTP_PACKET_SIZE 48                 // NTP数据包标准大小

// x86架构兼容的常量定义
#ifdef _WIN64
    #define NTP_UNIX_EPOCH_OFFSET 2208988800ULL    // 1900年到1970年的秒数差
#else
    #define NTP_UNIX_EPOCH_OFFSET 2208988800UI64   // x86版本的64位常量
#endif

// x86架构兼容的64位整数联合体类型
typedef union {
    timestamp_t value;      // 完整的64位值
    struct {
        uint32_t low;       // 低32位
        uint32_t high;      // 高32位
    } parts;
} uint64_union_t;

// NTP数据包结构定义 - 使用字节数组确保跨平台兼容性
#pragma pack(push, 1)
struct NTPPacket {
    unsigned char li_vn_mode;               // LI(2位) + VN(3位) + Mode(3位)
    unsigned char stratum;                  // 时间层级
    unsigned char poll;                     // 轮询间隔
    unsigned char precision;                // 时钟精度
    uint32_t root_delay;                   // 根延迟
    uint32_t root_dispersion;              // 根分散度
    uint32_t reference_id;                 // 参考时钟标识符
    unsigned char reference_timestamp[8];   // 参考时间戳（8字节）
    unsigned char origin_timestamp[8];      // 起始时间戳（8字节）
    unsigned char receive_timestamp[8];     // 接收时间戳（8字节）
    unsigned char transmit_timestamp[8];    // 传输时间戳（8字节）
};
#pragma pack(pop)

// 全球NTP时间服务器列表（按地理位置优化排序）
static const char* NTP_SERVERS[] = {
    // 亚洲地区优先（适合中国用户）
    "cn.pool.ntp.org",          // 中国NTP池
    "asia.pool.ntp.org",        // 亚洲NTP池
    "hk.pool.ntp.org",          // 香港NTP池
    "tw.pool.ntp.org",          // 台湾NTP池
    "jp.pool.ntp.org",          // 日本NTP池
    "kr.pool.ntp.org",          // 韩国NTP池
    "sg.pool.ntp.org",          // 新加坡NTP池
    
    // 全球通用服务器
    "pool.ntp.org",             // 全球NTP池
    "time.windows.com",         // 微软时间服务器
    "time.nist.gov",            // 美国国家标准时间
    
    // 欧洲地区
    "europe.pool.ntp.org",      // 欧洲NTP池
    "de.pool.ntp.org",          // 德国NTP池
    "uk.pool.ntp.org",          // 英国NTP池
    "fr.pool.ntp.org",          // 法国NTP池
    
    // 北美地区
    "north-america.pool.ntp.org", // 北美NTP池
    "us.pool.ntp.org",          // 美国NTP池
    "ca.pool.ntp.org",          // 加拿大NTP池
    
    // 其他地区
    "oceania.pool.ntp.org",     // 大洋洲NTP池
    "au.pool.ntp.org",          // 澳大利亚NTP池
    "south-america.pool.ntp.org", // 南美NTP池
    "br.pool.ntp.org",          // 巴西NTP池
    "africa.pool.ntp.org"       // 非洲NTP池
};

// x86兼容的64位网络字节序转换函数 - 优化版本
timestamp_t ntohl64_fixed(unsigned char* data) {
    // NTP时间戳格式：前32位是秒数，后32位是小数部分
    uint32_t seconds = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
    // 只返回秒数部分，忽略小数部分以简化处理
    return (timestamp_t)seconds;
}

void htonl64_fixed(timestamp_t host_value, unsigned char* data) {
    // 将主机字节序的时间戳转换为网络字节序
    uint32_t seconds = (uint32_t)host_value;
    data[0] = (seconds >> 24) & 0xFF;   // 最高字节
    data[1] = (seconds >> 16) & 0xFF;   // 次高字节
    data[2] = (seconds >> 8) & 0xFF;    // 次低字节
    data[3] = seconds & 0xFF;           // 最低字节
    // 小数部分设置为0（简化处理）
    data[4] = data[5] = data[6] = data[7] = 0;
}

// Windows网络套接字初始化
BOOL InitializeWinsock() {
    WSADATA wsaData;
    return WSAStartup(MAKEWORD(2, 2), &wsaData) == 0;
}

// 清理Windows网络套接字资源
void CleanupWinsock() {
    WSACleanup();
}

// 获取当前系统时间的NTP格式时间戳 - x86兼容版本
timestamp_t GetCurrentNTPTime() {
    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);
    
    // x86兼容的64位时间计算方式
    uint64_union_t time64;
    time64.parts.low = ft.dwLowDateTime;    // 文件时间低32位
    time64.parts.high = ft.dwHighDateTime;  // 文件时间高32位
    
    // 转换为Unix时间戳（秒为单位）
    // Windows FILETIME从1601年开始，Unix时间从1970年开始
    timestamp_t unixTime = (time64.value / 10000000) - 11644473600LL; // 1601年到1970年的秒数差
    
    // 转换为NTP时间戳（从1900年开始计算）
    return unixTime + NTP_UNIX_EPOCH_OFFSET;
}

// 从指定NTP服务器获取网络时间戳
int __cdecl GetNTPTimestampFromServer(const char* server, timestamp_t* timestamp, int timeoutMs) {
    if (!timestamp) {
        return NTP_ERROR_INVALID_RESPONSE;
    }

    SOCKET sock = INVALID_SOCKET;
    struct sockaddr_in serverAddr;
    NTPPacket packet;
    int result = NTP_ERROR_NETWORK;

    // 初始化Windows网络套接字
    if (!InitializeWinsock()) {
        //OutputDebugStringA("Winsock初始化失败");
        return NTP_ERROR_NETWORK;
    }

    do {
        // 创建UDP网络套接字
        sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
        if (sock == INVALID_SOCKET) {
            //OutputDebugStringA("网络套接字创建失败");
            break;
        }

        // 设置网络超时时间
        DWORD timeout = (DWORD)timeoutMs;
        setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, (char*)&timeout, sizeof(timeout));
        setsockopt(sock, SOL_SOCKET, SO_SNDTIMEO, (char*)&timeout, sizeof(timeout));

        // 解析NTP服务器网络地址
        ZeroMemory(&serverAddr, sizeof(serverAddr));
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(NTP_PORT);
        
        // 尝试直接解析IP地址
        serverAddr.sin_addr.s_addr = inet_addr(server);
        if (serverAddr.sin_addr.s_addr == INADDR_NONE) {
            // 如果不是纯IP地址，则进行DNS域名解析
            struct hostent* host = gethostbyname(server);
            if (!host) {
                char debugMsg[256];
                sprintf_s(debugMsg, sizeof(debugMsg), "域名解析失败: %s", server);
                //OutputDebugStringA(debugMsg);
                result = NTP_ERROR_NETWORK;
                break;
            }
            CopyMemory(&serverAddr.sin_addr, host->h_addr_list[0], host->h_length);
        }

        // 准备NTP请求数据包
        ZeroMemory(&packet, sizeof(packet));
        packet.li_vn_mode = 0x1B; // LI=0, VN=3, Mode=3 (客户端模式)
        
        // 设置传输时间戳到数据包
        timestamp_t currentTime = GetCurrentNTPTime();
        htonl64_fixed(currentTime, packet.transmit_timestamp);

        // 发送NTP时间请求到服务器
        if (sendto(sock, (char*)&packet, sizeof(packet), 0, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
            //OutputDebugStringA("NTP请求发送失败");
            result = NTP_ERROR_NETWORK;
            break;
        }

        // 接收NTP服务器响应数据
        struct sockaddr_in fromAddr;
        int fromLen = sizeof(fromAddr);
        int bytesReceived = recvfrom(sock, (char*)&packet, sizeof(packet), 0, (struct sockaddr*)&fromAddr, &fromLen);
        
        if (bytesReceived == SOCKET_ERROR) {
            int error = WSAGetLastError();
            if (error == WSAETIMEDOUT) {
                //OutputDebugStringA("NTP请求超时");
                result = NTP_ERROR_TIMEOUT;
            } else {
                //OutputDebugStringA("NTP响应接收失败");
                result = NTP_ERROR_NETWORK;
            }
            break;
        }

        if (bytesReceived != sizeof(packet)) {
            //OutputDebugStringA("NTP响应数据包大小无效");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        // 验证NTP响应数据包的有效性
        if ((packet.li_vn_mode & 0x07) != 4) { // Mode字段应该是4（服务器模式）
            //OutputDebugStringA("NTP响应模式无效");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        // 提取服务器传输的时间戳 - 使用transmit_timestamp字段
        timestamp_t serverTime = ntohl64_fixed(packet.transmit_timestamp);
        
        // 验证时间戳的合理性
        if (serverTime == 0) {
            //OutputDebugStringA("服务器返回零时间戳");
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }
        
        // 转换NTP时间戳为标准Unix时间戳
        if (serverTime < NTP_UNIX_EPOCH_OFFSET) {
            char debugMsg[256];
            sprintf_s(debugMsg, sizeof(debugMsg), "NTP时间戳无效: %I64d (数值过小)", serverTime);
            //OutputDebugStringA(debugMsg);
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }

        *timestamp = serverTime - NTP_UNIX_EPOCH_OFFSET;
        
        // 验证转换后Unix时间戳的合理性（应该在2000年之后）
        if (*timestamp < 946684800) { // 2000年1月1日的Unix时间戳
            char debugMsg[256];
            sprintf_s(debugMsg, sizeof(debugMsg), "转换后时间戳过旧: %I64d", *timestamp);
            //OutputDebugStringA(debugMsg);
            result = NTP_ERROR_INVALID_RESPONSE;
            break;
        }
        
        char debugMsg[256];
        sprintf_s(debugMsg, sizeof(debugMsg), "NTP同步成功 服务器: %s, NTP时间: %I64d, Unix时间戳: %I64d", server, serverTime, *timestamp);
        //OutputDebugStringA(debugMsg);
        
        result = NTP_SUCCESS;

    } while (FALSE);

    // 清理网络资源
    if (sock != INVALID_SOCKET) {
        closesocket(sock);
    }
    CleanupWinsock();

    return result;
}

// 轮询多个NTP服务器获取网络同步时间戳（智能故障转移）
int __cdecl GetNTPTimestamp(timestamp_t* timestamp) {
    if (!timestamp) {
        return NTP_ERROR_INVALID_RESPONSE;
    }

    const int serverCount = sizeof(NTP_SERVERS) / sizeof(NTP_SERVERS[0]);
    
    //OutputDebugStringA("开始NTP网络时间同步...");
    
    // 按优先级轮询服务器列表
    for (int i = 0; i < serverCount; i++) {
        char debugMsg[256];
        sprintf_s(debugMsg, sizeof(debugMsg), "尝试NTP服务器: %s (%d/%d)", NTP_SERVERS[i], i + 1, serverCount);
        //OutputDebugStringA(debugMsg);
        
        int result = GetNTPTimestampFromServer(NTP_SERVERS[i], timestamp, 3000); // 3秒超时
        
        if (result == NTP_SUCCESS) {
            sprintf_s(debugMsg, sizeof(debugMsg), "NTP时间同步成功，服务器: %s", NTP_SERVERS[i]);
            //OutputDebugStringA(debugMsg);
            return NTP_SUCCESS;
        }
        
        // 记录失败原因用于调试
        const char* errorMsg = "未知错误";
        switch (result) {
            case NTP_ERROR_NETWORK: errorMsg = "网络错误"; break;
            case NTP_ERROR_TIMEOUT: errorMsg = "请求超时"; break;
            case NTP_ERROR_INVALID_RESPONSE: errorMsg = "无效响应"; break;
        }
        
        sprintf_s(debugMsg, sizeof(debugMsg), "NTP服务器 %s 失败: %s", NTP_SERVERS[i], errorMsg);
        //OutputDebugStringA(debugMsg);
    }
    
    //OutputDebugStringA("所有NTP服务器均失败，使用本地系统时间作为备用方案");
    
    // 所有NTP服务器都失败时，使用本地时间作为备用方案
    *timestamp = GetLocalTimestamp();
    
    char debugMsg[256];
    sprintf_s(debugMsg, sizeof(debugMsg), "使用本地时间戳作为备用: %I64d", *timestamp);
    //OutputDebugStringA(debugMsg);
    
    return NTP_ERROR_ALL_SERVERS_FAILED;
}

// 获取本地系统时间戳（作为NTP失败时的备用方案）
timestamp_t __cdecl GetLocalTimestamp() {
    return (timestamp_t)time(NULL);
}
