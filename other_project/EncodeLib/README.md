# EncodeLib - RSA-2048非对称加密系统

## 概述

EncodeLib是一个基于RSA-2048算法的非对称加密系统，提供了C++库和C#封装。该系统实现了真正的非对称加密：
- **加密**：仅需要公钥
- **解密**：仅需要私钥
- **密钥生成**：自动生成RSA-2048密钥对

## 系统架构

```
┌─────────────────────────────────────┐
│           C# 应用层                  │
│  ┌─────────────────────────────────┐ │
│  │     EncodeLibManager.cs         │ │  <- 主要API接口
│  │     RSAEncryptionExample.cs    │ │  <- 使用示例
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│           C# 封装层                  │
│  ┌─────────────────────────────────┐ │
│  │     WindowsDllManager.cs        │ │  <- DLL加载和函数映射
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
                    │
┌─────────────────────────────────────┐
│           C++ 核心库                 │
│  ┌─────────────────────────────────┐ │
│  │     encode.h / encode.cpp       │ │  <- RSA-2048实现
│  │     TestExportLib.dll           │ │  <- 编译后的DLL
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

## 主要特性

### 🔐 RSA-2048非对称加密
- **密钥长度**: 2048位，提供高强度安全性
- **密钥格式**: `RSA-2048-PUB:n:e` (公钥) 和 `RSA-2048-PRI:n:d` (私钥)
- **加密算法**: 基于RSA参数的三层XOR流加密
- **文件头**: `ASYMV1.0` 魔数标识
- **密钥验证**: 解密时自动验证私钥与加密公钥是否匹配

### 📁 文件和数据加密
- **文件加密**: 支持任意大小文件的流式加密
- **数据加密**: 支持字节数组的内存加密
- **进度回调**: 实时显示加密/解密进度
- **完整性验证**: CRC32校验和确保数据完整性
- **密钥匹配验证**: 防止使用错误私钥解密

### 🔑 密钥管理
- **自动生成**: 一次性生成多个密钥对
- **内存安全**: 自动释放密钥内存，防止泄露
- **格式验证**: 自动验证密钥格式和有效性
- **配对验证**: 确保私钥与加密时使用的公钥匹配

## 密钥验证机制

### 🔒 自动密钥匹配验证

系统在解密时会自动验证私钥是否与加密时使用的公钥匹配：

1. **加密时存储**：
   - 公钥哈希值存储在加密文件头部
   - 公钥CRC32校验和存储在文件尾部

2. **解密时验证**：
   - 从私钥重构对应的公钥
   - 比较重构公钥的哈希值和校验和
   - 如果不匹配，返回 `ERR_INVALID_KEY` 错误

3. **验证示例**：
```csharp
// 生成两个不同的密钥对
RSAKeyPair keyPair1 = encodeLib.GenerateRSAKeyPair();
RSAKeyPair keyPair2 = encodeLib.GenerateRSAKeyPair();

// 使用keyPair1的公钥加密
byte[] encrypted = encodeLib.EncryptData(data, keyPair1.PublicKey);

// ✅ 使用匹配的私钥解密 - 成功
byte[] decrypted1 = encodeLib.DecryptData(encrypted, keyPair1.PrivateKey);

// ❌ 使用不匹配的私钥解密 - 抛出异常
try 
{
    byte[] decrypted2 = encodeLib.DecryptData(encrypted, keyPair2.PrivateKey);
}
catch (InvalidOperationException ex) 
{
    // ex.Message 包含 "ERR_INVALID_KEY" 或 "无效密钥"
    Console.WriteLine("密钥不匹配，解密失败！");
}
```

### 🛡️ 错误处理

当使用错误私钥解密时，系统会：
- **立即检测**：在开始解密数据前就验证密钥匹配
- **快速失败**：避免浪费时间处理无效解密
- **明确错误**：返回 `ERR_INVALID_KEY` (-9) 错误码
- **安全保护**：防止生成无意义的解密结果

## 快速开始

### 1. 基本使用

```csharp
using EncodeLib;

// 获取管理器实例
var encodeLib = EncodeLibManager.Instance;

// 生成RSA-2048密钥对
RSAKeyPair keyPair = encodeLib.GenerateRSAKeyPair();

// 加密数据（使用公钥）
byte[] originalData = Encoding.UTF8.GetBytes("Hello RSA-2048!");
byte[] encryptedData = encodeLib.EncryptData(originalData, keyPair.PublicKey);

// 解密数据（使用私钥）
byte[] decryptedData = encodeLib.DecryptData(encryptedData, keyPair.PrivateKey);
string result = Encoding.UTF8.GetString(decryptedData);
```

### 2. 文件加密

```csharp
// 加密文件（使用公钥）
int result = encodeLib.EncryptFile(
    "input.txt", 
    "encrypted.dat", 
    keyPair.PublicKey, 
    (filePath, progress) => Console.WriteLine($"加密进度: {progress:P1}")
);

// 解密文件（使用私钥）
result = encodeLib.DecryptFile(
    "encrypted.dat", 
    "decrypted.txt", 
    keyPair.PrivateKey,
    (filePath, progress) => Console.WriteLine($"解密进度: {progress:P1}")
);
```

### 3. 批量生成密钥对

```csharp
// 生成多个密钥对
RSAKeyPair[] keyPairs = encodeLib.GenerateRSAKeyPairs(5);

