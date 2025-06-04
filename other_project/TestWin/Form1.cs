using ISOLib.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestWin
{

    public partial class Form1 : Form
    {
        // 内存DLL管理器实例
        private MemoryDllManager dllManager;
        private bool disposed = false;

        public Form1()
        {
            InitializeComponent();
            
            // 初始化内存DLL管理器
            InitializeMemoryDll();
        }

        /// <summary>
        /// 初始化内存DLL管理器，从文件加载DLL到内存
        /// </summary>
        private void InitializeMemoryDll()
        {
            try
            {
                // 构建DLL文件路径 - 假设DLL在应用程序目录或bin目录下
                string appDir = Application.StartupPath;
                string dllPath = System.IO.Path.Combine(appDir, "TestExportLib.vmp.dll");
                
                // 如果主目录找不到，尝试其他常见位置
                if (!System.IO.File.Exists(dllPath))
                {
                    // 尝试相对路径
                    string[] possiblePaths = {
                        "TestExportLib.vmp.dll",
                        @"..\..\..\TestExportLib\bin\Debug\TestExportLib.vmp.dll",
                        @"..\..\..\TestExportLib\bin\Release\TestExportLib.vmp.dll",
                        @"..\..\TestExportLib\TestExportLib.vmp.dll"
                    };
                    
                    foreach (string path in possiblePaths)
                    {
                        string fullPath = System.IO.Path.Combine(appDir, path);
                        if (System.IO.File.Exists(fullPath))
                        {
                            dllPath = fullPath;
                            break;
                        }
                    }
                }

                if (!System.IO.File.Exists(dllPath))
                {
                    MessageBox.Show($"找不到DLL文件: TestExportLib.vmp.dll\n搜索路径: {dllPath}\n\n请确保DLL文件存在于应用程序目录中。", 
                        "DLL加载错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 从内存加载DLL
                dllManager = new MemoryDllManager(dllPath);
                
                // 验证DLL是否成功加载
                if (dllManager.IsLoaded)
                {
                    // 可选：在状态栏或日志中显示加载成功信息
                    this.Text += " - DLL已从内存加载";
                }
                else
                {
                    MessageBox.Show("DLL从内存加载失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化内存DLL失败: {ex.Message}\n\n详细错误:\n{ex}", 
                    "DLL加载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dllManager = null;
            }
        }

        /// <summary>
        /// 检查DLL是否已加载的辅助方法
        /// </summary>
        private bool CheckDllLoaded()
        {
            if (dllManager == null || !dllManager.IsLoaded)
            {
                MessageBox.Show("DLL未加载或加载失败！请重启应用程序。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        //加密
        private void button1_Click(object sender, EventArgs e)
        {
            if (!CheckDllLoaded()) return;

            try
            {
                string inputFile = textBox1.Text;
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要加密的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 根据inputFile同目录生成加密文件路径
                string directory = System.IO.Path.GetDirectoryName(inputFile);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(inputFile);
                string extension = System.IO.Path.GetExtension(inputFile);
                string encryptedFile = System.IO.Path.Combine(directory, fileName + extension + ".encrypted");

                string key = textBox2.Text;
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("请输入密钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 检查输入文件是否存在
                if (!System.IO.File.Exists(inputFile))
                {
                    MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 加密文件 - 使用内存DLL管理器
                richTextBox1.Clear(); // 清空之前的日志
                richTextBox1.AppendText($"开始加密文件: {System.IO.Path.GetFileName(inputFile)}\r\n");
                
                int result = dllManager.StreamEncryptFile(inputFile, encryptedFile, key, OnProgress);
                
                if (result == 0)
                {
                    richTextBox1.AppendText($"加密完成！文件保存在: {encryptedFile}\r\n");
                    MessageBox.Show($"文件加密成功！\n加密文件保存在：{encryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    richTextBox1.AppendText($"加密失败！错误码: {result}\r\n");
                    MessageBox.Show($"文件加密失败！错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            lala();
        }

        // 进度回调方法 - 改为实例方法，支持UI线程安全更新
        private void OnProgress(string filePath, double progress)
        {
            // progress 是 0.0 到 1.0 的小数，1.0 表示 100%
            int percent = (int)(progress * 100);
            
            // 检查是否需要跨线程调用
            if (this.InvokeRequired)
            {
                // 在UI线程上执行更新
                this.Invoke(new Action(() =>
                {
                    richTextBox1.AppendText($"文件 {System.IO.Path.GetFileName(filePath)} 处理进度: {percent}%\r\n");
                    richTextBox1.ScrollToCaret(); // 自动滚动到最新内容
                }));
            }
            else
            {
                // 已经在UI线程上，直接更新
                richTextBox1.AppendText($"文件 {System.IO.Path.GetFileName(filePath)} 处理进度: {percent}%\r\n");
                richTextBox1.ScrollToCaret(); // 自动滚动到最新内容
            }
        }
        
        static void lala()
        {
            // 原来的测试代码已移动到三个按钮的点击事件中
            // TestCryptoFunctions();

            //// 创建 PDU_RSC_STATUS_ITEM 结构体实例
        }

        // 获取错误消息的辅助方法
        private static string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0: return "成功";
                case -1: return "文件打开失败";
                case -2: return "内存分配失败";
                case -3: return "加密失败";
                case -4: return "解密失败";
                case -5: return "无效文件头";
                case -6: return "线程创建失败";
                default: return "未知错误";
            }
        }

        //解密
        private void button2_Click(object sender, EventArgs e)
        {
            if (!CheckDllLoaded()) return;

            try
            {
                string inputFile = textBox1.Text;
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要解密的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string key = textBox2.Text;
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("请输入密钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 检查输入文件是否存在
                if (!System.IO.File.Exists(inputFile))
                {
                    MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 生成解密后的文件路径
                string directory = System.IO.Path.GetDirectoryName(inputFile);
                string fileName = System.IO.Path.GetFileName(inputFile);
                string decryptedFile;

                // 如果是.encrypted文件，则去掉.encrypted后缀
                if (fileName.EndsWith(".encrypted"))
                {
                    decryptedFile = System.IO.Path.Combine(directory, fileName.Substring(0, fileName.Length - 10)); // 去掉".encrypted"
                }
                else
                {
                    // 如果不是.encrypted文件，添加_decrypted后缀
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(inputFile);
                    string extension = System.IO.Path.GetExtension(inputFile);
                    decryptedFile = System.IO.Path.Combine(directory, fileNameWithoutExt + "_decrypted" + extension);
                }

                // 解密文件 - 使用内存DLL管理器
                richTextBox1.Clear(); // 清空之前的日志
                richTextBox1.AppendText($"开始解密文件: {System.IO.Path.GetFileName(inputFile)}\r\n");
                
                int result = dllManager.StreamDecryptFile(inputFile, decryptedFile, key, OnProgress);
                
                if (result == 0)
                {
                    richTextBox1.AppendText($"解密完成！文件保存在: {decryptedFile}\r\n");
                    MessageBox.Show($"文件解密成功！\n解密文件保存在：{decryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = GetErrorMessage(result);
                    richTextBox1.AppendText($"解密失败！错误: {errorMsg} (错误码: {result})\r\n");
                    MessageBox.Show($"文件解密失败！\n错误: {errorMsg} (错误码: {result})", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //验证文件是否有效加密文件
        private void button3_Click(object sender, EventArgs e)
        {
            if (!CheckDllLoaded()) return;

            try
            {
                string inputFile = textBox1.Text;
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要验证的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string key = textBox2.Text;
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("请输入密钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 检查输入文件是否存在
                if (!System.IO.File.Exists(inputFile))
                {
                    MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 验证加密文件 - 使用内存DLL管理器
                int result = dllManager.ValidateEncryptedFile(inputFile, key);
                if (result == 1)
                {
                    MessageBox.Show("文件验证成功！\n这是一个有效的加密文件，并且密钥正确。", "验证成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("文件验证失败！\n可能的原因：\n1. 这不是一个加密文件\n2. 文件已损坏\n3. 密钥错误", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // 先测试NTP时间同步
                TestNTPSync();
                
                // 创建文件选择对话框
                OpenFileDialog openFileDialog = new OpenFileDialog();

                // 设置对话框属性
                openFileDialog.Title = "选择要处理的文件";
                openFileDialog.Filter = "所有文件 (*.*)|*.*|文本文件 (*.txt)|*.txt|加密文件 (*.encrypted)|*.encrypted";
                openFileDialog.FilterIndex = 1; // 默认选择"所有文件"
                openFileDialog.RestoreDirectory = true; // 记住上次打开的目录
                openFileDialog.CheckFileExists = true; // 检查文件是否存在
                openFileDialog.CheckPathExists = true; // 检查路径是否存在

                // 显示对话框
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 将选择的文件路径填入textBox1
                    textBox1.Text = openFileDialog.FileName;

                    // 可选：显示选择成功的提示
                    // MessageBox.Show($"已选择文件：{openFileDialog.FileName}", "文件选择", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作过程中发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 测试NTP时间同步
        private void TestNTPSync()
        {
            if (!CheckDllLoaded()) return;

            try
            {
                richTextBox1.AppendText("=== NTP时间同步测试 ===\r\n");
                
                long timestamp;
                int result = dllManager.GetNTPTimestamp(out timestamp);
                
                if (result == 0) // NTP_SUCCESS
                {
                    // 转换为可读的时间格式
                    DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                    DateTime localTime = utcTime.ToLocalTime();
                    
                    richTextBox1.AppendText($"NTP同步成功！\r\n");
                    richTextBox1.AppendText($"Unix时间戳: {timestamp}\r\n");
                    richTextBox1.AppendText($"UTC时间: {utcTime:yyyy-MM-dd HH:mm:ss}\r\n");
                    richTextBox1.AppendText($"本地时间: {localTime:yyyy-MM-dd HH:mm:ss}\r\n");
                }
                else
                {
                    string errorMsg = GetNTPErrorMessage(result);
                    richTextBox1.AppendText($"NTP同步失败: {errorMsg} (错误码: {result})\r\n");
                    
                    // 显示本地时间作为备用
                    long localTimestamp = dllManager.GetLocalTimestamp();
                    DateTime localTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(localTimestamp).ToLocalTime();
                    richTextBox1.AppendText($"使用本地时间: {localTimestamp} ({localTime:yyyy-MM-dd HH:mm:ss})\r\n");
                }
                
                richTextBox1.AppendText("\r\n");
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"NTP测试异常: {ex.Message}\r\n");
            }
        }

        // 测试指定服务器NTP同步
        private void TestNTPSyncFromServer(string server)
        {
            if (!CheckDllLoaded()) return;

            try
            {
                richTextBox1.AppendText($"=== 测试服务器: {server} ===\r\n");
                
                long timestamp;
                int result = dllManager.GetNTPTimestampFromServer(server, out timestamp, 5000); // 5秒超时
                
                if (result == 0) // NTP_SUCCESS
                {
                    DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                    DateTime localTime = utcTime.ToLocalTime();
                    
                    richTextBox1.AppendText($"服务器 {server} 同步成功！\r\n");
                    richTextBox1.AppendText($"时间戳: {timestamp}, 时间: {localTime:yyyy-MM-dd HH:mm:ss}\r\n");
                }
                else
                {
                    string errorMsg = GetNTPErrorMessage(result);
                    richTextBox1.AppendText($"服务器 {server} 同步失败: {errorMsg}\r\n");
                }
                
                richTextBox1.AppendText("\r\n");
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"测试服务器异常: {ex.Message}\r\n");
            }
        }

        // 获取NTP错误消息
        private string GetNTPErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 0: return "成功";
                case -1: return "网络错误";
                case -2: return "请求超时";
                case -3: return "无效响应";
                case -4: return "所有服务器失败";
                default: return "未知错误";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!CheckDllLoaded()) return;

            richTextBox1.Clear();
            richTextBox1.AppendText("=== NTP调试测试 ===\r\n");
            
            var result = dllManager.GetNTPTimestamp(out var time);
            
            richTextBox1.AppendText($"返回码: {result} ({GetNTPErrorMessage(result)})\r\n");
            richTextBox1.AppendText($"时间戳: {time}\r\n");
            
            if (time > 0) {
                try {
                    DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time);
                    DateTime localTime = utcTime.ToLocalTime();
                    richTextBox1.AppendText($"UTC时间: {utcTime:yyyy-MM-dd HH:mm:ss}\r\n");
                    richTextBox1.AppendText($"本地时间: {localTime:yyyy-MM-dd HH:mm:ss}\r\n");
                } catch (Exception ex) {
                    richTextBox1.AppendText($"时间转换失败: {ex.Message}\r\n");
                }
            }
            
            // 同时显示本地时间作为对比
            long localTime2 = dllManager.GetLocalTimestamp();
            DateTime localDT = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(localTime2).ToLocalTime();
            richTextBox1.AppendText($"本地系统时间戳: {localTime2} ({localDT:yyyy-MM-dd HH:mm:ss})\r\n");
        }

    }

    public static class StructBaseExtensions
    {
        public static IntPtr GetIntPtr<T>(this T myStruct) where T : struct
        {
            IntPtr pItemPtr = Marshal.AllocHGlobal(Marshal.SizeOf(myStruct));
            Marshal.StructureToPtr(myStruct, pItemPtr, false);
            return pItemPtr;
        }
    }
}
