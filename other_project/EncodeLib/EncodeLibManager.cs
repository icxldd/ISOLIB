using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EncodeLib
{
    /// <summary>
    /// EncodeLib主管理器类 - 对外提供加密解密和内存DLL加载服务
    /// 封装所有TestExportLib功能，提供简单易用的API
    /// </summary>
    public class EncodeLibManager : IDisposable
    {
        private MemoryDllManager dllManager;
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
            InitializeMemoryDll(GobalData.DllData);
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
        /// 从Base64字符串初始化内存DLL（完全无文件痕迹）
        /// </summary>
        /// <param name="base64DllData">DLL的Base64字符串数据</param>
        private void InitializeMemoryDll(string base64DllData)
        {
            try
            {
                // 从Base64转换为字节数组
                byte[] dllBytes = ConvertBase64ToBytes(base64DllData);

                if (dllBytes == null || dllBytes.Length == 0)
                {
                    throw new InvalidOperationException("Base64转换后的字节数组为空");
                }

                System.Diagnostics.Debug.WriteLine($"EncodeLib: 从Base64加载DLL，大小: {dllBytes.Length:N0} 字节");

                // 从字节数组创建内存DLL管理器（真正的内存加载）
                dllManager = new MemoryDllManager(dllBytes);

                // 验证DLL是否成功加载
                if (!dllManager.IsLoaded)
                {
                    throw new InvalidOperationException("DLL从内存加载失败");
                }

                // 立即清除内存中的DLL字节数组痕迹
                ClearMemoryTraces(dllBytes);

                System.Diagnostics.Debug.WriteLine("EncodeLib: DLL从Base64无痕加载成功！");
            }
            catch (Exception ex)
            {
                string errorMsg = $"从Base64初始化内存DLL失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"EncodeLib错误: {errorMsg}");
            }
        }

        /// <summary>
        /// 检查DLL是否已加载
        /// </summary>
        public bool IsLoaded => dllManager?.IsLoaded == true && !disposed;

        /// <summary>
        /// 加密文件
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="key">加密密钥</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int EncryptFile(string inputFilePath, string outputFilePath, string key, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            try
            {
                return dllManager.StreamEncryptFile(inputFilePath, outputFilePath, key, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib加密错误: {ex.Message}");
                return -1; // 返回通用错误码
            }
        }

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <param name="key">解密密钥</param>
        /// <param name="progressCallback">进度回调函数（可选）</param>
        /// <returns>成功返回0，失败返回错误码</returns>
        public int DecryptFile(string inputFilePath, string outputFilePath, string key, ProgressCallback progressCallback = null)
        {
            CheckDllLoaded();

            try
            {
                return dllManager.StreamDecryptFile(inputFilePath, outputFilePath, key, progressCallback);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EncodeLib解密错误: {ex.Message}");
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