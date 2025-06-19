#include <windows.h>
#include <stdio.h>
#include "hardware_id.h"

// 测试程序 - 演示硬件ID获取功能
int main() {
    printf("=== Windows 硬件ID获取测试程序 ===\n\n");

    char buffer[1024];
    int result;

    // 1. 获取Windows产品ID
    printf("1. Windows产品ID:\n");
    result = GetWindowsID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 2. 获取硬盘序列号
    printf("2. 硬盘序列号:\n");
    result = GetHardDiskID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 3. 获取MAC地址
    printf("3. MAC地址:\n");
    result = GetMACAddress(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 4. 获取主板序列号
    printf("4. 主板序列号:\n");
    result = GetMotherboardID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 5. 获取CPU ID
    printf("5. CPU ID:\n");
    result = GetCPUID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 6. 获取BIOS序列号
    printf("6. BIOS序列号:\n");
    result = GetBIOSID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 7. 获取系统UUID
    printf("7. 系统UUID:\n");
    result = GetSystemUUID(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 8. 生成机器指纹
    printf("8. 机器指纹:\n");
    result = GenerateMachineFingerprint(buffer, sizeof(buffer));
    if (result == 0) {
        printf("   成功: %s\n", buffer);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }
    printf("\n");

    // 9. 获取完整硬件信息
    printf("9. 完整硬件信息:\n");
    char info[2048];
    result = GetHardwareInfo(info, sizeof(info));
    if (result == 0) {
        printf("%s", info);
    } else {
        printf("   失败: 错误码 %d\n", result);
    }

    printf("\n按任意键退出...");
    getchar();

    return 0;
} 