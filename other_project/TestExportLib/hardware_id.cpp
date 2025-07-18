#include "pch.h"
#include <windows.h>
#include <wbemidl.h>
#include <comdef.h>
#include <Wbemidl.h>
#include <iphlpapi.h>
#include <intrin.h>
#include <winioctl.h>
#include <stdio.h>
#include <string.h>

#pragma comment(lib, "wbemuuid.lib")
#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "advapi32.lib")

// 初始化COM和WMI
class WMIHelper {
private:
    IWbemLocator* pLoc;
    IWbemServices* pSvc;
    bool initialized;
    bool comInitialized; // 标记是否由我们初始化COM

public:
    WMIHelper() : pLoc(nullptr), pSvc(nullptr), initialized(false), comInitialized(false) {
        Initialize();
    }

    ~WMIHelper() {
        Cleanup();
    }

    bool Initialize() {
        HRESULT hres;

        // 尝试初始化COM库，但如果已经初始化则继续
        hres = CoInitializeEx(0, COINIT_MULTITHREADED);
        if (SUCCEEDED(hres)) {
            comInitialized = true; // 我们成功初始化了COM
        } else if (hres == RPC_E_CHANGED_MODE) {
            // COM已经以不同模式初始化，尝试单线程模式
            hres = CoInitializeEx(0, COINIT_APARTMENTTHREADED);
            if (SUCCEEDED(hres)) {
                comInitialized = true;
            } else {
                // COM可能已经初始化，继续尝试使用现有状态
                comInitialized = false;
            }
        } else {
            // COM可能已经初始化，继续尝试使用现有状态
            comInitialized = false;
        }

        // 尝试设置COM安全级别（可能失败，但不是致命的）
        CoInitializeSecurity(
            NULL,
            -1,                          // COM认证
            NULL,                        // 认证服务
            NULL,                        // 保留
            RPC_C_AUTHN_LEVEL_NONE,     // 默认认证
            RPC_C_IMP_LEVEL_IMPERSONATE, // 默认模拟
            NULL,                        // 认证信息
            EOAC_NONE,                   // 附加功能
            NULL                         // 保留
        );

        // 创建WMI定位器
        hres = CoCreateInstance(
            CLSID_WbemLocator,
            0,
            CLSCTX_INPROC_SERVER,
            IID_IWbemLocator, (LPVOID*)&pLoc);

        if (FAILED(hres)) {
            if (comInitialized) {
                CoUninitialize();
                comInitialized = false;
            }
            return false;
        }

        // 连接到WMI
        hres = pLoc->ConnectServer(
            _bstr_t(L"ROOT\\CIMV2"), // WMI命名空间
            NULL,                    // 用户名
            NULL,                    // 用户密码
            0,                       // 语言环境
            NULL,                    // 安全标志
            0,                       // 权限
            0,                       // 上下文对象
            &pSvc                    // 指向IWbemServices代理的指针
        );

        if (FAILED(hres)) {
            pLoc->Release();
            pLoc = nullptr;
            if (comInitialized) {
                CoUninitialize();
                comInitialized = false;
            }
            return false;
        }

        // 设置代理安全级别
        hres = CoSetProxyBlanket(
            pSvc,                        // 指示代理进行设置
            RPC_C_AUTHN_WINNT,          // RPC_C_AUTHN_xxx
            RPC_C_AUTHZ_NONE,           // RPC_C_AUTHZ_xxx
            NULL,                        // 服务器主体名称
            RPC_C_AUTHN_LEVEL_CALL,     // RPC_C_AUTHN_LEVEL_xxx
            RPC_C_IMP_LEVEL_IMPERSONATE, // RPC_C_IMP_LEVEL_xxx
            NULL,                        // 客户端身份
            EOAC_NONE                    // 代理功能
        );

        if (FAILED(hres)) {
            pSvc->Release();
            pSvc = nullptr;
            pLoc->Release();
            pLoc = nullptr;
            if (comInitialized) {
                CoUninitialize();
                comInitialized = false;
            }
            return false;
        }

        initialized = true;
        return true;
    }

    void Cleanup() {
        if (pSvc) {
            pSvc->Release();
            pSvc = nullptr;
        }
        if (pLoc) {
            pLoc->Release();
            pLoc = nullptr;
        }
        // 只有当我们初始化COM时才调用CoUninitialize
        if (comInitialized) {
            CoUninitialize();
            comInitialized = false;
        }
        initialized = false;
    }

    bool IsInitialized() const { return initialized; }
    IWbemServices* GetService() const { return pSvc; }
};

