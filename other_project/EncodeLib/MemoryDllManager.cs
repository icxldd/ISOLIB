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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void HelloWordDelegate(IntPtr intPtr, ref PDU_RSC_STATUS_ITEM pItem, byte[] p2, UInt32[] p3, PDU_RSC_STATUS_DATA p4, [MarshalAs(UnmanagedType.LPStr)] string PreselectionValue);

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
        public delegate int ValidateEncryptedFileDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string key);

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

    // 支持的结构体定义
    public enum T_PDU_IT
    {
        PDU_IT_IO_UNUM32 = 0x1000,
        PDU_IT_IO_PROG_VOLTAGE = 0x1001,
        PDU_IT_IO_BYTEARRAY = 0x1002,
        PDU_IT_IO_FILTER = 0x1003,
        PDU_IT_IO_EVENT_QUEUE_PROPERTY = 0x1004,
        PDU_IT_RSC_STATUS = 0x1100,
        PDU_IT_PARAM = 0x1200,
        PDU_IT_RESULT = 0x1300,
        PDU_IT_STATUS = 0x1301,
        PDU_IT_ERROR = 0x1302,
        PDU_IT_INFO = 0x1303,
        PDU_IT_RSC_ID = 0x1400,
        PDU_IT_RSC_CONFLICT = 0x1500,
        PDU_IT_MODULE_ID = 0x1600,
        PDU_IT_UNIQUE_RESP_ID_TABLE = 0x1700,
        PDU_IT_VEHICLE_ID = 0x1800,
        PDU_IT_ETH_SWITCH_STATE = 0x1801
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PDU_RSC_STATUS_DATA
    {
        public uint hMod;
        public uint ResourceId;
        public uint ResourceStatus;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PDU_RSC_STATUS_ITEM
    {
        public T_PDU_IT ItemType;
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
        public uint NumEntries;
        public PDU_RSC_STATUS_DATA pResourceStatusData;
    }

    // 进度回调委托定义
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProgressCallback([MarshalAs(UnmanagedType.LPStr)] string filePath, double progress);
} 