#pragma once

#ifdef __cplusplus
extern "C" {
#endif

// 硬件ID获取函数声明

/// <summary>
/// 获取Windows产品ID
/// </summary>
/// <param name="windowsId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
__declspec(dllexport) int GetWindowsID(char* windowsId, int maxLen);

/// <summary>
/// 获取硬盘序列号
/// </summary>
/// <param name="diskId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
__declspec(dllexport) int GetHardDiskID(char* diskId, int maxLen);

/// <summary>
/// 获取网卡MAC地址
/// </summary>
/// <param name="macAddr">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=内存分配失败, -3=未找到合适网卡</returns>
__declspec(dllexport) int GetMACAddress(char* macAddr, int maxLen);

/// <summary>
/// 获取主板序列号
/// </summary>
/// <param name="motherboardId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
__declspec(dllexport) int GetMotherboardID(char* motherboardId, int maxLen);

/// <summary>
/// 获取CPU ID
/// </summary>
/// <param name="cpuId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
__declspec(dllexport) int GetCPUID(char* cpuId, int maxLen);

/// <summary>
/// 获取BIOS序列号
/// </summary>
/// <param name="biosId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
__declspec(dllexport) int GetBIOSID(char* biosId, int maxLen);

/// <summary>
/// 获取系统UUID
/// </summary>
/// <param name="systemUuid">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
__declspec(dllexport) int GetSystemUUID(char* systemUuid, int maxLen);

/// <summary>
/// 生成机器指纹（组合多个硬件ID）
/// </summary>
/// <param name="fingerprint">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
__declspec(dllexport) int GenerateMachineFingerprint(char* fingerprint, int maxLen);

/// <summary>
/// 获取详细硬件信息
/// </summary>
/// <param name="info">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
__declspec(dllexport) int GetHardwareInfo(char* info, int maxLen);

#ifdef __cplusplus
}
#endif 