using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EncodeLib
{
    /// <summary>
    /// EncodeLib主管理器类 - 对外提供加密解密和DLL加载服务
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

        // ========== 双密钥系统管理函数 ==========

        /// <summary>
        /// 使用私钥初始化双密钥加密系统
        /// </summary>
        /// <param name="privateKey">私钥字符串（支持任意长度）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int InitializePrivateKey(string privateKey)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            try
            {
                return dllManager.InitStreamFile(privateKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib初始化私钥错误: {ex.Message}");
                return -7; // ERR_INVALID_PARAMETER
            }
        }

        /// <summary>
        /// 清理私钥，释放内存（安全清除）
        /// </summary>
        public void ClearPrivateKey()
        {
            CheckDllLoaded();

            try
            {
                dllManager.ClearPrivateKey();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib清理私钥错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查私钥是否已设置
        /// </summary>
        /// <returns>true表示已设置，false表示未设置</returns>
        public bool IsPrivateKeySet()
        {
            CheckDllLoaded();

            try
            {
                return dllManager.IsPrivateKeySet() == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib检查私钥错误: {ex.Message}");
                return false;
            }
        }

        // ========== 双密钥加密解密函数 ==========

        /// <summary>
        /// 加密文件（双密钥系统：需要预先设置私钥，此处传入公钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int EncryptFile(string inputFilePath, string outputFilePath, string publicKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (!IsPrivateKeySet())
            {
                throw new InvalidOperationException("私钥未设置！请先调用 InitializePrivateKey 方法。");
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
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
        /// 解密文件（双密钥系统：需要预先设置私钥，此处传入公钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int DecryptFile(string inputFilePath, string outputFilePath, string publicKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (!IsPrivateKeySet())
            {
                throw new InvalidOperationException("私钥未设置！请先调用 InitializePrivateKey 方法。");
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            try
            {
                return dllManager.StreamDecryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib解密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        // ========== 字节数组加密解密函数 ==========

        /// <summary>
        /// 加密字节数组（双密钥系统：需要预先设置私钥，此处传入公钥）
        /// </summary>
        /// <param name="inputData">输入数据字节数组</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <returns>成功返回加密后的字节数组，失败抛出异常</returns>
        public byte[] EncryptData(byte[] inputData, string publicKey)
        {
            CheckDllLoaded();

            if (!IsPrivateKeySet())
            {
                throw new InvalidOperationException("私钥未设置！请先调用 InitializePrivateKey 方法。");
            }

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
        /// 解密字节数组（双密钥系统：需要预先设置私钥，此处传入公钥）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <returns>成功返回解密后的字节数组，失败抛出异常</returns>
        public byte[] DecryptData(byte[] encryptedData, string publicKey)
        {
            CheckDllLoaded();

            if (!IsPrivateKeySet())
            {
                throw new InvalidOperationException("私钥未设置！请先调用 InitializePrivateKey 方法。");
            }

            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("加密数据不能为空", nameof(encryptedData));
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
                inputPtr = Marshal.AllocHGlobal(encryptedData.Length);
                Marshal.Copy(encryptedData, 0, inputPtr, encryptedData.Length);

                // 调用DLL函数
                UIntPtr outputLength;
                int errorCode = dllManager.StreamDecryptData(
                    inputPtr,
                    new UIntPtr((uint)encryptedData.Length),
                    publicKey,
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
        /// 验证加密数据（双密钥系统：需要预先设置私钥，此处传入公钥）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <returns>有效返回true，无效返回false</returns>
        public bool ValidateData(byte[] encryptedData, string publicKey)
        {
            try
            {
                // 尝试解密来验证数据有效性
                byte[] decryptedData = DecryptData(encryptedData, publicKey);
                return decryptedData != null && decryptedData.Length > 0;
            }
            catch
            {
                return false;
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
                case -8: return "私钥未设置";
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