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

public:
    WMIHelper() : pLoc(nullptr), pSvc(nullptr), initialized(false) {
        Initialize();
    }

    ~WMIHelper() {
        Cleanup();
    }

    bool Initialize() {
        HRESULT hres;

        // 初始化COM库
        hres = CoInitializeEx(0, COINIT_MULTITHREADED);
        if (FAILED(hres)) {
            return false;
        }

        // 设置COM安全级别
        hres = CoInitializeSecurity(
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

        if (FAILED(hres)) {
            CoUninitialize();
            return false;
        }

        // 创建WMI定位器
        hres = CoCreateInstance(
            CLSID_WbemLocator,
            0,
            CLSCTX_INPROC_SERVER,
            IID_IWbemLocator, (LPVOID*)&pLoc);

        if (FAILED(hres)) {
            CoUninitialize();
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
            CoUninitialize();
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
            pLoc->Release();
            CoUninitialize();
            return false;
        }

        initialized = true;
        return true;
    }

    void Cleanup() {
        if (pSvc) pSvc->Release();
        if (pLoc) pLoc->Release();
        CoUninitialize();
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
extern "C" __declspec(dllexport) int GetWindowsID(char* windowsId, int maxLen) {
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
extern "C" __declspec(dllexport) int GetHardDiskID(char* diskId, int maxLen) {
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
extern "C" __declspec(dllexport) int GetMACAddress(char* macAddr, int maxLen) {
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
        PIP_ADAPTER_INFO pAdapter = pAdapterInfo;
        while (pAdapter) {
            // 跳过回环接口和非以太网接口
            if (pAdapter->Type == MIB_IF_TYPE_ETHERNET || pAdapter->Type == IF_TYPE_IEEE80211) {
                sprintf_s(macAddr, maxLen, "%02X-%02X-%02X-%02X-%02X-%02X",
                    pAdapter->Address[0], pAdapter->Address[1],
                    pAdapter->Address[2], pAdapter->Address[3],
                    pAdapter->Address[4], pAdapter->Address[5]);
                free(pAdapterInfo);
                return 0; // 成功
            }
            pAdapter = pAdapter->Next;
        }
    }

    free(pAdapterInfo);
    return -3; // 未找到合适的网卡
}

// 获取主板序列号
extern "C" __declspec(dllexport) int GetMotherboardID(char* motherboardId, int maxLen) {
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
extern "C" __declspec(dllexport) int GetCPUID(char* cpuId, int maxLen) {
    if (!cpuId || maxLen <= 0) return -1;

    // 使用CPUID指令获取CPU序列号
    int cpuInfo[4] = { 0 };
    __cpuid(cpuInfo, 1);

    // 格式化CPU ID
    sprintf_s(cpuId, maxLen, "%08X-%08X", cpuInfo[3], cpuInfo[0]);
    return 0; // 成功
}

// 获取BIOS序列号
extern "C" __declspec(dllexport) int GetBIOSID(char* biosId, int maxLen) {
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
extern "C" __declspec(dllexport) int GetSystemUUID(char* systemUuid, int maxLen) {
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

// 生成机器指纹（组合多个硬件ID）
extern "C" __declspec(dllexport) int GenerateMachineFingerprint(char* fingerprint, int maxLen) {
    if (!fingerprint || maxLen <= 0) return -1;

    char windowsId[256] = { 0 };
    char diskId[256] = { 0 };
    char macAddr[256] = { 0 };
    char motherboardId[256] = { 0 };
    char cpuId[256] = { 0 };
    char biosId[256] = { 0 };

    // 获取各种硬件ID
    GetWindowsID(windowsId, sizeof(windowsId));
    GetHardDiskID(diskId, sizeof(diskId));
    GetMACAddress(macAddr, sizeof(macAddr));
    GetMotherboardID(motherboardId, sizeof(motherboardId));
    GetCPUID(cpuId, sizeof(cpuId));
    GetBIOSID(biosId, sizeof(biosId));

    // 组合生成指纹
    char combined[2048];
    sprintf_s(combined, sizeof(combined), "%s|%s|%s|%s|%s|%s",
        windowsId, diskId, macAddr, motherboardId, cpuId, biosId);

    // 计算简单哈希
    unsigned int hash = 0;
    for (int i = 0; combined[i]; i++) {
        hash = hash * 31 + (unsigned char)combined[i];
    }

    // 格式化指纹
    sprintf_s(fingerprint, maxLen, "%08X-%04X-%04X",
        hash, (hash >> 16) & 0xFFFF, hash & 0xFFFF);

    return 0; // 成功
}

// 获取详细硬件信息
extern "C" __declspec(dllexport) int GetHardwareInfo(char* info, int maxLen) {
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