// 从WMI查询中获取字符串值
bool GetWMIStringValue(IWbemServices* pSvc, const wchar_t* query, const wchar_t* property, char* result, int maxLen) {
    if (!pSvc || !query || !property || !result) return false;

    IEnumWbemClassObject* pEnumerator = NULL;
    HRESULT hres = pSvc->ExecQuery(
        bstr_t("WQL"),
        bstr_t(query),
        WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY,
        NULL,
        &pEnumerator);

    if (FAILED(hres)) {
        return false;
    }

    IWbemClassObject* pclsObj = NULL;
    ULONG uReturn = 0;
    bool success = false;

    while (pEnumerator) {
        HRESULT hr = pEnumerator->Next(WBEM_INFINITE, 1, &pclsObj, &uReturn);
        if (0 == uReturn) break;

        VARIANT vtProp;
        hr = pclsObj->Get(property, 0, &vtProp, 0, 0);
        if (SUCCEEDED(hr) && vtProp.vt == VT_BSTR && vtProp.bstrVal) {
            // 转换为多字节字符串
            int len = WideCharToMultiByte(CP_UTF8, 0, vtProp.bstrVal, -1, result, maxLen, NULL, NULL);
            if (len > 0) {
                success = true;
            }
        }
        VariantClear(&vtProp);
        pclsObj->Release();
        break; // 只取第一个结果
    }

    pEnumerator->Release();
    return success;
}

// 获取Windows产品ID
 int GetWindowsID(char* windowsId, int maxLen) {
    if (!windowsId || maxLen <= 0) return -1;

    WMIHelper wmi;
    if (!wmi.IsInitialized()) return -2;

    // 查询Windows产品ID
    const wchar_t* query = L"SELECT ProductId FROM Win32_OperatingSystem";
    if (GetWMIStringValue(wmi.GetService(), query, L"ProductId", windowsId, maxLen)) {
        return 0; // 成功
    }

    return -3; // 查询失败
}

// 获取硬盘序列号
 int GetHardDiskID(char* diskId, int maxLen) {
    if (!diskId || maxLen <= 0) return -1;

    WMIHelper wmi;
    if (!wmi.IsInitialized()) return -2;

    // 查询第一个物理硬盘的序列号
    const wchar_t* query = L"SELECT SerialNumber FROM Win32_PhysicalMedia WHERE Tag LIKE '%PHYSICALDRIVE0%'";
    if (GetWMIStringValue(wmi.GetService(), query, L"SerialNumber", diskId, maxLen)) {
        // 移除空格
        char* src = diskId;
        char* dst = diskId;
        while (*src) {
            if (*src != ' ') {
                *dst++ = *src;
            }
            src++;
        }
        *dst = '\0';
        return 0; // 成功
    }

    return -3; // 查询失败
}

// 获取网卡MAC地址
int GetMACAddress(char* macAddr, int maxLen) {
    if (!macAddr || maxLen <= 0) return -1;

    ULONG outBufLen = sizeof(IP_ADAPTER_INFO);
    PIP_ADAPTER_INFO pAdapterInfo = (IP_ADAPTER_INFO*)malloc(sizeof(IP_ADAPTER_INFO));
    if (pAdapterInfo == NULL) return -2;

    // 获取适配器信息
    if (GetAdaptersInfo(pAdapterInfo, &outBufLen) == ERROR_BUFFER_OVERFLOW) {
        free(pAdapterInfo);
        pAdapterInfo = (IP_ADAPTER_INFO*)malloc(outBufLen);
        if (pAdapterInfo == NULL) return -2;
    }

    DWORD dwRetVal = GetAdaptersInfo(pAdapterInfo, &outBufLen);
    if (dwRetVal == NO_ERROR) {
        char bestMac[64] = {0};
        bool foundMac = false;
        
        PIP_ADAPTER_INFO pAdapter = pAdapterInfo;
        while (pAdapter) {
            // 接受任何以太网或WiFi网卡，包括虚拟网卡
            if (pAdapter->Type == MIB_IF_TYPE_ETHERNET || pAdapter->Type == IF_TYPE_IEEE80211) {
                char tempMac[64];
                sprintf_s(tempMac, sizeof(tempMac), "%02X-%02X-%02X-%02X-%02X-%02X",
                    pAdapter->Address[0], pAdapter->Address[1],
                    pAdapter->Address[2], pAdapter->Address[3],
                    pAdapter->Address[4], pAdapter->Address[5]);
                
                // 只跳过全零MAC地址
                if (strncmp(tempMac, "00-00-00-00-00-00", 17) != 0) {
                    strcpy_s(bestMac, sizeof(bestMac), tempMac);
                    foundMac = true;
                    
                    // 如果找到物理网卡（非虚拟），优先使用
                    if (pAdapter->Type == MIB_IF_TYPE_ETHERNET &&
                        strncmp(tempMac, "00-50-56", 8) != 0 && 
                        strncmp(tempMac, "08-00-27", 8) != 0) {
                        strcpy_s(macAddr, maxLen, tempMac);
                        free(pAdapterInfo);
                        return 0; // 找到物理网卡，直接返回
                    }
                }
            }
            pAdapter = pAdapter->Next;
        }
        
        // 如果没找到物理网卡，使用任何可用的MAC地址
        if (foundMac) {
            strcpy_s(macAddr, maxLen, bestMac);
            free(pAdapterInfo);
            return 0;
        }
    }

    free(pAdapterInfo);
    return -3; // 未找到任何网卡
}