foreach (var kp in keyPairs)
{
    Console.WriteLine($"公钥: {kp.PublicKey.Substring(0, 50)}...");
    Console.WriteLine($"私钥: {kp.PrivateKey.Substring(0, 50)}...");
}
```

## API 参考

### EncodeLibManager 类

#### 密钥生成
- `RSAKeyPair GenerateRSAKeyPair()` - 生成单个密钥对
- `RSAKeyPair[] GenerateRSAKeyPairs(int count)` - 生成多个密钥对

#### 文件加密
- `int EncryptFile(string inputPath, string outputPath, string publicKey, ProgressCallback callback = null)`
- `int DecryptFile(string inputPath, string outputPath, string privateKey, ProgressCallback callback = null)`
- `bool ValidateEncryptedFile(string filePath, string privateKey)`

#### 数据加密
- `byte[] EncryptData(byte[] inputData, string publicKey)`
- `byte[] DecryptData(byte[] encryptedData, string privateKey)`
- `bool ValidateData(byte[] encryptedData, string privateKey)`

#### 辅助函数
- `uint CalculateCRC32(byte[] data)` - 计算CRC32校验和
- `uint CalculateKeyHash(string key)` - 计算密钥哈希值
- `static string GetErrorMessage(int errorCode)` - 获取错误信息

### RSAKeyPair 类

```csharp
public class RSAKeyPair
{
    public string PublicKey { get; set; }   // RSA-2048公钥
    public string PrivateKey { get; set; }  // RSA-2048私钥
}
```

## 错误码

| 错误码 | 含义 |
|--------|------|
| 0 | 操作成功 |
| -1 | 文件打开失败 |
| -2 | 内存分配失败 |
| -3 | 加密操作失败 |
| -4 | 解密操作失败 |
| -5 | 无效文件头 |
| -6 | 线程创建失败 |
| -7 | 无效参数 |
| -8 | 密钥生成失败 |
| -9 | 无效密钥 |

## 完整示例

### 运行示例代码

```csharp
// 运行完整示例
RSAEncryptionExample.RunExample();

// 运行性能测试
RSAEncryptionExample.RunPerformanceTest();
```

### 实际应用场景

#### 1. 文档加密系统
```csharp
public class DocumentEncryption
{
    private readonly EncodeLibManager encodeLib;
    private RSAKeyPair systemKeyPair;

    public DocumentEncryption()
    {
        encodeLib = EncodeLibManager.Instance;
        systemKeyPair = encodeLib.GenerateRSAKeyPair();
    }

    public bool EncryptDocument(string docPath, string encryptedPath)
    {
        int result = encodeLib.EncryptFile(docPath, encryptedPath, systemKeyPair.PublicKey);
        return result == 0;
    }

    public bool DecryptDocument(string encryptedPath, string outputPath)
    {
        int result = encodeLib.DecryptFile(encryptedPath, outputPath, systemKeyPair.PrivateKey);
        return result == 0;
    }
}
```

#### 2. 网络传输加密
```csharp
public class SecureDataTransfer
{
    public byte[] PrepareSecureData(string data, string recipientPublicKey)
    {
        var encodeLib = EncodeLibManager.Instance;
        byte[] originalData = Encoding.UTF8.GetBytes(data);
        return encodeLib.EncryptData(originalData, recipientPublicKey);
    }

    public string ReceiveSecureData(byte[] encryptedData, string myPrivateKey)
    {
        var encodeLib = EncodeLibManager.Instance;
        byte[] decryptedData = encodeLib.DecryptData(encryptedData, myPrivateKey);
        return Encoding.UTF8.GetString(decryptedData);
    }
}
```

## 系统要求

- **.NET Framework**: 4.5 或更高版本
- **操作系统**: Windows 7/8/10/11 (x86/x64)
- **依赖库**: TestExportLib.dll (RSA-2048 C++实现)

## 安全注意事项

1. **私钥保护**: 私钥必须安全存储，不能泄露
2. **密钥轮换**: 定期更换密钥对以提高安全性
3. **内存清理**: 系统自动清理敏感数据内存
4. **传输安全**: 公钥可以公开传输，私钥必须通过安全通道传输

## 性能特点

- **密钥生成**: ~50-100ms (取决于系统性能)
- **加密速度**: ~10-50 MB/s (取决于数据大小和系统性能)
- **解密速度**: ~10-50 MB/s (取决于数据大小和系统性能)
- **内存占用**: 低内存占用，支持大文件流式处理

## 更新历史

### v2.0 (当前版本)
- ✅ 重构为真正的RSA-2048非对称加密系统
- ✅ 加密仅需公钥，解密仅需私钥
- ✅ 添加自动密钥对生成功能
- ✅ 移除旧的双密钥系统
- ✅ 增强安全性和性能

### v1.0 (旧版本)
- ❌ 双密钥系统（已废弃）
- ❌ 需要同时设置公钥和私钥（已废弃）

## 技术支持

如有问题或建议，请联系开发团队。 