using System;
using System.IO;
using System.Text;

namespace EncodeLib
{
    /// <summary>
    /// RSA-2048非对称加密系统使用示例
    /// 演示如何生成密钥对、加密解密文件和数据
    /// </summary>
    public static class RSAEncryptionExample
    {
        /// <summary>
        /// 完整的RSA-2048加密解密示例
        /// </summary>
        public static void RunExample()
        {
            try
            {
                Console.WriteLine("=== RSA-2048非对称加密系统示例 ===\n");

                // 获取EncodeLibManager实例
                var encodeLib = EncodeLibManager.Instance;

                if (!encodeLib.IsLoaded)
                {
                    Console.WriteLine("错误: DLL未成功加载！");
                    return;
                }

                Console.WriteLine("1. DLL加载成功！");

                // 生成RSA-2048密钥对
                Console.WriteLine("\n2. 生成RSA-2048密钥对...");
                RSAKeyPair keyPair = encodeLib.GenerateRSAKeyPair();

                Console.WriteLine($"公钥: {keyPair.PublicKey.Substring(0, Math.Min(50, keyPair.PublicKey.Length))}...");
                Console.WriteLine($"私钥: {keyPair.PrivateKey.Substring(0, Math.Min(50, keyPair.PrivateKey.Length))}...");

                // 测试字节数组加密解密
                Console.WriteLine("\n3. 测试字节数组加密解密...");
                TestDataEncryption(encodeLib, keyPair);

                // 测试文件加密解密
                Console.WriteLine("\n4. 测试文件加密解密...");
                TestFileEncryption(encodeLib, keyPair);

                // 测试多个密钥对生成
                Console.WriteLine("\n5. 测试生成多个密钥对...");
                TestMultipleKeyPairs(encodeLib);

                Console.WriteLine("\n=== 所有测试完成！ ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"示例运行错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }
        }

        /// <summary>
        /// 测试字节数组加密解密
        /// </summary>
        private static void TestDataEncryption(EncodeLibManager encodeLib, RSAKeyPair keyPair)
        {
            try
            {
                // 准备测试数据
                string originalText = "这是一个RSA-2048非对称加密测试！Hello RSA-2048 Asymmetric Encryption!";
                byte[] originalData = Encoding.UTF8.GetBytes(originalText);

                Console.WriteLine($"原始数据: {originalText}");
                Console.WriteLine($"原始数据长度: {originalData.Length} 字节");

                // 使用公钥加密
                byte[] encryptedData = encodeLib.EncryptData(originalData, keyPair.PublicKey);
                Console.WriteLine($"加密后数据长度: {encryptedData.Length} 字节");

                // 使用私钥解密
                byte[] decryptedData = encodeLib.DecryptData(encryptedData, keyPair.PrivateKey);
                string decryptedText = Encoding.UTF8.GetString(decryptedData);

                Console.WriteLine($"解密后数据: {decryptedText}");
                Console.WriteLine($"加密解密成功: {originalText == decryptedText}");

                // 验证数据
                bool isValid = encodeLib.ValidateData(encryptedData, keyPair.PrivateKey);
                Console.WriteLine($"数据验证结果: {isValid}");

                // 计算校验和
                uint crc32 = encodeLib.CalculateCRC32(originalData);
                uint keyHash = encodeLib.CalculateKeyHash(keyPair.PublicKey);
                Console.WriteLine($"原始数据CRC32: 0x{crc32:X8}");
                Console.WriteLine($"公钥哈希值: 0x{keyHash:X8}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"字节数组加密测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试文件加密解密
        /// </summary>
        private static void TestFileEncryption(EncodeLibManager encodeLib, RSAKeyPair keyPair)
        {
            string tempDir = Path.GetTempPath();
            string originalFile = Path.Combine(tempDir, "rsa_test_original.txt");
            string encryptedFile = Path.Combine(tempDir, "rsa_test_encrypted.dat");
            string decryptedFile = Path.Combine(tempDir, "rsa_test_decrypted.txt");

            try
            {
                // 创建测试文件
                string testContent = $"RSA-2048文件加密测试\n时间: {DateTime.Now}\n这是一个多行测试文件。\nLine 1\nLine 2\nLine 3\n测试中文内容";
                File.WriteAllText(originalFile, testContent, Encoding.UTF8);

                Console.WriteLine($"创建测试文件: {originalFile}");
                Console.WriteLine($"原始文件大小: {new FileInfo(originalFile).Length} 字节");

                // 使用公钥加密文件
                int encryptResult = encodeLib.EncryptFile(originalFile, encryptedFile, keyPair.PublicKey, 
                    (filePath, progress) => {
                        if (progress >= 1.0)
                            Console.WriteLine($"加密进度: {progress:P1} - 完成");
                    });

                if (encryptResult == 0)
                {
                    Console.WriteLine($"文件加密成功: {encryptedFile}");
                    Console.WriteLine($"加密文件大小: {new FileInfo(encryptedFile).Length} 字节");

                    // 验证加密文件
                    bool isValidFile = encodeLib.ValidateEncryptedFile(encryptedFile, keyPair.PrivateKey);
                    Console.WriteLine($"加密文件验证: {isValidFile}");

                    // 使用私钥解密文件
                    int decryptResult = encodeLib.DecryptFile(encryptedFile, decryptedFile, keyPair.PrivateKey,
                        (filePath, progress) => {
                            if (progress >= 1.0)
                                Console.WriteLine($"解密进度: {progress:P1} - 完成");
                        });

                    if (decryptResult == 0)
                    {
                        Console.WriteLine($"文件解密成功: {decryptedFile}");
                        
                        // 比较原始文件和解密文件
                        string originalContent = File.ReadAllText(originalFile, Encoding.UTF8);
                        string decryptedContent = File.ReadAllText(decryptedFile, Encoding.UTF8);
                        
                        Console.WriteLine($"文件内容一致: {originalContent == decryptedContent}");
                    }
                    else
                    {
                        Console.WriteLine($"文件解密失败，错误码: {decryptResult} ({EncodeLibManager.GetErrorMessage(decryptResult)})");
                    }
                }
                else
                {
                    Console.WriteLine($"文件加密失败，错误码: {encryptResult} ({EncodeLibManager.GetErrorMessage(encryptResult)})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件加密测试失败: {ex.Message}");
            }
            finally
            {
                // 清理临时文件
                try
                {
                    if (File.Exists(originalFile)) File.Delete(originalFile);
                    if (File.Exists(encryptedFile)) File.Delete(encryptedFile);
                    if (File.Exists(decryptedFile)) File.Delete(decryptedFile);
                }
                catch { }
            }
        }

        /// <summary>
        /// 测试生成多个密钥对
        /// </summary>
        private static void TestMultipleKeyPairs(EncodeLibManager encodeLib)
        {
            try
            {
                // 生成3个密钥对
                RSAKeyPair[] keyPairs = encodeLib.GenerateRSAKeyPairs(3);

                Console.WriteLine($"成功生成 {keyPairs.Length} 个密钥对:");

                for (int i = 0; i < keyPairs.Length; i++)
                {
                    Console.WriteLine($"密钥对 {i + 1}:");
                    Console.WriteLine($"  公钥长度: {keyPairs[i].PublicKey.Length} 字符");
                    Console.WriteLine($"  私钥长度: {keyPairs[i].PrivateKey.Length} 字符");
                    
                    // 简单测试每个密钥对
                    string testData = $"Test data for key pair {i + 1}";
                    byte[] original = Encoding.UTF8.GetBytes(testData);
                    byte[] encrypted = encodeLib.EncryptData(original, keyPairs[i].PublicKey);
                    byte[] decrypted = encodeLib.DecryptData(encrypted, keyPairs[i].PrivateKey);
                    string result = Encoding.UTF8.GetString(decrypted);
                    
                    Console.WriteLine($"  测试结果: {testData == result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"多密钥对测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 性能测试示例
        /// </summary>
        public static void RunPerformanceTest()
        {
            try
            {
                Console.WriteLine("=== RSA-2048性能测试 ===\n");

                var encodeLib = EncodeLibManager.Instance;
                if (!encodeLib.IsLoaded)
                {
                    Console.WriteLine("错误: DLL未成功加载！");
                    return;
                }

                // 生成密钥对
                Console.WriteLine("生成密钥对...");
                var startTime = DateTime.Now;
                RSAKeyPair keyPair = encodeLib.GenerateRSAKeyPair();
                var keyGenTime = DateTime.Now - startTime;
                Console.WriteLine($"密钥生成耗时: {keyGenTime.TotalMilliseconds:F2} ms");

                // 测试不同大小数据的加密性能
                int[] dataSizes = { 1024, 10240, 102400, 1048576 }; // 1KB, 10KB, 100KB, 1MB

                foreach (int size in dataSizes)
                {
                    Console.WriteLine($"\n测试数据大小: {size / 1024.0:F1} KB");

                    // 生成测试数据
                    byte[] testData = new byte[size];
                    new Random().NextBytes(testData);

                    // 加密性能测试
                    startTime = DateTime.Now;
                    byte[] encrypted = encodeLib.EncryptData(testData, keyPair.PublicKey);
                    var encryptTime = DateTime.Now - startTime;

                    // 解密性能测试
                    startTime = DateTime.Now;
                    byte[] decrypted = encodeLib.DecryptData(encrypted, keyPair.PrivateKey);
                    var decryptTime = DateTime.Now - startTime;

                    Console.WriteLine($"  加密耗时: {encryptTime.TotalMilliseconds:F2} ms");
                    Console.WriteLine($"  解密耗时: {decryptTime.TotalMilliseconds:F2} ms");
                    Console.WriteLine($"  数据完整性: {testData.Length == decrypted.Length}");
                }

                Console.WriteLine("\n=== 性能测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"性能测试失败: {ex.Message}");
            }
        }
    }
} 