// 获取主板序列号
 int GetMotherboardID(char* motherboardId, int maxLen) {
    if (!motherboardId || maxLen <= 0) return -1;

    WMIHelper wmi;
    if (!wmi.IsInitialized()) return -2;

    // 查询主板序列号
    const wchar_t* query = L"SELECT SerialNumber FROM Win32_BaseBoard";
    if (GetWMIStringValue(wmi.GetService(), query, L"SerialNumber", motherboardId, maxLen)) {
        return 0; // 成功
    }

    return -3; // 查询失败
}

// 获取CPU ID
int GetCPUID(char* cpuId, int maxLen) {
    if (!cpuId || maxLen <= 0) return -1;

    // 使用CPUID指令获取稳定的CPU特征值
    int cpuInfo1[4] = { 0 };  // CPUID(1) - 处理器信息和特征位
    int cpuInfo0[4] = { 0 };  // CPUID(0) - 厂商信息
    
    __cpuid(cpuInfo0, 0);     // 获取厂商ID
    __cpuid(cpuInfo1, 1);     // 获取处理器签名和特征
    
    // 只使用最稳定的CPU特征值，避免不确定因素
    // 使用: 厂商ID + 处理器签名 + 稳定特征位
    sprintf_s(cpuId, maxLen, "%08X-%08X-%08X-%08X",
        cpuInfo0[1],     // 厂商ID前4字节 ("Genu" for Intel, "Auth" for AMD)
        cpuInfo0[3],     // 厂商ID后4字节 ("ineI" for Intel, "enti" for AMD)
        cpuInfo1[0],     // 处理器签名(EAX) - 包含型号、家族、步进
        cpuInfo1[3]);    // 特征标志位(EDX) - 最稳定的特征位
        
    return 0; // 成功
}

// 获取BIOS序列号
int GetBIOSID(char* biosId, int maxLen) {
    if (!biosId || maxLen <= 0) return -1;

    WMIHelper wmi;
    if (!wmi.IsInitialized()) return -2;

    // 查询BIOS序列号
    const wchar_t* query = L"SELECT SerialNumber FROM Win32_BIOS";
    if (GetWMIStringValue(wmi.GetService(), query, L"SerialNumber", biosId, maxLen)) {
        return 0; // 成功
    }

    return -3; // 查询失败
}

// 获取系统UUID
 int GetSystemUUID(char* systemUuid, int maxLen) {
    if (!systemUuid || maxLen <= 0) return -1;

    WMIHelper wmi;
    if (!wmi.IsInitialized()) return -2;

    // 查询系统UUID
    const wchar_t* query = L"SELECT UUID FROM Win32_ComputerSystemProduct";
    if (GetWMIStringValue(wmi.GetService(), query, L"UUID", systemUuid, maxLen)) {
        return 0; // 成功
    }

    return -3; // 查询失败
}

