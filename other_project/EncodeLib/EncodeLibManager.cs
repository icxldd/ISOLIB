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
        private MemoryDllManager memoryDllManager;
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
            // 优先尝试内存加载，失败则回退到Windows API加载
            //if (!InitializeMemoryDll())
            {
                InitializeWindowsDll();
            }
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
        /// 初始化内存DLL管理器，从嵌入资源加载DLL
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        private bool InitializeMemoryDll()
        {
            try
            {
                // 可能的DLL文件名
                string[] dllNames = {
                    GlobalData.DLL_NAME,
                    GlobalData.DLL_NAME_BACKUP
                };

                byte[] dllBytes = null;
                string foundDllName = null;

                // 尝试从嵌入资源加载DLL
                foreach (string dllName in dllNames)
                {
                    try
                    {
                        if (EmbeddedResourceManager.IsEmbeddedDllExists(dllName))
                        {
                            dllBytes = EmbeddedResourceManager.GetEmbeddedDllBytes(dllName);
                            foundDllName = dllName;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"尝试加载嵌入DLL {dllName} 失败: {ex.Message}");
                        continue;
                    }
                }

                if (dllBytes == null)
                {
                    System.Diagnostics.Debug.WriteLine("EncodeLib: 未找到嵌入的DLL资源");
                    return false;
                }

                // 使用内存DLL管理器加载
                memoryDllManager = new MemoryDllManager(dllBytes);

                // 验证DLL是否成功加载
                if (!memoryDllManager.IsLoaded)
                {
                    memoryDllManager?.Dispose();
                    memoryDllManager = null;
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"EncodeLib: 内存DLL加载成功！资源: {foundDllName}");
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"初始化内存DLL失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"EncodeLib错误: {errorMsg}");
                
                // 清理资源
                memoryDllManager?.Dispose();
                memoryDllManager = null;
                return false;
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
                    // 尝试从嵌入资源提取DLL到当前运行目录
                    dllPath = ExtractEmbeddedDllToCurrentDirectory();
                    
                    if (string.IsNullOrEmpty(dllPath))
                    {
                        throw new FileNotFoundException($"找不到{GlobalData.DLL_NAME}文件，且无法从嵌入资源中提取");
                    }
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
        /// 从嵌入资源提取DLL到当前运行目录
        /// </summary>
        /// <returns>成功返回DLL文件路径，失败返回null</returns>
        private string ExtractEmbeddedDllToCurrentDirectory()
        {
            try
            {
                const string dllName = GlobalData.DLL_NAME;
                
                // 检查嵌入资源中是否存在DLL
                if (!EmbeddedResourceManager.IsEmbeddedDllExists(dllName))
                {
                    System.Diagnostics.Debug.WriteLine($"嵌入资源中不存在: {dllName}");
                    return null;
                }

                // 从嵌入资源读取DLL字节数组
                byte[] dllBytes = EmbeddedResourceManager.GetEmbeddedDllBytes(dllName);
                if (dllBytes == null || dllBytes.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"从嵌入资源读取DLL失败: {dllName}");
                    return null;
                }

                // 确定输出路径（当前运行目录）
                string outputPath = Path.Combine(Environment.CurrentDirectory, dllName);

                // 如果文件已存在且大小相同，直接返回路径
                if (File.Exists(outputPath))
                {
                    FileInfo existingFile = new FileInfo(outputPath);
                    if (existingFile.Length == dllBytes.Length)
                    {
                        System.Diagnostics.Debug.WriteLine($"DLL文件已存在且大小匹配: {outputPath}");
                        return outputPath;
                    }
                }

                // 写入DLL文件到磁盘
                File.WriteAllBytes(outputPath, dllBytes);

                // 验证文件是否成功写入
                if (File.Exists(outputPath))
                {
                    FileInfo writtenFile = new FileInfo(outputPath);
                    if (writtenFile.Length == dllBytes.Length)
                    {
                        System.Diagnostics.Debug.WriteLine($"成功从嵌入资源提取DLL: {outputPath} (大小: {dllBytes.Length:N0} 字节)");
                        return outputPath;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DLL文件大小不匹配: 期望 {dllBytes.Length}, 实际 {writtenFile.Length}");
                        File.Delete(outputPath); // 删除损坏的文件
                        return null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DLL文件写入失败: {outputPath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从嵌入资源提取DLL时发生错误: {ex.Message}");
                return null;
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
                GlobalData.DLL_NAME
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
        public bool IsLoaded => (dllManager?.IsLoaded == true || memoryDllManager?.IsLoaded == true) && !disposed;

        /// <summary>
        /// 获取当前DLL管理器（内存优先）
        /// </summary>
        /// <returns>当前可用的DLL管理器接口</returns>
        private dynamic GetCurrentDllManager()
        {
            if (memoryDllManager?.IsLoaded == true)
                return memoryDllManager;
            else if (dllManager?.IsLoaded == true)
                return dllManager;
            else
                throw new InvalidOperationException("没有可用的DLL管理器");
        }

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
                var currentManager = GetCurrentDllManager();
                return currentManager.InitStreamFile(privateKey);
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
                var currentManager = GetCurrentDllManager();
                currentManager.ClearPrivateKey();
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
                var currentManager = GetCurrentDllManager();
                return currentManager.IsPrivateKeySet() == 1;
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
                var currentManager = GetCurrentDllManager();
                return currentManager.StreamEncryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib加密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 加密文件（双密钥系统：直接传入私钥和公钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="privateKey">私钥字符串</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int EncryptFile(string inputFilePath, string outputFilePath, string privateKey, string publicKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            try
            {
                // 先设置私钥
                int initResult = InitializePrivateKey(privateKey);
                if (initResult != 0)
                {
                    return initResult; // 返回初始化错误码
                }

                // 执行加密
                return EncryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
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
                var currentManager = GetCurrentDllManager();
                return currentManager.StreamDecryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib解密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 解密文件（双密钥系统：直接传入私钥和公钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="privateKey">私钥字符串</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int DecryptFile(string inputFilePath, string outputFilePath, string privateKey, string publicKey, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            try
            {
                // 先设置私钥
                int initResult = InitializePrivateKey(privateKey);
                if (initResult != 0)
                {
                    return initResult; // 返回初始化错误码
                }

                // 执行解密
                return DecryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
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
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.StreamEncryptData(
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
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeEncryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放加密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 加密字节数组（双密钥系统：直接传入私钥和公钥）
        /// </summary>
        /// <param name="inputData">输入数据字节数组</param>
        /// <param name="privateKey">私钥字符串</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <returns>成功返回加密后的字节数组，失败抛出异常</returns>
        public byte[] EncryptData(byte[] inputData, string privateKey, string publicKey)
        {
            CheckDllLoaded();

            if (inputData == null || inputData.Length == 0)
            {
                throw new ArgumentException("输入数据不能为空", nameof(inputData));
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("私钥不能为空", nameof(privateKey));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            try
            {
                // 先设置私钥
                int initResult = InitializePrivateKey(privateKey);
                if (initResult != 0)
                {
                    throw new InvalidOperationException($"初始化私钥失败，错误码: {initResult} ({GetErrorMessage(initResult)})");
                }

                // 执行加密（调用原有的方法）
                return EncryptData(inputData, publicKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib字节数组加密错误: {ex.Message}");
                throw new InvalidOperationException($"字节数组加密失败: {ex.Message}", ex);
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
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.StreamDecryptData(
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
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeDecryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放解密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 解密字节数组（双密钥系统：直接传入私钥和公钥）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="privateKey">私钥字符串</param>
        /// <param name="publicKey">公钥字符串（与私钥组合使用）</param>
        /// <returns>成功返回解密后的字节数组，失败抛出异常</returns>
        public byte[] DecryptData(byte[] encryptedData, string privateKey, string publicKey)
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

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            try
            {
                // 先设置私钥
                int initResult = InitializePrivateKey(privateKey);
                if (initResult != 0)
                {
                    throw new InvalidOperationException($"初始化私钥失败，错误码: {initResult} ({GetErrorMessage(initResult)})");
                }

                // 执行解密（调用原有的方法）
                return DecryptData(encryptedData, publicKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib字节数组解密错误: {ex.Message}");
                throw new InvalidOperationException($"字节数组解密失败: {ex.Message}", ex);
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
                var currentManager = GetCurrentDllManager();
                return currentManager.GetNTPTimestamp(out timestamp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib NTP错误: {ex.Message}");
                return -1;
            }
        }

        // ========== 自包含式加密/解密函数（无需预设私钥） ==========

        /// <summary>
        /// 自包含式文件加密函数（自动生成2048位私钥）
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="publicKey">公钥字符串</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        /// <remarks>
        /// 此函数会自动生成2048位私钥并存储在加密文件中，无需预设私钥。
        /// 与传统的双密钥系统不同，这是一个完全独立的加密系统。
        /// </remarks>
        public int SelfContainedEncryptFile(string inputFilePath, string outputFilePath, string publicKey, ProgressCallback progressCallback = null)
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
                var currentManager = GetCurrentDllManager();
                return currentManager.SelfContainedEncryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib自包含式加密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 自包含式文件解密函数（从文件中读取私钥）
        /// </summary>
        /// <param name="inputFilePath">输入加密文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="publicKey">公钥字符串</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        /// <remarks>
        /// 此函数会从加密文件中读取私钥并验证其完整性，然后进行解密。
        /// 如果私钥被篡改，解密将失败。
        /// </remarks>
        public int SelfContainedDecryptFile(string inputFilePath, string outputFilePath, string publicKey, ProgressCallback progressCallback = null)
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
                var currentManager = GetCurrentDllManager();
                return currentManager.SelfContainedDecryptFile(inputFilePath, outputFilePath, publicKey, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib自包含式解密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 自包含式数据加密函数（自动生成2048位私钥）
        /// </summary>
        /// <param name="inputData">输入数据字节数组</param>
        /// <param name="publicKey">公钥字符串</param>
        /// <returns>成功返回加密后的字节数组，失败抛出异常</returns>
        /// <remarks>
        /// 此函数会自动生成2048位私钥并包含在加密数据中，无需预设私钥。
        /// 返回的数据包含完整的加密信息，可直接用于存储或传输。
        /// </remarks>
        public byte[] SelfContainedEncryptData(byte[] inputData, string publicKey)
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
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.SelfContainedEncryptData(
                    inputPtr,
                    new UIntPtr((uint)inputData.Length),
                    publicKey,
                    out outputPtr,
                    out outputLength);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"自包含式加密失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (outputPtr == IntPtr.Zero || outputLength.ToUInt32() == 0)
                {
                    throw new InvalidOperationException("自包含式加密返回空数据");
                }

                // 立即复制到C#字节数组
                int length = (int)outputLength.ToUInt32();
                result = new byte[length];
                Marshal.Copy(outputPtr, result, 0, length);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib自包含式字节数组加密错误: {ex.Message}");
                throw new InvalidOperationException($"自包含式字节数组加密失败: {ex.Message}", ex);
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
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeEncryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放自包含式加密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 自包含式数据解密函数（从数据中读取私钥）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="publicKey">公钥字符串</param>
        /// <returns>成功返回解密后的字节数组，失败抛出异常</returns>
        /// <remarks>
        /// 此函数会从加密数据中读取私钥并验证其完整性，然后进行解密。
        /// 如果私钥被篡改，解密将失败。
        /// </remarks>
        public byte[] SelfContainedDecryptData(byte[] encryptedData, string publicKey)
        {
            CheckDllLoaded();

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
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.SelfContainedDecryptData(
                    inputPtr,
                    new UIntPtr((uint)encryptedData.Length),
                    publicKey,
                    out outputPtr,
                    out outputLength);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"自包含式解密失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (outputPtr == IntPtr.Zero || outputLength.ToUInt32() == 0)
                {
                    throw new InvalidOperationException("自包含式解密返回空数据");
                }

                // 立即复制到C#字节数组
                int length = (int)outputLength.ToUInt32();
                result = new byte[length];
                Marshal.Copy(outputPtr, result, 0, length);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib自包含式字节数组解密错误: {ex.Message}");
                throw new InvalidOperationException($"自包含式字节数组解密失败: {ex.Message}", ex);
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
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeDecryptedData(outputPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放自包含式解密数据内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 生成2048位随机私钥（辅助函数）
        /// </summary>
        /// <returns>成功返回私钥字节数组，失败抛出异常</returns>
        /// <remarks>
        /// 此函数用于生成高质量的2048位随机私钥，主要用于测试和调试目的。
        /// 在正常使用中，私钥由加密函数自动生成。
        /// </remarks>
        public byte[] Generate2048BitPrivateKey()
        {
            CheckDllLoaded();

            IntPtr privateKeyPtr = IntPtr.Zero;
            byte[] result = null;

            try
            {
                // 分配256字节缓冲区用于2048位私钥
                const int PRIVATE_KEY_SIZE = 256;
                privateKeyPtr = Marshal.AllocHGlobal(PRIVATE_KEY_SIZE);

                int keyLength;
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.Generate2048BitPrivateKey(privateKeyPtr, out keyLength);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"生成私钥失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (keyLength <= 0 || keyLength > PRIVATE_KEY_SIZE)
                {
                    throw new InvalidOperationException($"私钥长度无效: {keyLength}");
                }

                // 复制私钥到C#字节数组
                result = new byte[keyLength];
                Marshal.Copy(privateKeyPtr, result, 0, keyLength);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib生成私钥错误: {ex.Message}");
                throw new InvalidOperationException($"生成2048位私钥失败: {ex.Message}", ex);
            }
            finally
            {
                // 安全清理私钥内存
                if (privateKeyPtr != IntPtr.Zero)
                {
                    // 清零内存后释放
                    for (int i = 0; i < 256; i++)
                    {
                        Marshal.WriteByte(privateKeyPtr, i, 0);
                    }
                    Marshal.FreeHGlobal(privateKeyPtr);
                }
            }
        }

        // ========== 硬件ID获取函数 ==========

        /// <summary>
        /// 获取机器指纹（硬件ID组合哈希）
        /// </summary>
        /// <returns>成功返回机器指纹字符串，失败抛出异常</returns>
        /// <remarks>
        /// 此函数会获取多个硬件标识符（Windows产品ID、硬盘序列号、MAC地址、主板序列号、CPU ID、BIOS序列号）
        /// 并将它们组合生成一个唯一的机器指纹。该指纹可用于硬件锁定、许可验证等用途。
        /// 指纹格式：XXXXXXXX-XXXX-XXXX（基于硬件ID的哈希值）
        /// </remarks>
        public string GetMachineFingerprint()
        {
            CheckDllLoaded();

            try
            {
                // 创建256字节的StringBuilder用于接收机器指纹
                System.Text.StringBuilder fingerprint = new System.Text.StringBuilder(256);

                // 调用DLL函数获取机器指纹
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.GetMachineFingerprint(fingerprint);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"获取机器指纹失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                string result = fingerprint.ToString().Trim();
                
                if (string.IsNullOrEmpty(result))
                {
                    throw new InvalidOperationException("获取的机器指纹为空");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib获取机器指纹错误: {ex.Message}");
                throw new InvalidOperationException($"获取机器指纹失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取机器指纹（带错误处理的安全版本）
        /// </summary>
        /// <param name="fingerprint">输出机器指纹字符串</param>
        /// <returns>成功返回true，失败返回false</returns>
        /// <remarks>
        /// 此函数是GetMachineFingerprint的安全版本，不会抛出异常，而是通过返回值指示操作结果。
        /// 适用于需要优雅处理错误的场景。
        /// </remarks>
        public bool TryGetMachineFingerprint(out string fingerprint)
        {
            fingerprint = string.Empty;

            try
            {
                fingerprint = GetMachineFingerprint();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib尝试获取机器指纹失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查DLL是否已加载的辅助方法
        /// </summary>
        private void CheckDllLoaded()
        {
            if ((dllManager == null || !dllManager.IsLoaded) && 
                (memoryDllManager == null || !memoryDllManager.IsLoaded) || 
                disposed)
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
                
                memoryDllManager?.Dispose();
                memoryDllManager = null;
                
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

        // ========== 私钥提取函数 ==========

        /// <summary>
        /// 从自包含式加密文件中提取私钥
        /// </summary>
        /// <param name="inputFilePath">加密文件路径</param>
        /// <param name="publicKey">公钥字符串（用于验证）</param>
        /// <returns>成功返回私钥十六进制字符串，失败抛出异常</returns>
        /// <remarks>
        /// 此函数从自包含式加密文件中提取私钥，并验证公钥匹配性和私钥完整性。
        /// 私钥以十六进制字符串格式返回，便于存储和传输。
        /// </remarks>
        public string ExtractPrivateKeyFromFile(string inputFilePath, string publicKey)
        {
            CheckDllLoaded();

            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentException("输入文件路径不能为空", nameof(inputFilePath));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"输入文件不存在: {inputFilePath}");
            }

            IntPtr privateKeyPtr = IntPtr.Zero;
            string result = null;

            try
            {
                // 调用DLL函数
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.ExtractPrivateKeyFromFile(inputFilePath, publicKey, out privateKeyPtr);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"提取私钥失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (privateKeyPtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException("提取的私钥为空");
                }

                // 将C字符串转换为C#字符串
                result = Marshal.PtrToStringAnsi(privateKeyPtr);

                if (string.IsNullOrEmpty(result))
                {
                    throw new InvalidOperationException("私钥字符串为空");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib从文件提取私钥错误: {ex.Message}");
                throw new InvalidOperationException($"从文件提取私钥失败: {ex.Message}", ex);
            }
            finally
            {
                // 释放DLL分配的内存
                if (privateKeyPtr != IntPtr.Zero)
                {
                    try
                    {
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeDecryptedData(privateKeyPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放私钥内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 从自包含式加密数据中提取私钥
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="publicKey">公钥字符串（用于验证）</param>
        /// <returns>成功返回私钥十六进制字符串，失败抛出异常</returns>
        /// <remarks>
        /// 此函数从自包含式加密数据中提取私钥，并验证公钥匹配性和私钥完整性。
        /// 私钥以十六进制字符串格式返回，便于存储和传输。
        /// </remarks>
        public string ExtractPrivateKeyFromData(byte[] encryptedData, string publicKey)
        {
            CheckDllLoaded();

            if (encryptedData == null || encryptedData.Length == 0)
            {
                throw new ArgumentException("加密数据不能为空", nameof(encryptedData));
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("公钥不能为空", nameof(publicKey));
            }

            IntPtr inputPtr = IntPtr.Zero;
            IntPtr privateKeyPtr = IntPtr.Zero;
            string result = null;

            try
            {
                // 分配非托管内存用于输入数据
                inputPtr = Marshal.AllocHGlobal(encryptedData.Length);
                Marshal.Copy(encryptedData, 0, inputPtr, encryptedData.Length);

                // 调用DLL函数
                var currentManager = GetCurrentDllManager();
                int errorCode = currentManager.ExtractPrivateKeyFromData(
                    inputPtr,
                    new UIntPtr((uint)encryptedData.Length),
                    publicKey,
                    out privateKeyPtr);

                if (errorCode != 0)
                {
                    throw new InvalidOperationException($"提取私钥失败，错误码: {errorCode} ({GetErrorMessage(errorCode)})");
                }

                if (privateKeyPtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException("提取的私钥为空");
                }

                // 将C字符串转换为C#字符串
                result = Marshal.PtrToStringAnsi(privateKeyPtr);

                if (string.IsNullOrEmpty(result))
                {
                    throw new InvalidOperationException("私钥字符串为空");
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib从数据提取私钥错误: {ex.Message}");
                throw new InvalidOperationException($"从数据提取私钥失败: {ex.Message}", ex);
            }
            finally
            {
                // 释放分配的内存
                if (inputPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputPtr);
                }

                if (privateKeyPtr != IntPtr.Zero)
                {
                    try
                    {
                        var currentManager = GetCurrentDllManager();
                        currentManager.FreeDecryptedData(privateKeyPtr);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放私钥内存失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 从自包含式加密文件中提取私钥（带错误处理的安全版本）
        /// </summary>
        /// <param name="inputFilePath">加密文件路径</param>
        /// <param name="publicKey">公钥字符串（用于验证）</param>
        /// <param name="privateKey">输出私钥十六进制字符串</param>
        /// <returns>成功返回true，失败返回false</returns>
        /// <remarks>
        /// 此函数是ExtractPrivateKeyFromFile的安全版本，不会抛出异常，而是通过返回值指示操作结果。
        /// 适用于需要优雅处理错误的场景。
        /// </remarks>
        public bool TryExtractPrivateKeyFromFile(string inputFilePath, string publicKey, out string privateKey)
        {
            privateKey = string.Empty;

            try
            {
                privateKey = ExtractPrivateKeyFromFile(inputFilePath, publicKey);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib尝试从文件提取私钥失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从自包含式加密数据中提取私钥（带错误处理的安全版本）
        /// </summary>
        /// <param name="encryptedData">加密数据字节数组</param>
        /// <param name="publicKey">公钥字符串（用于验证）</param>
        /// <param name="privateKey">输出私钥十六进制字符串</param>
        /// <returns>成功返回true，失败返回false</returns>
        /// <remarks>
        /// 此函数是ExtractPrivateKeyFromData的安全版本，不会抛出异常，而是通过返回值指示操作结果。
        /// 适用于需要优雅处理错误的场景。
        /// </remarks>
        public bool TryExtractPrivateKeyFromData(byte[] encryptedData, string publicKey, out string privateKey)
        {
            privateKey = string.Empty;

            try
            {
                privateKey = ExtractPrivateKeyFromData(encryptedData, publicKey);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib尝试从数据提取私钥失败: {ex.Message}");
                return false;
            }
        }
    }
}