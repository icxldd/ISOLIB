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
int GetWindowsID(char* windowsId, int maxLen);

/// <summary>
/// 获取硬盘序列号
/// </summary>
/// <param name="diskId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
int GetHardDiskID(char* diskId, int maxLen);

/// <summary>
/// 获取网卡MAC地址
/// </summary>
/// <param name="macAddr">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=内存分配失败, -3=未找到合适网卡</returns>
int GetMACAddress(char* macAddr, int maxLen);

/// <summary>
/// 获取主板序列号
/// </summary>
/// <param name="motherboardId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
int GetMotherboardID(char* motherboardId, int maxLen);

/// <summary>
/// 获取CPU ID
/// </summary>
/// <param name="cpuId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
int GetCPUID(char* cpuId, int maxLen);

/// <summary>
/// 获取BIOS序列号
/// </summary>
/// <param name="biosId">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
int GetBIOSID(char* biosId, int maxLen);

/// <summary>
/// 获取系统UUID
/// </summary>
/// <param name="systemUuid">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误, -2=初始化失败, -3=查询失败</returns>
int GetSystemUUID(char* systemUuid, int maxLen);

/// <summary>
/// 生成机器指纹（组合多个硬件ID）
/// </summary>
/// <param name="fingerprint">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
int GenerateMachineFingerprint(char* fingerprint, int maxLen);

/// <summary>
/// 生成机器指纹（C#友好版本）- 无需长度参数
/// </summary>
/// <param name="data">输出缓冲区（C#字符串）</param>
/// <returns>0=成功, -1=参数错误</returns>
__declspec(dllexport) int GetMachineFingerprint(const char* data);

/// <summary>
/// 获取详细硬件信息
/// </summary>
/// <param name="info">输出缓冲区</param>
/// <param name="maxLen">缓冲区最大长度</param>
/// <returns>0=成功, -1=参数错误</returns>
int GetHardwareInfo(char* info, int maxLen);

#ifdef __cplusplus
}
#endif 