// 生成机器指纹（基于多个硬件标识符）- 修复唯一性问题
int GenerateMachineFingerprint(char* fingerprint, int maxLen) {
    if (!fingerprint || maxLen <= 0) return -1;

    char cpuId[256] = { 0 };
    char macAddr[256] = { 0 };
    char diskId[256] = { 0 };
    char motherboardId[256] = { 0 };
    char biosId[256] = { 0 };
    char systemUuid[256] = { 0 };

    // 首先获取不需要COM的硬件信息
    GetCPUID(cpuId, sizeof(cpuId));
    GetMACAddress(macAddr, sizeof(macAddr));

    // 使用独立的作用域确保WMI资源立即释放
    {
        WMIHelper wmi;
        if (wmi.IsInitialized()) {
            IWbemServices* pSvc = wmi.GetService();
            
            // 快速批量获取所有WMI信息
            // 获取硬盘序列号
            const wchar_t* diskQuery = L"SELECT SerialNumber FROM Win32_PhysicalMedia WHERE Tag LIKE '%PHYSICALDRIVE0%'";
            if (GetWMIStringValue(pSvc, diskQuery, L"SerialNumber", diskId, sizeof(diskId))) {
                // 移除空格
                char* src = diskId;
                char* dst = diskId;
                while (*src) {
                    if (*src != ' ') {
                        *dst++ = *src;
                    }
                    src++;
                }
                *dst = '\0';
            }
            
            // 获取主板序列号
            const wchar_t* mbQuery = L"SELECT SerialNumber FROM Win32_BaseBoard";
            GetWMIStringValue(pSvc, mbQuery, L"SerialNumber", motherboardId, sizeof(motherboardId));
            
            // 获取BIOS序列号
            const wchar_t* biosQuery = L"SELECT SerialNumber FROM Win32_BIOS";
            GetWMIStringValue(pSvc, biosQuery, L"SerialNumber", biosId, sizeof(biosId));
            
            // 获取系统UUID
            const wchar_t* uuidQuery = L"SELECT UUID FROM Win32_ComputerSystemProduct";
            GetWMIStringValue(pSvc, uuidQuery, L"UUID", systemUuid, sizeof(systemUuid));
        }
        // WMIHelper析构函数会在这里自动调用，完全清理COM资源
    }

    // 强制COM清理（额外保险）
    CoFreeUnusedLibraries();

    // 至少需要有一个有效的硬件标识符
    if (strlen(cpuId) == 0 && strlen(macAddr) == 0 && strlen(diskId) == 0 && 
        strlen(motherboardId) == 0 && strlen(biosId) == 0 && strlen(systemUuid) == 0) {
        return -3; // 没有获取到任何硬件标识符
    }

    // 组合多个硬件标识符（只使用成功获取的）
    char combined[2048] = { 0 };
    
    if (strlen(cpuId) > 0) {
        strcat_s(combined, sizeof(combined), "CPU:");
        strcat_s(combined, sizeof(combined), cpuId);
        strcat_s(combined, sizeof(combined), ";");
    }
    
    if (strlen(macAddr) > 0) {
        strcat_s(combined, sizeof(combined), "MAC:");
        strcat_s(combined, sizeof(combined), macAddr);
        strcat_s(combined, sizeof(combined), ";");
    }
    
    if (strlen(diskId) > 0) {
        strcat_s(combined, sizeof(combined), "DISK:");
        strcat_s(combined, sizeof(combined), diskId);
        strcat_s(combined, sizeof(combined), ";");
    }
    
    if (strlen(motherboardId) > 0) {
        strcat_s(combined, sizeof(combined), "MB:");
        strcat_s(combined, sizeof(combined), motherboardId);
        strcat_s(combined, sizeof(combined), ";");
    }
    
    if (strlen(biosId) > 0) {
        strcat_s(combined, sizeof(combined), "BIOS:");
        strcat_s(combined, sizeof(combined), biosId);
        strcat_s(combined, sizeof(combined), ";");
    }
    
    if (strlen(systemUuid) > 0) {
        strcat_s(combined, sizeof(combined), "UUID:");
        strcat_s(combined, sizeof(combined), systemUuid);
        strcat_s(combined, sizeof(combined), ";");
    }

    // 如果组合字符串为空，使用当前时间戳作为备选
    if (strlen(combined) == 0) {
        sprintf_s(combined, sizeof(combined), "FALLBACK:%lld", 
            (long long)GetTickCount64());
    }

    // 使用改进的哈希算法 - 提高唯一性
    unsigned long long hash1 = 0x9e3779b97f4a7c15ULL; // 种子1
    unsigned long long hash2 = 0x517cc1b727220a95ULL; // 种子2
    
    // 双重哈希算法
    int len = strlen(combined);
    for (int i = 0; i < len; i++) {
        // 第一个哈希 - DJB2变种
        hash1 = ((hash1 << 5) + hash1) + (unsigned char)combined[i];
        // 第二个哈希 - SDBM变种
        hash2 = (unsigned char)combined[i] + (hash2 << 6) + (hash2 << 16) - hash2;
    }
    
    // 混合两个哈希值
    hash1 ^= (hash1 >> 32);
    hash1 *= 0x9e3779b97f4a7c15ULL;
    hash1 ^= (hash1 >> 32);
    
    hash2 ^= (hash2 >> 32);
    hash2 *= 0x517cc1b727220a95ULL;
    hash2 ^= (hash2 >> 32);
    
    // 组合两个哈希值
    unsigned long long finalHash = hash1 ^ hash2;

    // 生成稳定的指纹格式：XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    sprintf_s(fingerprint, maxLen, "%08X-%04X-%04X-%04X-%08X%04X",
        (unsigned int)(finalHash & 0xFFFFFFFF),
        (unsigned short)((finalHash >> 32) & 0xFFFF),
        (unsigned short)((finalHash >> 16) & 0xFFFF),
        (unsigned short)((finalHash >> 48) & 0xFFFF),
        (unsigned int)((hash1 >> 8) & 0xFFFFFFFF),
        (unsigned short)(hash2 & 0xFFFF));

    return 0; // 成功
}

