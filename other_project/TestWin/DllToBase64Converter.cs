using System;
using System.IO;

namespace TestWin
{
    /// <summary>
    /// DLL转Base64转换器 - 将DLL文件转换为Base64字符串，可直接硬编码到代码中
    /// 用于完全无文件依赖的DLL分发
    /// </summary>
    public static class DllToBase64Converter
    {
        /// <summary>
        /// 将DLL文件转换为Base64字符串
        /// </summary>
        /// <param name="dllFilePath">DLL文件路径</param>
        /// <returns>Base64编码的字符串</returns>
        public static string ConvertDllToBase64(string dllFilePath)
        {
            if (!File.Exists(dllFilePath))
                throw new FileNotFoundException($"DLL文件不存在: {dllFilePath}");

            byte[] dllBytes = File.ReadAllBytes(dllFilePath);
            return Convert.ToBase64String(dllBytes);
        }

        /// <summary>
        /// 从Base64字符串还原DLL字节数组
        /// </summary>
        /// <param name="base64String">Base64编码的字符串</param>
        /// <returns>DLL字节数组</returns>
        public static byte[] ConvertBase64ToDllBytes(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                throw new ArgumentException("Base64字符串不能为空");

            return Convert.FromBase64String(base64String);
        }

        /// <summary>
        /// 生成C#代码字符串，包含硬编码的DLL Base64数据
        /// </summary>
        /// <param name="dllFilePath">DLL文件路径</param>
        /// <param name="className">生成的类名</param>
        /// <param name="variableName">变量名</param>
        /// <returns>C#代码字符串</returns>
        public static string GenerateCSharpCode(string dllFilePath, string className = "EmbeddedDllData", string variableName = "TestExportLibDll")
        {
            string base64Data = ConvertDllToBase64(dllFilePath);
            FileInfo fileInfo = new FileInfo(dllFilePath);
            
            string code = $@"using System;

namespace TestWin
{{
    /// <summary>
    /// 嵌入式DLL数据 - 自动生成的类
    /// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
    /// 原始文件: {fileInfo.Name}
    /// 文件大小: {fileInfo.Length:N0} 字节
    /// </summary>
    public static class {className}
    {{
        /// <summary>
        /// {fileInfo.Name} 的Base64编码数据
        /// </summary>
        public static readonly string {variableName}Base64 = @""
{SplitBase64String(base64Data, 100)}"";

        /// <summary>
        /// 获取 {fileInfo.Name} 的字节数组
        /// </summary>
        /// <returns>DLL字节数组</returns>
        public static byte[] Get{variableName}Bytes()
        {{
            return Convert.FromBase64String({variableName}Base64);
        }}
    }}
}}";
            return code;
        }

        /// <summary>
        /// 将长Base64字符串分割成多行，便于代码阅读
        /// </summary>
        /// <param name="base64String">Base64字符串</param>
        /// <param name="lineLength">每行长度</param>
        /// <returns>分割后的字符串</returns>
        private static string SplitBase64String(string base64String, int lineLength)
        {
            if (string.IsNullOrEmpty(base64String))
                return string.Empty;

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < base64String.Length; i += lineLength)
            {
                if (i > 0) lines.AppendLine();
                
                int length = Math.Min(lineLength, base64String.Length - i);
                lines.Append(base64String.Substring(i, length));
            }
            
            return lines.ToString();
        }

        /// <summary>
        /// 将DLL转换为Base64并保存为C#代码文件
        /// </summary>
        /// <param name="dllFilePath">DLL文件路径</param>
        /// <param name="outputCsFilePath">输出的C#文件路径</param>
        /// <param name="className">生成的类名</param>
        /// <param name="variableName">变量名</param>
        public static void SaveAsCSFile(string dllFilePath, string outputCsFilePath, string className = "EmbeddedDllData", string variableName = "TestExportLibDll")
        {
            string code = GenerateCSharpCode(dllFilePath, className, variableName);
            File.WriteAllText(outputCsFilePath, code, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 一键转换工具 - 将指定DLL转换为嵌入代码
        /// </summary>
        public static void ConvertDllForEmbedding()
        {
            try
            {
                string dllPath = @"D:\work\code\ISOLib\other_project\TestWin\bin\Debug\TestExportLib.vmp.dll";
                string outputPath = @"D:\work\code\ISOLib\other_project\TestWin\EmbeddedDllData.cs";
                
                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"错误：找不到DLL文件 {dllPath}");
                    return;
                }

                SaveAsCSFile(dllPath, outputPath);
                
                FileInfo dllInfo = new FileInfo(dllPath);
                FileInfo csInfo = new FileInfo(outputPath);
                
                Console.WriteLine($"DLL转换完成！");
                Console.WriteLine($"原始DLL: {dllInfo.Name} ({dllInfo.Length:N0} 字节)");
                Console.WriteLine($"生成文件: {csInfo.Name} ({csInfo.Length:N0} 字节)");
                Console.WriteLine($"输出路径: {outputPath}");
                Console.WriteLine();
                Console.WriteLine("使用方法:");
                Console.WriteLine("byte[] dllBytes = EmbeddedDllData.GetTestExportLibDllBytes();");
                Console.WriteLine("var dllManager = new MemoryDllManager(dllBytes);");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换失败: {ex.Message}");
            }
        }
    }
} 