# EncodeLib - 内存DLL加密解密库

EncodeLib是一个封装了TestExportLib.vmp.dll功能的C#库，提供文件加密解密和NTP时间同步服务。该库实现了完全的内存DLL加载，无硬盘痕迹。

## 主要特性

- **内存DLL加载**: 完全从内存加载DLL，无硬盘痕迹
- **文件加密解密**: 支持流式文件加密解密，支持大文件处理
- **密钥验证**: 支持加密文件密钥验证
- **NTP时间同步**: 支持从多个NTP服务器获取时间戳
- **进度回调**: 支持加密解密进度回调
- **单例模式**: 自动管理DLL生命周期

## 快速开始

### 1. 添加引用

将EncodeLib.dll添加到你的项目引用中。

### 2. 基本使用

```csharp
using EncodeLib;

// 获取EncodeLib实例（单例）
var encodeLib = EncodeLibManager.Instance;

// 检查是否成功加载
if (encodeLib.IsLoaded)
{
    Console.WriteLine("EncodeLib加载成功！");
}
```

### 3. 文件加密

```csharp
// 加密文件
string inputFile = @"C:\test\document.txt";
string outputFile = @"C:\test\document.txt.encrypted";
string key = "your_secret_key";

int result = EncodeLibManager.Instance.EncryptFile(
    inputFile, 
    outputFile, 
    key, 
    (filePath, progress) => {
        // 进度回调
        Console.WriteLine($"加密进度: {progress * 100:F1}%");
    });

if (result == 0)
{
    Console.WriteLine("加密成功！");
}
else
{
    Console.WriteLine($"加密失败: {EncodeLibManager.GetErrorMessage(result)}");
}
```

### 4. 文件解密

```csharp
// 解密文件
string encryptedFile = @"C:\test\document.txt.encrypted";
string decryptedFile = @"C:\test\document_decrypted.txt";
string key = "your_secret_key";

int result = EncodeLibManager.Instance.DecryptFile(
    encryptedFile, 
    decryptedFile, 
    key, 
    (filePath, progress) => {
        // 进度回调
        Console.WriteLine($"解密进度: {progress * 100:F1}%");
    });

if (result == 0)
{
    Console.WriteLine("解密成功！");
}
else
{
    Console.WriteLine($"解密失败: {EncodeLibManager.GetErrorMessage(result)}");
}
```

### 5. 验证加密文件

```csharp
// 验证加密文件是否有效
string encryptedFile = @"C:\test\document.txt.encrypted";
string key = "your_secret_key";

int result = EncodeLibManager.Instance.ValidateEncryptedFile(encryptedFile, key);

if (result == 1)
{
    Console.WriteLine("文件验证成功，密钥正确！");
}
else
{
    Console.WriteLine("文件验证失败，密钥错误或文件已损坏！");
}
```

### 6. NTP时间同步

```csharp
// 获取NTP时间戳
long timestamp;
int result = EncodeLibManager.Instance.GetNTPTimestamp(out timestamp);

if (result == 0)
{
    DateTime ntpTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
    Console.WriteLine($"NTP时间: {ntpTime.ToLocalTime()}");
}

// 从指定服务器获取NTP时间戳
result = EncodeLibManager.Instance.GetNTPTimestampFromServer("pool.ntp.org", out timestamp, 5000);

if (result == 0)
{
    DateTime ntpTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
    Console.WriteLine($"来自pool.ntp.org的时间: {ntpTime.ToLocalTime()}");
}

// 获取本地时间戳
long localTimestamp = EncodeLibManager.Instance.GetLocalTimestamp();
DateTime localTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(localTimestamp);
Console.WriteLine($"本地时间: {localTime.ToLocalTime()}");
```

## 错误处理

```csharp
try
{
    int result = EncodeLibManager.Instance.EncryptFile(inputFile, outputFile, key);
    
    if (result != 0)
    {
        string errorMessage = EncodeLibManager.GetErrorMessage(result);
        Console.WriteLine($"操作失败: {errorMessage} (错误码: {result})");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"发生异常: {ex.Message}");
}
```

## 错误码说明

- `0`: 操作成功
- `-1`: 文件打开失败
- `-2`: 内存分配失败
- `-3`: 加密操作失败
- `-4`: 解密操作失败
- `-5`: 无效文件头
- `-6`: 线程创建失败

## 进度回调委托

```csharp
public delegate void ProgressCallback(string filePath, double progress);
```

- `filePath`: 当前处理的文件路径
- `progress`: 进度值（0.0-1.0，1.0表示100%完成）

## 注意事项

1. **单例模式**: EncodeLibManager使用单例模式，全局只有一个实例
2. **线程安全**: 进度回调可能在工作线程中调用，需要注意UI线程同步
3. **资源管理**: 库会自动管理DLL资源，无需手动释放
4. **密钥安全**: 请妥善保管加密密钥，丢失密钥将无法解密文件
5. **平台兼容**: 支持.NET Framework 4.6.2及以上版本

## 技术实现

- **内存DLL加载**: 基于DLLFromMemory.Net实现
- **嵌入资源**: TestExportLib.vmp.dll作为嵌入资源包含在库中
- **流式处理**: 支持大文件的流式加密解密
- **多线程**: 底层支持多线程并行处理提高性能 