// 获取详细硬件信息
int GetHardwareInfo(char* info, int maxLen) {
    if (!info || maxLen <= 0) return -1;

    char windowsId[256] = { 0 };
    char diskId[256] = { 0 };
    char macAddr[256] = { 0 };
    char motherboardId[256] = { 0 };
    char cpuId[256] = { 0 };
    char biosId[256] = { 0 };
    char systemUuid[256] = { 0 };

    // 获取所有硬件ID
    GetWindowsID(windowsId, sizeof(windowsId));
    GetHardDiskID(diskId, sizeof(diskId));
    GetMACAddress(macAddr, sizeof(macAddr));
    GetMotherboardID(motherboardId, sizeof(motherboardId));
    GetCPUID(cpuId, sizeof(cpuId));
    GetBIOSID(biosId, sizeof(biosId));
    GetSystemUUID(systemUuid, sizeof(systemUuid));

    // 格式化输出
    sprintf_s(info, maxLen,
        "Windows ID: %s\n"
        "Hard Disk ID: %s\n"
        "MAC Address: %s\n"
        "Motherboard ID: %s\n"
        "CPU ID: %s\n"
        "BIOS ID: %s\n"
        "System UUID: %s\n",
        windowsId[0] ? windowsId : "N/A",
        diskId[0] ? diskId : "N/A",
        macAddr[0] ? macAddr : "N/A",
        motherboardId[0] ? motherboardId : "N/A",
        cpuId[0] ? cpuId : "N/A",
        biosId[0] ? biosId : "N/A",
        systemUuid[0] ? systemUuid : "N/A");

    return 0; // 成功
}

// 生成机器指纹（C#友好版本）- 无需长度参数
extern "C" __declspec(dllexport) int GetMachineFingerprint(const char* data) {
    if (!data) return -1;

    // 更安全的方式：先在栈上生成指纹，再复制到输出缓冲区
    char tempBuffer[256] = {0};
    
    // 调用现有的GenerateMachineFingerprint函数
    int result = GenerateMachineFingerprint(tempBuffer, sizeof(tempBuffer));
    
    if (result == 0) {
        // 成功生成指纹，复制到输出缓冲区
        // 注意：这里假设C#传递的缓冲区至少有256字节
        strcpy_s(const_cast<char*>(data), 256, tempBuffer);
    }
    
    return result;
}

// 生成机器指纹（调试版本）- 显示详细硬件信息获取状态
extern "C" __declspec(dllexport) int GetMachineFingerprintDebug(const char* fingerprint, const char* debugInfo, int maxLen) {
    if (!fingerprint || !debugInfo || maxLen <= 0) return -1;

    char cpuId[256] = { 0 };
    char debug[1024] = { 0 };

    // 只检查CPU ID获取状态
    int cpuResult = GetCPUID(cpuId, sizeof(cpuId));
    if (cpuResult == 0 && strlen(cpuId) > 0) {
        sprintf_s(debug, sizeof(debug), "CPU ID: OK (%s)\nStatus: SUCCESS - Device fingerprint generated based on CPU ID", cpuId);
    } else {
        sprintf_s(debug, sizeof(debug), "CPU ID: FAILED (code: %d)\nStatus: ERROR - Cannot generate device fingerprint", cpuResult);
    }

    // 复制调试信息
    strcpy_s(const_cast<char*>(debugInfo), maxLen, debug);

    // 生成指纹
    if (cpuResult != 0 || strlen(cpuId) == 0) {
        strcpy_s(const_cast<char*>(fingerprint), maxLen, "ERROR-NO-CPU-ID");
        return -3;
    }

    // 调用原始指纹生成逻辑
    char tempFingerprint[256] = {0};
    int result = GenerateMachineFingerprint(tempFingerprint, sizeof(tempFingerprint));
    
    if (result == 0) {
        strcpy_s(const_cast<char*>(fingerprint), maxLen, tempFingerprint);
    }
    
    return result;
} 