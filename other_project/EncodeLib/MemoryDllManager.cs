using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EncodeLib
{
    /// <summary>
    /// 内存DLL管理器 - 管理从内存加载的TestExportLib.dll
    /// 提供所有导出函数的委托包装
    /// </summary>
    internal class MemoryDllManager : IDisposable
    {
        private DLLFromMemory dllInstance;
        private bool disposed = false;

        // 委托定义 - 与原始DllImport声明对应
        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //public delegate void HelloWordDelegate(IntPtr intPtr, ref PDU_RSC_STATUS_ITEM pItem, byte[] p2, UInt32[] p3, PDU_RSC_STATUS_DATA p4, [MarshalAs(UnmanagedType.LPStr)] string PreselectionValue);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void HelloWord2Delegate(UInt32 hh);

        // 双密钥系统函数委托
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int InitStreamFileDelegate([MarshalAs(UnmanagedType.LPStr)] string privateKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ClearPrivateKeyDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int IsPrivateKeySetDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StreamEncryptFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string outputPath,
            [MarshalAs(UnmanagedType.LPStr)] string key,
            ProgressCallback progressCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StreamDecryptFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string outputPath,
            [MarshalAs(UnmanagedType.LPStr)] string key,
            ProgressCallback progressCallback);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetNTPTimestampDelegate(out long timestamp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetNTPTimestampFromServerDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string server,
            out long timestamp,
            int timeoutMs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long GetLocalTimestampDelegate();

        // 新增：字节数组加解密函数委托
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StreamEncryptDataDelegate(
            IntPtr inputData,
            UIntPtr inputLength,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr outputData,
            out UIntPtr outputLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StreamDecryptDataDelegate(
            IntPtr inputData,
            UIntPtr inputLength,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr outputData,
            out UIntPtr outputLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeEncryptedDataDelegate(IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeDecryptedDataDelegate(IntPtr data);

        // ========== 自包含式加密/解密函数委托（无需预设私钥） ==========
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SelfContainedEncryptFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string outputPath,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            ProgressCallback progressCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SelfContainedDecryptFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string outputPath,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            ProgressCallback progressCallback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SelfContainedEncryptDataDelegate(
            IntPtr inputData,
            UIntPtr inputLength,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr outputData,
            out UIntPtr outputLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SelfContainedDecryptDataDelegate(
            IntPtr inputData,
            UIntPtr inputLength,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr outputData,
            out UIntPtr outputLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ValidateSelfContainedFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Generate2048BitPrivateKeyDelegate(
            IntPtr privateKey,
            out int keyLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint CalculatePrivateKeyHashDelegate(
            IntPtr privateKey,
            int keyLength);

        // ========== 私钥提取函数委托 ==========
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ExtractPrivateKeyFromFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr extractedPrivateKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ExtractPrivateKeyFromDataDelegate(
            IntPtr inputData,
            UIntPtr inputLength,
            [MarshalAs(UnmanagedType.LPStr)] string publicKey,
            out IntPtr extractedPrivateKey);

        // 硬件ID获取函数委托
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetMachineFingerprintDelegate(
            [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder data);

        // 双密钥系统函数实例
        public InitStreamFileDelegate InitStreamFile { get; private set; }
        public ClearPrivateKeyDelegate ClearPrivateKey { get; private set; }
        public IsPrivateKeySetDelegate IsPrivateKeySet { get; private set; }

        public StreamEncryptFileDelegate StreamEncryptFile { get; private set; }
        public StreamDecryptFileDelegate StreamDecryptFile { get; private set; }
        public GetNTPTimestampDelegate GetNTPTimestamp { get; private set; }

        // 新增：字节数组加解密函数实例
        public StreamEncryptDataDelegate StreamEncryptData { get; private set; }
        public StreamDecryptDataDelegate StreamDecryptData { get; private set; }
        public FreeEncryptedDataDelegate FreeEncryptedData { get; private set; }
        public FreeDecryptedDataDelegate FreeDecryptedData { get; private set; }

        // ========== 自包含式加密/解密函数实例（无需预设私钥） ==========
        public SelfContainedEncryptFileDelegate SelfContainedEncryptFile { get; private set; }
        public SelfContainedDecryptFileDelegate SelfContainedDecryptFile { get; private set; }
        public SelfContainedEncryptDataDelegate SelfContainedEncryptData { get; private set; }
        public SelfContainedDecryptDataDelegate SelfContainedDecryptData { get; private set; }
        public Generate2048BitPrivateKeyDelegate Generate2048BitPrivateKey { get; private set; }

        // ========== 私钥提取函数实例 ==========
        public ExtractPrivateKeyFromFileDelegate ExtractPrivateKeyFromFile { get; private set; }
        public ExtractPrivateKeyFromDataDelegate ExtractPrivateKeyFromData { get; private set; }

        // 硬件ID获取函数实例
        public GetMachineFingerprintDelegate GetMachineFingerprint { get; private set; }

        /// <summary>
        /// 构造函数 - 从指定路径加载DLL到内存
        /// </summary>
        /// <param name="dllPath">DLL文件路径</param>
        public MemoryDllManager(string dllPath)
        {
            LoadDllFromFile(dllPath);
            InitializeFunctions();
        }

        /// <summary>
        /// 构造函数 - 从字节数组加载DLL到内存
        /// </summary>
        /// <param name="dllBytes">DLL字节数据</param>
        public MemoryDllManager(byte[] dllBytes)
        {
            LoadDllFromBytes(dllBytes);
            InitializeFunctions();
        }

        /// <summary>
        /// 从文件加载DLL
        /// </summary>
        /// <param name="dllPath">DLL文件路径</param>
        private void LoadDllFromFile(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"找不到DLL文件: {dllPath}");

            byte[] dllBytes = File.ReadAllBytes(dllPath);
            LoadDllFromBytes(dllBytes);
        }

        /// <summary>
        /// 从字节数组加载DLL
        /// </summary>
        /// <param name="dllBytes">DLL字节数据</param>
        private void LoadDllFromBytes(byte[] dllBytes)
        {
            try
            {
                dllInstance = new DLLFromMemory(dllBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"从内存加载DLL失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化所有函数委托
        /// </summary>
        private void InitializeFunctions()
        {
            try
            {
                // 双密钥系统函数
                InitStreamFile = dllInstance.GetDelegateFromFuncName<InitStreamFileDelegate>("InitStreamFile");
                ClearPrivateKey = dllInstance.GetDelegateFromFuncName<ClearPrivateKeyDelegate>("ClearPrivateKey");
                IsPrivateKeySet = dllInstance.GetDelegateFromFuncName<IsPrivateKeySetDelegate>("IsPrivateKeySet");

                // 获取所有导出函数的委托
                StreamEncryptFile = dllInstance.GetDelegateFromFuncName<StreamEncryptFileDelegate>("StreamEncryptFile");
                StreamDecryptFile = dllInstance.GetDelegateFromFuncName<StreamDecryptFileDelegate>("StreamDecryptFile");
                GetNTPTimestamp = dllInstance.GetDelegateFromFuncName<GetNTPTimestampDelegate>("GetNTPTimestamp");

                // 新增：字节数组加解密函数委托
                StreamEncryptData = dllInstance.GetDelegateFromFuncName<StreamEncryptDataDelegate>("StreamEncryptData");
                StreamDecryptData = dllInstance.GetDelegateFromFuncName<StreamDecryptDataDelegate>("StreamDecryptData");
                FreeEncryptedData = dllInstance.GetDelegateFromFuncName<FreeEncryptedDataDelegate>("FreeEncryptedData");
                FreeDecryptedData = dllInstance.GetDelegateFromFuncName<FreeDecryptedDataDelegate>("FreeDecryptedData");

                // ========== 自包含式加密/解密函数实例（无需预设私钥） ==========
                SelfContainedEncryptFile = dllInstance.GetDelegateFromFuncName<SelfContainedEncryptFileDelegate>("SelfContainedEncryptFile");
                SelfContainedDecryptFile = dllInstance.GetDelegateFromFuncName<SelfContainedDecryptFileDelegate>("SelfContainedDecryptFile");
                SelfContainedEncryptData = dllInstance.GetDelegateFromFuncName<SelfContainedEncryptDataDelegate>("SelfContainedEncryptData");
                SelfContainedDecryptData = dllInstance.GetDelegateFromFuncName<SelfContainedDecryptDataDelegate>("SelfContainedDecryptData");
                Generate2048BitPrivateKey = dllInstance.GetDelegateFromFuncName<Generate2048BitPrivateKeyDelegate>("Generate2048BitPrivateKey");

                // ========== 私钥提取函数实例 ==========
                ExtractPrivateKeyFromFile = dllInstance.GetDelegateFromFuncName<ExtractPrivateKeyFromFileDelegate>("ExtractPrivateKeyFromFile");
                ExtractPrivateKeyFromData = dllInstance.GetDelegateFromFuncName<ExtractPrivateKeyFromDataDelegate>("ExtractPrivateKeyFromData");

                // 硬件ID获取函数实例
                GetMachineFingerprint = dllInstance.GetDelegateFromFuncName<GetMachineFingerprintDelegate>("GetMachineFingerprint");
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new InvalidOperationException($"无法找到DLL导出函数: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查DLL是否已成功加载
        /// </summary>
        public bool IsLoaded => dllInstance != null && !disposed;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                dllInstance?.Close();
                dllInstance = null;
                disposed = true;
            }
        }

  
    }
}