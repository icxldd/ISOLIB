using System;
using System.IO;
using System.Reflection;

namespace TestWin
{
    /// <summary>
    /// 嵌入资源管理器 - 用于从程序集中读取嵌入的DLL文件
    /// 实现完全无硬盘痕迹的DLL加载
    /// </summary>
    public static class EmbeddedResourceManager
    {
        /// <summary>
        /// 从嵌入资源中读取DLL字节数组
        /// </summary>
        /// <param name="dllName">DLL文件名（如：TestExportLib.vmp.dll）</param>
        /// <returns>DLL的字节数组</returns>
        public static byte[] GetEmbeddedDllBytes(string dllName)
        {
            try
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                
                // 方法1：直接使用逻辑名称
                using (Stream stream = currentAssembly.GetManifestResourceStream(dllName))
                {
                    if (stream != null)
                    {
                        return ReadStreamToBytes(stream);
                    }
                }

                // 方法2：使用完整的资源名称
                string fullResourceName = $"{currentAssembly.GetName().Name}.{dllName}";
                using (Stream stream = currentAssembly.GetManifestResourceStream(fullResourceName))
                {
                    if (stream != null)
                    {
                        return ReadStreamToBytes(stream);
                    }
                }

                // 方法3：搜索所有资源名称，找到匹配的
                string[] resourceNames = currentAssembly.GetManifestResourceNames();
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName.EndsWith(dllName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                return ReadStreamToBytes(stream);
                            }
                        }
                    }
                }

                throw new FileNotFoundException($"无法在嵌入资源中找到DLL文件: {dllName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取嵌入DLL资源失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有嵌入资源的名称列表（用于调试）
        /// </summary>
        /// <returns>资源名称数组</returns>
        public static string[] GetAllEmbeddedResourceNames()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            return currentAssembly.GetManifestResourceNames();
        }

        /// <summary>
        /// 检查指定的DLL是否存在于嵌入资源中
        /// </summary>
        /// <param name="dllName">DLL文件名</param>
        /// <returns>是否存在</returns>
        public static bool IsEmbeddedDllExists(string dllName)
        {
            try
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = currentAssembly.GetManifestResourceNames();
                
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName.EndsWith(dllName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将流读取为字节数组
        /// </summary>
        /// <param name="stream">输入流</param>
        /// <returns>字节数组</returns>
        private static byte[] ReadStreamToBytes(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 获取嵌入资源的详细信息（用于调试）
        /// </summary>
        /// <returns>资源信息字符串</returns>
        public static string GetEmbeddedResourceInfo()
        {
            try
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = currentAssembly.GetManifestResourceNames();
                
                string info = $"程序集: {currentAssembly.GetName().Name}\n";
                info += $"嵌入资源数量: {resourceNames.Length}\n\n";
                
                foreach (string resourceName in resourceNames)
                {
                    using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
                    {
                        long size = stream?.Length ?? 0;
                        info += $"资源: {resourceName} (大小: {size:N0} 字节)\n";
                    }
                }
                
                return info;
            }
            catch (Exception ex)
            {
                return $"获取资源信息失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 尝试从多个可能的位置加载DLL字节数组
        /// 优先级：嵌入资源 > 硬盘文件
        /// </summary>
        /// <param name="dllName">DLL文件名</param>
        /// <param name="fallbackPaths">备用硬盘路径</param>
        /// <returns>DLL字节数组</returns>
        public static byte[] GetDllBytesWithFallback(string dllName, params string[] fallbackPaths)
        {
            // 优先尝试从嵌入资源加载
            try
            {
                if (IsEmbeddedDllExists(dllName))
                {
                    return GetEmbeddedDllBytes(dllName);
                }
            }
            catch
            {
                // 嵌入资源加载失败，继续尝试硬盘文件
            }

            // 尝试从硬盘文件加载（作为备用方案）
            foreach (string path in fallbackPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllBytes(path);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            throw new FileNotFoundException($"无法找到DLL文件: {dllName}，已尝试嵌入资源和以下路径:\n{string.Join("\n", fallbackPaths)}");
        }
    }
} 