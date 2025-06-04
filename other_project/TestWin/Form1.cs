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
using EncodeLib;

namespace TestWin
{ 

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            // 初始化EncodeLib
            InitializeEncodeLib();
        }

        /// <summary>
        /// 初始化EncodeLib管理器
        /// </summary>
        private void InitializeEncodeLib()
        {
            try
            {
                // 显示加载信息
                this.Text += " - 正在初始化EncodeLib...";
                
                // 尝试获取EncodeLib实例（单例模式）
                var encodeLib = EncodeLibManager.Instance;
                
                // 验证是否成功加载
                if (encodeLib.IsLoaded)
                {
                    this.Text = this.Text.Replace("正在初始化EncodeLib...", "EncodeLib已成功初始化 [无硬盘痕迹]");
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("EncodeLib初始化成功！");
                    #endif
                }
                else
                {
                    throw new InvalidOperationException("EncodeLib初始化失败");
                }
            }
            catch (Exception ex)
            {
                this.Text = "TestWin - EncodeLib初始化失败";
                
                string errorMsg = $"初始化EncodeLib失败: {ex.Message}\n\n";
                errorMsg += "可能的解决方案:\n";
                errorMsg += "1. 确保EncodeLib.dll存在且正确引用\n";
                errorMsg += "2. 确保TestExportLib.vmp.dll已正确嵌入到EncodeLib中\n";
                errorMsg += "3. 确保DLL与当前平台（x86/x64）兼容\n\n";
                errorMsg += $"详细错误:\n{ex}";
                
                MessageBox.Show(errorMsg, "EncodeLib初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 检查EncodeLib是否已加载的辅助方法
        /// </summary>
        private bool CheckEncodeLibLoaded()
        {
            try
            {
                if (!EncodeLibManager.Instance.IsLoaded)
                {
                    MessageBox.Show("EncodeLib未加载或加载失败！请重启应用程序。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"EncodeLib检查失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        //加密
        private void button1_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibLoaded()) return;

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

                // 加密文件 - 使用EncodeLib
                richTextBox1.Clear(); // 清空之前的日志
                richTextBox1.AppendText($"开始加密文件: {System.IO.Path.GetFileName(inputFile)}\r\n");
                
                int result = EncodeLibManager.Instance.EncryptFile(inputFile, encryptedFile, key, OnProgress);
                
                if (result == 0)
                {
                    richTextBox1.AppendText($"加密完成！文件保存在: {encryptedFile}\r\n");
                    MessageBox.Show($"文件加密成功！\n加密文件保存在：{encryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    richTextBox1.AppendText($"加密失败！错误码: {result} - {EncodeLibManager.GetErrorMessage(result)}\r\n");
                    MessageBox.Show($"文件加密失败！错误码: {result}\n错误信息: {EncodeLibManager.GetErrorMessage(result)}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                this.Invoke(new Action<string, double>(OnProgress), filePath, progress);
                return;
            }
            
            // 更新日志显示
            string fileName = System.IO.Path.GetFileName(filePath);
            string progressText = $"处理进度: {fileName} - {percent}%\r\n";
            richTextBox1.AppendText(progressText);
            
            // 滚动到底部
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            
            // 强制刷新UI
            Application.DoEvents();
        }

        static void lala()
        {
            // 这个方法的具体实现暂时保留原样
            // 注释掉错误的静态类实例化代码
            System.Diagnostics.Debug.WriteLine("lala方法被调用");
        }

        //解密
        private void button2_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibLoaded()) return;

            try
            {
                string inputFile = textBox1.Text;
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要解密的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 生成解密文件路径（移除.encrypted后缀，或添加.decrypted后缀）
                string decryptedFile;
                if (inputFile.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase))
                {
                    decryptedFile = inputFile.Substring(0, inputFile.Length - ".encrypted".Length);
                }
                else
                {
                    string directory = System.IO.Path.GetDirectoryName(inputFile);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(inputFile);
                    string extension = System.IO.Path.GetExtension(inputFile);
                    decryptedFile = System.IO.Path.Combine(directory, fileName + extension + ".decrypted");
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

                // 解密文件 - 使用EncodeLib
                richTextBox1.Clear();
                richTextBox1.AppendText($"开始解密文件: {System.IO.Path.GetFileName(inputFile)}\r\n");
                
                int result = EncodeLibManager.Instance.DecryptFile(inputFile, decryptedFile, key, OnProgress);
                
                if (result == 0)
                {
                    richTextBox1.AppendText($"解密完成！文件保存在: {decryptedFile}\r\n");
                    MessageBox.Show($"文件解密成功！\n解密文件保存在：{decryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    richTextBox1.AppendText($"解密失败！错误码: {result} - {EncodeLibManager.GetErrorMessage(result)}\r\n");
                    MessageBox.Show($"文件解密失败！错误码: {result}\n错误信息: {EncodeLibManager.GetErrorMessage(result)}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //验证文件
        private void button3_Click(object sender, EventArgs e)
        {
            //if (!CheckEncodeLibLoaded()) return;

            //try
            //{
            //    string inputFile = textBox1.Text;
            //    string key = textBox2.Text;

            //    if (string.IsNullOrEmpty(inputFile))
            //    {
            //        MessageBox.Show("请选择要验证的文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return;
            //    }

            //    if (string.IsNullOrEmpty(key))
            //    {
            //        MessageBox.Show("请输入密钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return;
            //    }

            //    if (!System.IO.File.Exists(inputFile))
            //    {
            //        MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        return;
            //    }

            //    // 验证文件 - 使用EncodeLib
            //    richTextBox1.Clear();
            //    richTextBox1.AppendText($"开始验证文件: {System.IO.Path.GetFileName(inputFile)}\r\n");
                
            //    int result = EncodeLibManager.Instance.ValidateEncryptedFile(inputFile, key);
                
            //    if (result == 1)
            //    {
            //        richTextBox1.AppendText("文件验证成功！密钥正确。\r\n");
            //        MessageBox.Show("文件验证成功！密钥正确。", "验证成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //    else
            //    {
            //        richTextBox1.AppendText("文件验证失败！密钥错误或文件已损坏。\r\n");
            //        MessageBox.Show("文件验证失败！密钥错误或文件已损坏。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"验证过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        //选择文件
        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "所有文件 (*.*)|*.*|文本文件 (*.txt)|*.txt|加密文件 (*.encrypted)|*.encrypted";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog.FileName;
                richTextBox1.Clear();
                richTextBox1.AppendText($"已选择文件: {openFileDialog.FileName}\r\n");
            }
        }

        //NTP时间同步测试
        private void TestNTPSync()
        {
            if (!CheckEncodeLibLoaded()) return;

            try
            {
                richTextBox1.AppendText("开始获取NTP时间戳...\r\n");
                
                long timestamp;
                int result = EncodeLibManager.Instance.GetNTPTimestamp(out timestamp);
                
                if (result == 0)
                {
                    DateTime ntpTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                    richTextBox1.AppendText($"NTP时间戳获取成功！\r\n");
                    richTextBox1.AppendText($"时间戳: {timestamp}\r\n");
                    richTextBox1.AppendText($"UTC时间: {ntpTime:yyyy-MM-dd HH:mm:ss}\r\n");
                    richTextBox1.AppendText($"本地时间: {ntpTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}\r\n");
                }
                else
                {
                    richTextBox1.AppendText($"NTP时间戳获取失败！错误码: {result}\r\n");
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"NTP时间同步异常: {ex.Message}\r\n");
            }
        }

        private void TestNTPSyncFromServer(string server)
        {
            //if (!CheckEncodeLibLoaded()) return;

            //try
            //{
            //    richTextBox1.AppendText($"开始从服务器 {server} 获取NTP时间戳...\r\n");
                
            //    long timestamp;
            //    int result = EncodeLibManager.Instance.GetNTPTimestampFromServer(server, out timestamp, 5000);
                
            //    if (result == 0)
            //    {
            //        DateTime ntpTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
            //        richTextBox1.AppendText($"从 {server} 获取NTP时间戳成功！\r\n");
            //        richTextBox1.AppendText($"时间戳: {timestamp}\r\n");
            //        richTextBox1.AppendText($"UTC时间: {ntpTime:yyyy-MM-dd HH:mm:ss}\r\n");
            //        richTextBox1.AppendText($"本地时间: {ntpTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}\r\n");
            //    }
            //    else
            //    {
            //        richTextBox1.AppendText($"从 {server} 获取NTP时间戳失败！错误码: {result}\r\n");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    richTextBox1.AppendText($"从 {server} NTP时间同步异常: {ex.Message}\r\n");
            //}
        }

        //NTP同步测试按钮
        private void button5_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("=== NTP时间同步测试 ===\r\n");
            
            // 测试默认NTP服务器
            TestNTPSync();
            
            richTextBox1.AppendText("\r\n=== 测试不同NTP服务器 ===\r\n");
            
            // 测试不同的NTP服务器
            string[] ntpServers = {
                "pool.ntp.org",
                "time.windows.com",
                "time.nist.gov",
                "cn.pool.ntp.org"
            };
            
            foreach (string server in ntpServers)
            {
                TestNTPSyncFromServer(server);
                richTextBox1.AppendText("\r\n");
            }
            
            // 获取本地时间戳作为对比
            try
            {
                //long localTimestamp = EncodeLibManager.Instance.GetLocalTimestamp();
                //DateTime localTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(localTimestamp);
                //richTextBox1.AppendText("=== 本地时间戳 ===\r\n");
                //richTextBox1.AppendText($"本地时间戳: {localTimestamp}\r\n");
                //richTextBox1.AppendText($"UTC时间: {localTime:yyyy-MM-dd HH:mm:ss}\r\n");
                //richTextBox1.AppendText($"本地时间: {localTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}\r\n");
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"获取本地时间戳失败: {ex.Message}\r\n");
            }
        }
    }

    public static class StructBaseExtensions
    {
        public static IntPtr GetIntPtr<T>(this T myStruct) where T : struct
        {
            IntPtr structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(myStruct));
            Marshal.StructureToPtr(myStruct, structPtr, false);
            return structPtr;
        }
    }
}
