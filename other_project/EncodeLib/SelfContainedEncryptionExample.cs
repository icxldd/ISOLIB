using System;
using System.IO;
using System.Text;

namespace EncodeLib
{
    /// <summary>
    /// 自包含式加密系统使用示例
    /// 展示如何使用无需预设私钥的加密/解密功能
    /// </summary>
    public class SelfContainedEncryptionExample
    {
        /// <summary>
        /// 文件加密/解密示例
        /// </summary>
        public static void FileEncryptionExample()
        {
            Console.WriteLine("=== 自包含式文件加密/解密示例 ===");

            try
            {
                // 获取加密库实例
                var encodeLib = EncodeLibManager.Instance;

                // 设置公钥（只需要公钥，私钥会自动生成）
                string publicKey = "MyPublicKey2024!@#";

                // 创建测试文件
                string inputFile = "test_input.txt";
                string encryptedFile = "test_encrypted.enc";
                string decryptedFile = "test_decrypted.txt";

                // 写入测试数据
                string testData = "这是自包含式加密系统的测试数据！\n" +
                                "特点：\n" +
                                "1. 无需预设私钥\n" +
                                "2. 自动生成2048位私钥\n" +
                                "3. 私钥存储在加密文件中\n" +
                                "4. 完整性验证保护\n" +
                                "5. 高强度安全性";

                File.WriteAllText(inputFile, testData, Encoding.UTF8);
                Console.WriteLine($"创建测试文件: {inputFile}");

                // 进度回调函数
                ProgressCallback progressCallback = (filePath, progress) =>
                {
                    Console.WriteLine($"处理进度: {Path.GetFileName(filePath)} - {progress:P2}");
                };

                // 自包含式加密（无需预设私钥）
                Console.WriteLine("\n开始自包含式加密...");
                int encryptResult = encodeLib.SelfContainedEncryptFile(inputFile, encryptedFile, publicKey, progressCallback);

                if (encryptResult == 0)
                {
                    Console.WriteLine("✅ 加密成功！");
                    Console.WriteLine($"加密文件大小: {new FileInfo(encryptedFile).Length} 字节");

                    // 验证加密文件
                    bool isValid = encodeLib.ValidateSelfContainedFile(encryptedFile, publicKey);
                    Console.WriteLine($"文件完整性验证: {(isValid ? "✅ 通过" : "❌ 失败")}");
                }
                else
                {
                    Console.WriteLine($"❌ 加密失败，错误码: {encryptResult} ({EncodeLibManager.GetErrorMessage(encryptResult)})");
                    return;
                }

                // 自包含式解密（从文件中读取私钥）
                Console.WriteLine("\n开始自包含式解密...");
                int decryptResult = encodeLib.SelfContainedDecryptFile(encryptedFile, decryptedFile, publicKey, progressCallback);

                if (decryptResult == 0)
                {
                    Console.WriteLine("✅ 解密成功！");

                    // 验证解密结果
                    string decryptedData = File.ReadAllText(decryptedFile, Encoding.UTF8);
                    bool dataMatches = decryptedData == testData;
                    Console.WriteLine($"数据完整性验证: {(dataMatches ? "✅ 通过" : "❌ 失败")}");

                    if (dataMatches)
                    {
                        Console.WriteLine("📝 解密后的数据:");
                        Console.WriteLine(decryptedData);
                    }
                }
                else
                {
                    Console.WriteLine($"❌ 解密失败，错误码: {decryptResult} ({EncodeLibManager.GetErrorMessage(decryptResult)})");
                }

                // 清理测试文件
                CleanupFiles(inputFile, encryptedFile, decryptedFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 示例执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 数据加密/解密示例
        /// </summary>
        public static void DataEncryptionExample()
        {
            Console.WriteLine("\n=== 自包含式数据加密/解密示例 ===");

            try
            {
                // 获取加密库实例
                var encodeLib = EncodeLibManager.Instance;

                // 设置公钥
                string publicKey = "DataEncryptionKey2024";

                // 准备测试数据
                string originalText = "自包含式数据加密测试！🔐\n支持任意二进制数据加密。";
                byte[] originalData = Encoding.UTF8.GetBytes(originalText);

                Console.WriteLine($"原始数据大小: {originalData.Length} 字节");
                Console.WriteLine($"原始数据内容: {originalText}");

                // 自包含式数据加密
                Console.WriteLine("\n开始数据加密...");
                byte[] encryptedData = encodeLib.SelfContainedEncryptData(originalData, publicKey);

                Console.WriteLine($"✅ 加密成功！");
                Console.WriteLine($"加密数据大小: {encryptedData.Length} 字节");
                Console.WriteLine($"大小增长: {encryptedData.Length - originalData.Length} 字节 (包含私钥和元数据)");

                // 验证加密数据
                bool isValidData = encodeLib.ValidateSelfContainedData(encryptedData, publicKey);
                Console.WriteLine($"数据完整性验证: {(isValidData ? "✅ 通过" : "❌ 失败")}");

                // 自包含式数据解密
                Console.WriteLine("\n开始数据解密...");
                byte[] decryptedData = encodeLib.SelfContainedDecryptData(encryptedData, publicKey);

                Console.WriteLine($"✅ 解密成功！");
                Console.WriteLine($"解密数据大小: {decryptedData.Length} 字节");

                // 验证解密结果
                string decryptedText = Encoding.UTF8.GetString(decryptedData);
                bool dataMatches = decryptedText == originalText;
                Console.WriteLine($"数据一致性验证: {(dataMatches ? "✅ 通过" : "❌ 失败")}");

                if (dataMatches)
                {
                    Console.WriteLine($"📝 解密后的内容: {decryptedText}");
                }

                // 展示错误公钥的情况
                Console.WriteLine("\n--- 错误公钥测试 ---");
                try
                {
                    string wrongKey = "WrongPublicKey";
                    encodeLib.SelfContainedDecryptData(encryptedData, wrongKey);
                    Console.WriteLine("❌ 应该解密失败但却成功了！");
                }
                catch (Exception)
                {
                    Console.WriteLine("✅ 使用错误公钥正确拒绝解密");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 数据加密示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 性能测试示例
        /// </summary>
        public static void PerformanceExample()
        {
            Console.WriteLine("\n=== 自包含式加密性能测试 ===");

            try
            {
                var encodeLib = EncodeLibManager.Instance;
                string publicKey = "PerformanceTestKey2024";

                // 测试不同大小的数据
                int[] testSizes = { 1024, 10 * 1024, 100 * 1024, 1024 * 1024 }; // 1KB, 10KB, 100KB, 1MB

                foreach (int size in testSizes)
                {
                    Console.WriteLine($"\n测试数据大小: {FormatBytes(size)}");

                    // 生成测试数据
                    byte[] testData = new byte[size];
                    new Random().NextBytes(testData);

                    // 加密性能测试
                    var encryptStartTime = DateTime.Now;
                    byte[] encryptedData = encodeLib.SelfContainedEncryptData(testData, publicKey);
                    var encryptDuration = DateTime.Now - encryptStartTime;

                    Console.WriteLine($"  加密耗时: {encryptDuration.TotalMilliseconds:F2} ms");
                    Console.WriteLine($"  加密速度: {FormatBytes((long)(size / encryptDuration.TotalSeconds))}/秒");

                    // 解密性能测试
                    var decryptStartTime = DateTime.Now;
                    byte[] decryptedData = encodeLib.SelfContainedDecryptData(encryptedData, publicKey);
                    var decryptDuration = DateTime.Now - decryptStartTime;

                    Console.WriteLine($"  解密耗时: {decryptDuration.TotalMilliseconds:F2} ms");
                    Console.WriteLine($"  解密速度: {FormatBytes((long)(size / decryptDuration.TotalSeconds))}/秒");

                    // 验证数据完整性
                    bool isEqual = CompareBytes(testData, decryptedData);
                    Console.WriteLine($"  数据完整性: {(isEqual ? "✅ 通过" : "❌ 失败")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 性能测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 私钥生成示例
        /// </summary>
        public static void PrivateKeyGenerationExample()
        {
            Console.WriteLine("\n=== 2048位私钥生成示例 ===");

            try
            {
                var encodeLib = EncodeLibManager.Instance;

                Console.WriteLine("生成多个2048位随机私钥...");

                for (int i = 1; i <= 3; i++)
                {
                    byte[] privateKey = encodeLib.Generate2048BitPrivateKey();
                    Console.WriteLine($"私钥 #{i}:");
                    Console.WriteLine($"  长度: {privateKey.Length} 字节 ({privateKey.Length * 8} 位)");
                    Console.WriteLine($"  前16字节: {BitConverter.ToString(privateKey, 0, Math.Min(16, privateKey.Length))}");
                    Console.WriteLine($"  熵值估算: {CalculateEntropy(privateKey):F2} bits/byte");

                    // 安全清理
                    Array.Clear(privateKey, 0, privateKey.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 私钥生成示例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("🔐 自包含式加密系统完整示例");
            Console.WriteLine("========================================");

            FileEncryptionExample();
            DataEncryptionExample();
            PerformanceExample();
            PrivateKeyGenerationExample();

            Console.WriteLine("\n========================================");
            Console.WriteLine("✅ 所有示例执行完成！");
        }

        #region 辅助方法

        /// <summary>
        /// 清理测试文件
        /// </summary>
        private static void CleanupFiles(params string[] files)
        {
            foreach (string file in files)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:F2} {units[unit]}";
        }

        /// <summary>
        /// 比较两个字节数组是否相等
        /// </summary>
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 计算数据的熵值（信息量）
        /// </summary>
        private static double CalculateEntropy(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            // 统计每个字节值的出现频率
            int[] freq = new int[256];
            foreach (byte b in data)
            {
                freq[b]++;
            }

            // 计算熵值
            double entropy = 0;
            int length = data.Length;

            for (int i = 0; i < 256; i++)
            {
                if (freq[i] > 0)
                {
                    double probability = (double)freq[i] / length;
                    entropy -= probability * Math.Log2(probability);
                }
            }

            return entropy;
        }

        #endregion
    }
} 