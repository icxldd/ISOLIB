using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EncodeLib
{
    /// <summary>
    /// RSA-2048非对称加密密钥对
    /// </summary>
    public class RSAKeyPair
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }

        public RSAKeyPair(string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }
    }

    /// <summary>
    /// EncodeLib主管理器类 - RSA-2048非对称加密系统
    /// 封装所有TestExportLib功能，提供简单易用的API
    /// </summary>
    public class EncodeLibManager : IDisposable
    {
        private WindowsDllManager dllManager;
        private bool disposed = false;
        private static EncodeLibManager instance;
        private static readonly object lockObject = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static EncodeLibManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new EncodeLibManager();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 私有构造函数，实现单例模式
        /// </summary>
        private EncodeLibManager()
        {
            InitializeWindowsDll();
        }

        /// <summary>
        /// 从Base64字符串转换为字节数组
        /// </summary>
        /// <param name="base64String">Base64字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] ConvertBase64ToBytes(string base64String)
        {
            try
            {
                // 清理Base64字符串（移除可能的换行符、空格等）
                string cleanBase64 = base64String.Replace("\r", "").Replace("\n", "").Replace(" ", "");

                // 转换为字节数组
                byte[] bytes = Convert.FromBase64String(cleanBase64);

                return bytes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Base64转换失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清除内存痕迹
        /// </summary>
        /// <param name="data">要清除的字节数组</param>
        private void ClearMemoryTraces(byte[] data)
        {
            if (data != null)
            {
                // 覆盖原始字节数组
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }

                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        /// <summary>
        /// 初始化Windows DLL管理器，使用Windows API加载DLL
        /// </summary>
        private void InitializeWindowsDll()
        {
            try
            {
                // 寻找DLL文件路径
                string dllPath = FindDllPath();
                
                if (string.IsNullOrEmpty(dllPath))
                {
                    throw new FileNotFoundException("找不到TestExportLib.dll文件");
                }

                // 使用Windows API加载DLL
                dllManager = new WindowsDllManager(dllPath);

                // 验证DLL是否成功加载
                if (!dllManager.IsLoaded)
                {
                    throw new InvalidOperationException("DLL加载失败");
                }

                System.Diagnostics.Debug.WriteLine($"EncodeLib: DLL加载成功！路径: {dllPath}");
            }
            catch (Exception ex)
            {
                string errorMsg = $"初始化Windows DLL失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"EncodeLib错误: {errorMsg}");
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        /// <summary>
        /// 查找DLL文件路径
        /// </summary>
        /// <returns>DLL文件的完整路径</returns>
        private string FindDllPath()
        {
            // 可能的DLL文件名
            string[] dllNames = {
                "TestExportLib.dll",
                "ExportLib.dll",
                "TestExportLib.vmp.dll",
                "ExportLib.vmp.dll"
            };

            // 可能的搜索路径
            string[] searchPaths = {
                Environment.CurrentDirectory,                    // 当前目录
                AppDomain.CurrentDomain.BaseDirectory,         // 应用程序基目录
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), // 当前程序集目录
                Path.Combine(Environment.CurrentDirectory, "lib"),      // lib子目录
                Path.Combine(Environment.CurrentDirectory, "bin"),      // bin子目录
                Path.Combine(Environment.CurrentDirectory, "dll"),      // dll子目录
            };

            // 遍历所有可能的组合
            foreach (string searchPath in searchPaths)
            {
                foreach (string dllName in dllNames)
                {
                    string fullPath = Path.Combine(searchPath, dllName);
                    if (File.Exists(fullPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"找到DLL文件: {fullPath}");
                        return fullPath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 检查DLL是否已加载
        /// </summary>
        public bool IsLoaded => dllManager?.IsLoaded == true && !disposed;

        // ========== RSA-2048密钥对生成和管理函数 ==========

        /// <summary>
        /// 生成RSA-2048密钥对
        /// </summary>
        /// <param name="count">要生成的密钥对数量</param>
        /// <returns>RSA密钥对数组</returns>
        public RSAKeyPair[] GenerateRSAKeyPairs(int count)
        {
            CheckDllLoaded();

            if (count <= 0)
            {
                throw new ArgumentException("密钥对数量必须大于0", nameof(count));
            }

            WindowsDllManager.KeyPair[] nativeKeyPairs = new WindowsDllManager.KeyPair[count];
            RSAKeyPair[] managedKeyPairs = new RSAKeyPair[count];

            try
            {
                // 调用DLL函数生成密钥对
                int result = dllManager.GenerateKeyPairs(count, nativeKeyPairs);
                if (result != 0)
                {
                    throw new InvalidOperationException($"密钥对生成失败，错误码: {result} ({GetErrorMessage(result)})");
                }

                // 将原生指针转换为托管字符串
                for (int i = 0; i < count; i++)
                {
                    string publicKey = Marshal.PtrToStringAnsi(nativeKeyPairs[i].publicKey);
                    string privateKey = Marshal.PtrToStringAnsi(nativeKeyPairs[i].privateKey);
                    
                    managedKeyPairs[i] = new RSAKeyPair(publicKey, privateKey);
                }

                // 释放原生内存
                dllManager.FreeKeyPairs(count, nativeKeyPairs);

                return managedKeyPairs;
            }
            catch (Exception ex)
            {
                // 确保在异常情况下也释放内存
                try
                {
                    dllManager.FreeKeyPairs(count, nativeKeyPairs);
                }
                catch { }

                System.Diagnostics.Debug.WriteLine($"EncodeLib密钥对生成错误: {ex.Message}");
                throw new InvalidOperationException($"RSA密钥对生成失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成单个RSA-2048密钥对
        /// </summary>
        /// <returns>RSA密钥对</returns>
        public RSAKeyPair GenerateRSAKeyPair()
        {
            RSAKeyPair[] keyPairs = GenerateRSAKeyPairs(1);
            return keyPairs[0];
        }

        // ========== RSA-2048非对称加密解密函数 ==========

        /// <summary>
        /// 加密文件（RSA-2048非对称加密：仅需要公钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="publicKey">RSA-2048公钥字符串</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int EncryptFile(string inputFilePath, string outputFilePath, string publicKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentException("输入文件路径不能为空", nameof(inputFilePath));
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentException("输出文件路径不能为空", nameof(outputFilePath));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"输入文件不存在: {inputFilePath}");
            }

            try
            {
                return dllManager.StreamEncryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib加密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }
        /// <summary>
        /// 获取NTP时间戳
        /// </summary>
        /// <param name="timestamp">输出时间戳</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int GetNTPTimestamp(out long timestamp)
        {
            CheckDllLoaded();
            timestamp = 0;

            try
            {
                return dllManager.GetNTPTimestamp(out timestamp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib NTP错误: {ex.Message}");
                return -1;
            }
        }


        /// <summary>
        /// 解密文件（RSA-2048非对称加密：仅需要私钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="privateKey">RSA-2048私钥字符串</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int DecryptFile(string inputFilePath, string outputFilePath, string privateKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentException("输入文件路径不能为空", nameof(inputFilePath));
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentException("输出文件路径不能为空", nameof(outputFilePath));
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"输入文件不存在: {inputFilePath}");
            }

            try
            {
                return dllManager.StreamDecryptFile(inputFilePath, outputFilePath, privateKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib解密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 验证加密文件（RSA-2048非对称加密：使用私钥验证）
        /// </summary>
        /// <param name="encryptedFilePath">加密文件路径</param>
        /// <param name="privateKey">RSA-2048私钥字符串</param>
        /// <returns>有效返回true，无效返回false</returns>
        public bool ValidateEncryptedFile(string encryptedFilePath, string privateKey)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(encryptedFilePath) || string.IsNullOrEmpty(privateKey))
            {
                return false;
            }

            if (!File.Exists(encryptedFilePath))
            {
                return false;
            }

            try
            {
                return dllManager.ValidateEncryptedFile(encryptedFilePath, privateKey) == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib验证错误: {ex.Message}");
                return false;
            }
        }

        // ========== 字节数组加密解密函数 ==========

        /// <summary>
        /// 加密字节数组（RSA-2048非对称加密：仅需要公钥）
        /// </summary>
        /// <param name="inputData">输入数据字节数组</param>
        /// <param name="publicKey">RSA-2048公钥字符串</param>
        /// <returns>成功返回加密后的字节数组，失败抛出异常</returns>
        public byte[] EncryptData(byte[] inputData, string publicKey)
        {
            CheckDllLoaded();

            if (inputData == null || inputData.Length == 0)
            {
                throw new ArgumentException("输入数据不能为空", nameof(inputData));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            IntPtr inputPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            byte[] result = null;

            try
            {
                // 分配非托管内存用于输入数据
                inputPtr = Marshal.AllocHGlobal(inputData.Length);
                Marshal.Copy(inputData, 0, inputPtr, inputData.Length);

                // 调用DLL函数
                UIntPtr outputLength;
                int errorCode = dllManager.StreamEncryptData(
                    inputPtr,
                    new UIntPtr((uint)inputData.Length),
                    publicKey,
                    out outputPtr,
                    out outputLength);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"加密失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (outputPtr == IntPtr.Zero || outputLength.ToUInt32() == 0)
                {
                    throw new InvalidOperationException("加密返回空数据");
                }

                // 立即复制到C#字节数组
                int length = (int)outputLength.ToUInt32();
                result = new byte[length];
                Marshal.Copy(outputPtr, result, 0, length);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib字节数组加密错误: {ex.Message}");
                throw new InvalidOperationException($"字节数组加密失败: {ex.Message}", ex);
            }
            finally
            {
                // 立即释放所有分配的内存
                if (inputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputPtr);
                }

                if (outputPtr != IntPtr.Zero)
                {
                    try
                    {
                        dllManager.FreeEncryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放加密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 解密字节数组（RSA-2048非对称加密：仅需要私钥）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="privateKey">RSA-2048私钥字符串</param>
        /// <returns>成功返回解密后的字节数组，失败抛出异常</returns>
        public byte[] DecryptData(byte[] encryptedData, string privateKey)
        {
            CheckDllLoaded();

            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("加密数据不能为空", nameof(encryptedData));
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            IntPtr inputPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            byte[] result = null;

            try
            {
                // 分配非托管内存用于输入数据
                inputPtr = Marshal.AllocHGlobal(encryptedData.Length);
                Marshal.Copy(encryptedData, 0, inputPtr, encryptedData.Length);

                // 调用DLL函数
                UIntPtr outputLength;
                int errorCode = dllManager.StreamDecryptData(
                    inputPtr,
                    new UIntPtr((uint)encryptedData.Length),
                    privateKey,
                    out outputPtr,
                    out outputLength);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"解密失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (outputPtr == IntPtr.Zero || outputLength.ToUInt32() == 0)
                {
                    throw new InvalidOperationException("解密返回空数据");
                }

                // 立即复制到C#字节数组
                int length = (int)outputLength.ToUInt32();
                result = new byte[length];
                Marshal.Copy(outputPtr, result, 0, length);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib字节数组解密错误: {ex.Message}");
                throw new InvalidOperationException($"字节数组解密失败: {ex.Message}", ex);
            }
            finally
            {
                // 立即释放所有分配的内存
                if (inputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputPtr);
                }

                if (outputPtr != IntPtr.Zero)
                {
                    try
                    {
                        dllManager.FreeDecryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放解密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 验证加密数据（RSA-2048非对称加密：尝试解密来验证）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="privateKey">RSA-2048私钥字符串</param>
        /// <returns>有效返回true，无效返回false</returns>
        public bool ValidateData(byte[] encryptedData, string privateKey)
        {
            try
            {
                // 尝试解密来验证数据有效性
                byte[] decryptedData = DecryptData(encryptedData, privateKey);
                return decryptedData != null && decryptedData.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // ========== 辅助函数 ==========

        /// <summary>
        /// 计算数据的CRC32校验和
        /// </summary>
        /// <param name="data">输入数据</param>
        /// <returns>CRC32校验和</returns>
        public uint CalculateCRC32(byte[] data)
        {
            CheckDllLoaded();

            if (data == null || data.Length == 0)
            {
                return 0;
            }

            IntPtr dataPtr = IntPtr.Zero;
            try
            {
                dataPtr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, dataPtr, data.Length);
                
                return dllManager.CalculateCRC32(dataPtr, new UIntPtr((uint)data.Length));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib CRC32计算错误: {ex.Message}");
                return 0;
            }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
        }

        /// <summary>
        /// 计算密钥的哈希值
        /// </summary>
        /// <param name="key">密钥字符串</param>
        /// <returns>密钥哈希值</returns>
        public uint CalculateKeyHash(string key)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            try
            {
                return dllManager.CalculateKeyHash(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib密钥哈希计算错误: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 检查DLL是否已加载的辅助方法
        /// </summary>
        private void CheckDllLoaded()
        {
            if (dllManager == null || !dllManager.IsLoaded || disposed)
            {
                throw new InvalidOperationException("DLL未加载或已释放！");
            }
        }

        /// <summary>
        /// 获取错误码对应的错误信息
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>错误信息</returns>
        public static string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0: return "操作成功";
                case -1: return "文件打开失败";
                case -2: return "内存分配失败";
                case -3: return "加密操作失败";
                case -4: return "解密操作失败";
                case -5: return "无效文件头";
                case -6: return "线程创建失败";
                case -7: return "无效参数";
                case -8: return "密钥生成失败";
                case -9: return "无效密钥";
                default: return $"未知错误 (代码: {errorCode})";
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                dllManager?.Dispose();
                dllManager = null;
                disposed = true;

                lock (lockObject)
                {
                    if (instance == this)
                    {
                        instance = null;
                    }
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~EncodeLibManager()
        {
            Dispose();
        }
    }
}