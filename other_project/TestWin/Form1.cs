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
                    EncodeLibManager.Instance.InitializePrivateKey("MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQCOqkBwrg6Fq60Wt+wzgJDZCWJnFJYgVXKPhHzyGW0LdHQS3KBgfWSQaslovYoHO60znx4w/+kToGnHP4GPstXrsOKhz/i3mByA/FNkEWPheSbBVpS2pQFl6FBijYJaJYgzXRzziEO2Tj54aLMGf9jW3mhHQm4BfK03tppi/hoV4LPJC1DmWQ9G8xZky+ZpsL8YEc1YcpR3O57KH/RAGwDgTXpVgJ2dCkA0BRknvcMfViNZcYh1bEKy+AURdS8hJ3JdUiK2dVm+q090xOSon7sjsfkCL3ZEaKHzPPRMqx1LTKQGa/SDjhZv0wFibprw1CkLmLH3i7Wdu9A0N8WqbrRjAgMBAAECggEAZhYN5pOmcKBYS1lw+6mT/LpqX7irdJewUmJLxjHLhdbe+GBHosQXof/H/9shWeuqFLZXtFhrQFAZYSpgW6Ns0CrTAVcAct+2BdaJFaIcBsvan56E6+1HAtUqMFtyW29f9uE6RknLqjhzG1ZQROZXE+oyVEuEzCubB7Ly5sNNhzkhycOhE3VgCcVD+laDrw9wWTnuJ+ur5ffaHP4qFZ7zPOmIe4ZjFI/iWebFomEJnepFqQmCAQH74i40xpdH5TFGNfGlYB5yBkLkNsYJFRaW2qWSEzAviHyZVeQnTEFKNif2g0p0td5kBxSt09LoO1mX4hkHtlbCxxrSNKjJDudOAQKBgQDFMFHguUYYHGE8BdgICBD1dqWEdg4iwx4jDwAxJG0SikWOutVf41aYp6YLBJcJ+K0PtwQMr9hdXYfeP7efO0YM1BNzColisAhHpdp1jMTUttIczhhyjo5rjWl5qvjnLhJiQB0R2PQ7r7SvGGNYZVfgLqv7yXjO6Bb/+PmjSY/mUQKBgQC5NvV4b1Tjx+5ysqgRAmd0gw8jOtD9WtS6ERxnjYY6sht0ndHEQo0C8TOmEBMBeeBYRHO2O9E6HstMhmK8Bmi2/Ksae0BTcIVG52co0zArEcvKE2ldA5uqX+aI9BmtNVJ4CkFNvwWrCCDT29BprwYXzNlEo3hX1FBj17MvcBDecwKBgAtVjq9DFwNVxkUD9PnpNMhXLIZjnsZivr23JASvGlHhfsQIezFKyPR3VnT1q5TYJWJs25+7D822DZQ5x10wtAMSwZdwOJtikOdFYjw1fi7X31XmhsM27HrEIxbqO+pV3JqnIsSe2tL/c3xJA5TWJmntZNdRKk+CSagm8HpxRQMxAoGASrMfzbMZScUZJqlnn3SYxSUWtd7C62v24BSGoD00JfgvmpkMQVuWA9nEOvXAtJezI+Z3xMfbWtWQqQyKRctP8H13hPawuvZmynIJ6S1EABrtVlL968XIwq5rDFFnCbS3zjJUpEamwpREqS2+oOE2U+MKveQwZTv8MEiOvFM2eoECgYA+ZpQ2eAQf4CTHOXYjJtp7kcyG1lfJ5l5bEwwtSvexGBMCtGhnDaBcnBid2jv8vjBoB4WEcgehg1K4atmqjqhJtOn/iy7WGwhoPXmZ0SNKVjDP0P1Q9VrKXgNNcxxbcN8VkbN23TCZDxVn813+7JO8UjiY05Evinwd85E7y7009Q==");
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

        /// <summary>
        /// 将文件转换为Base64并保存到当前目录
        /// </summary>
        /// <returns>转换是否成功</returns>
        public bool ConvertFileToBase64AndSave()
        {
            try
            {
                // 获取文件路径
                string filePath = textBox1.Text;
                
                // 验证文件路径
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    richTextBox1.AppendText("错误：请先选择文件路径！\r\n");
                    MessageBox.Show("请先选择文件路径！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                
                if (!System.IO.File.Exists(filePath))
                {
                    richTextBox1.AppendText($"错误：文件不存在：{filePath}\r\n");
                    MessageBox.Show($"文件不存在：{filePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                richTextBox1.AppendText($"开始转换文件：{System.IO.Path.GetFileName(filePath)}\r\n");
                
                // 读取文件数据
                byte[] fileData = System.IO.File.ReadAllBytes(filePath);
                richTextBox1.AppendText($"文件大小：{fileData.Length:N0} 字节\r\n");
                
                // 转换为Base64
                string base64String = Convert.ToBase64String(fileData);
                richTextBox1.AppendText($"Base64长度：{base64String.Length:N0} 字符\r\n");
                
                // 生成输出文件名
                string originalFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string outputFileName = $"{originalFileName}_base64.txt";
                string outputPath = System.IO.Path.Combine(Application.StartupPath, outputFileName);
                
                // 写入Base64数据到文件
                System.IO.File.WriteAllText(outputPath, base64String, Encoding.UTF8);
                
                richTextBox1.AppendText($"Base64数据已保存到：{outputPath}\r\n");
                richTextBox1.AppendText("转换完成！\r\n");
                
                MessageBox.Show($"文件转换成功！\n\n原文件：{filePath}\n输出文件：{outputPath}\n\nBase64长度：{base64String.Length:N0} 字符", 
                    "转换成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"文件转换失败：{ex.Message}";
                richTextBox1.AppendText($"错误：{errorMsg}\r\n");
                MessageBox.Show(errorMsg, "转换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 将Base64文件还原为原始文件
        /// </summary>
        /// <param name="base64FilePath">Base64文件路径</param>
        /// <returns>还原是否成功</returns>
        public bool ConvertBase64ToFile(string base64FilePath = null)
        {
            try
            {
                // 如果没有指定Base64文件路径，使用文件选择对话框
                if (string.IsNullOrWhiteSpace(base64FilePath))
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "Base64文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                    openFileDialog.Title = "选择Base64文件";
                    openFileDialog.InitialDirectory = Application.StartupPath;
                    
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }
                    
                    base64FilePath = openFileDialog.FileName;
                }
                
                if (!System.IO.File.Exists(base64FilePath))
                {
                    richTextBox1.AppendText($"错误：Base64文件不存在：{base64FilePath}\r\n");
                    return false;
                }
                
                richTextBox1.AppendText($"开始还原Base64文件：{System.IO.Path.GetFileName(base64FilePath)}\r\n");
                
                // 读取Base64数据
                string base64String = System.IO.File.ReadAllText(base64FilePath, Encoding.UTF8);
                richTextBox1.AppendText($"Base64长度：{base64String.Length:N0} 字符\r\n");
                
                // 从Base64转换为字节数组
                byte[] fileData = Convert.FromBase64String(base64String);
                richTextBox1.AppendText($"还原文件大小：{fileData.Length:N0} 字节\r\n");
                
                // 生成输出文件名
                string base64FileName = System.IO.Path.GetFileNameWithoutExtension(base64FilePath);
                string outputFileName = base64FileName.Replace("_base64", "_restored");
                string outputPath = System.IO.Path.Combine(Application.StartupPath, outputFileName);
                
                // 如果输出文件已存在，添加时间戳
                if (System.IO.File.Exists(outputPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    outputPath = System.IO.Path.Combine(Application.StartupPath, $"{outputFileName}_{timestamp}");
                }
                
                // 写入还原的文件数据
                System.IO.File.WriteAllBytes(outputPath, fileData);
                
                richTextBox1.AppendText($"文件已还原到：{outputPath}\r\n");
                richTextBox1.AppendText("还原完成！\r\n");
                
                MessageBox.Show($"Base64文件还原成功！\n\nBase64文件：{base64FilePath}\n还原文件：{outputPath}\n\n文件大小：{fileData.Length:N0} 字节", 
                    "还原成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Base64文件还原失败：{ex.Message}";
                richTextBox1.AppendText($"错误：{errorMsg}\r\n");
                MessageBox.Show(errorMsg, "还原失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 批量转换目录中的所有文件为Base64
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="filePattern">文件模式（如 *.dll, *.exe）</param>
        /// <returns>转换是否成功</returns>
        public bool BatchConvertToBase64(string directoryPath = null, string filePattern = "*.*")
        {
            try
            {
                // 如果没有指定目录，使用文件夹选择对话框
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                    folderDialog.Description = "选择要批量转换的目录";
                    folderDialog.SelectedPath = Application.StartupPath;
                    
                    if (folderDialog.ShowDialog() != DialogResult.OK)
                    {
                        return false;
                    }
                    
                    directoryPath = folderDialog.SelectedPath;
                }
                
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    richTextBox1.AppendText($"错误：目录不存在：{directoryPath}\r\n");
                    return false;
                }
                
                richTextBox1.AppendText($"开始批量转换目录：{directoryPath}\r\n");
                richTextBox1.AppendText($"文件模式：{filePattern}\r\n");
                
                // 获取目录中的所有文件
                string[] files = System.IO.Directory.GetFiles(directoryPath, filePattern);
                
                if (files.Length == 0)
                {
                    richTextBox1.AppendText("目录中没有找到匹配的文件！\r\n");
                    return false;
                }
                
                richTextBox1.AppendText($"找到 {files.Length} 个文件\r\n");
                
                int successCount = 0;
                int failCount = 0;
                
                // 逐个转换文件
                foreach (string file in files)
                {
                    try
                    {
                        // 临时设置textBox1的值
                        string originalText = textBox1.Text;
                        textBox1.Text = file;
                        
                        richTextBox1.AppendText($"\n正在转换：{System.IO.Path.GetFileName(file)}\r\n");
                        
                        if (ConvertFileToBase64AndSave())
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                        
                        // 恢复textBox1的值
                        textBox1.Text = originalText;
                    }
                    catch (Exception ex)
                    {
                        richTextBox1.AppendText($"转换文件 {System.IO.Path.GetFileName(file)} 失败：{ex.Message}\r\n");
                        failCount++;
                    }
                }
                
                richTextBox1.AppendText($"\n批量转换完成！\r\n");
                richTextBox1.AppendText($"成功：{successCount} 个文件\r\n");
                richTextBox1.AppendText($"失败：{failCount} 个文件\r\n");
                
                MessageBox.Show($"批量转换完成！\n\n成功：{successCount} 个文件\n失败：{failCount} 个文件", 
                    "批量转换完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                return successCount > 0;
            }
            catch (Exception ex)
            {
                string errorMsg = $"批量转换失败：{ex.Message}";
                richTextBox1.AppendText($"错误：{errorMsg}\r\n");
                MessageBox.Show(errorMsg, "批量转换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 快捷调用：转换textBox1中的文件为Base64（可绑定到按钮点击事件）
        /// </summary>
        private void ConvertToBase64_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("=== 文件转Base64 ===\r\n");
            ConvertFileToBase64AndSave();
        }

        /// <summary>
        /// 快捷调用：Base64文件还原（可绑定到按钮点击事件）
        /// </summary>
        private void ConvertFromBase64_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("=== Base64文件还原 ===\r\n");
            ConvertBase64ToFile();
        }

        /// <summary>
        /// 快捷调用：批量转换目录中的DLL文件为Base64（可绑定到按钮点击事件）
        /// </summary>
        private void BatchConvertDLLs_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("=== 批量转换DLL文件 ===\r\n");
            BatchConvertToBase64(null, "*.dll");
        }

        /// <summary>
        /// 专用于DLL文件的转换方法
        /// </summary>
        /// <param name="dllFilePath">DLL文件路径，为空时使用textBox1.Text</param>
        /// <returns>转换是否成功</returns>
        public bool ConvertDllToBase64(string dllFilePath = null)
        {
            try
            {
                // 如果没有指定路径，使用textBox1的值
                if (string.IsNullOrWhiteSpace(dllFilePath))
                {
                    dllFilePath = textBox1.Text;
                }

                // 验证是否为DLL文件
                if (!dllFilePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    richTextBox1.AppendText("警告：选择的文件不是DLL文件\r\n");
                }

                // 临时设置textBox1的值
                string originalText = textBox1.Text;
                textBox1.Text = dllFilePath;

                // 调用转换方法
                bool result = ConvertFileToBase64AndSave();

                // 恢复textBox1的值
                textBox1.Text = originalText;

                if (result)
                {
                    richTextBox1.AppendText("提示：可以将生成的Base64数据用于无痕加载方案\r\n");
                }

                return result;
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"DLL转换失败：{ex.Message}\r\n");
                return false;
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var b= ConvertDllToBase64(textBox1.Text);
            MessageBox.Show("转换是否成功:"+b);
        }


        //数据加密
        private void button7_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibLoaded()) return;

            try
            {
                string inputData = textBox1.Text;
                string publicKey = textBox2.Text;

                // 验证输入
                if (string.IsNullOrEmpty(inputData))
                {
                    MessageBox.Show("请在文本框中输入要加密的数据！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 清空日志
                richTextBox1.Clear();
                richTextBox1.AppendText("=== 数据加密 ===\r\n");
                richTextBox1.AppendText($"原始数据长度: {inputData.Length} 字符\r\n");
                richTextBox1.AppendText($"公钥: {publicKey}\r\n");
                richTextBox1.AppendText("开始加密...\r\n");

                // 将字符串转换为字节数组
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
                richTextBox1.AppendText($"转换为字节数组: {inputBytes.Length} 字节\r\n");

                // 调用加密方法
                byte[] encryptedData = EncodeLibManager.Instance.EncryptData(inputBytes, publicKey);

                // 将加密后的数据转换为Base64字符串便于显示和存储
                string encryptedBase64 = Convert.ToBase64String(encryptedData);

                richTextBox1.AppendText($"加密成功！\r\n");
                richTextBox1.AppendText($"加密后数据长度: {encryptedData.Length} 字节\r\n");
                richTextBox1.AppendText($"Base64编码长度: {encryptedBase64.Length} 字符\r\n");
                richTextBox1.AppendText("\r\n=== 加密结果 (Base64编码) ===\r\n");
                richTextBox1.AppendText(encryptedBase64);
                richTextBox1.AppendText("\r\n\r\n提示: 可以复制上面的Base64字符串用于解密测试");

                // 将加密结果也设置到textBox1中，方便直接进行解密测试
                textBox1.Text = encryptedBase64;

                MessageBox.Show($"数据加密成功！\n\n原始数据长度: {inputData.Length} 字符\n加密后长度: {encryptedData.Length} 字节\nBase64长度: {encryptedBase64.Length} 字符\n\n加密结果已显示在日志中并自动填入输入框",
                    "加密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"\r\n加密失败: {ex.Message}\r\n");
                MessageBox.Show($"数据加密失败: {ex.Message}", "加密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //数据解密
        private void button6_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibLoaded()) return;

            try
            {
                string encryptedBase64 = textBox1.Text;
                string publicKey = textBox2.Text;

                // 验证输入
                if (string.IsNullOrEmpty(encryptedBase64))
                {
                    MessageBox.Show("请在文本框中输入要解密的数据（Base64格式）！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 清空日志
                richTextBox1.Clear();
                richTextBox1.AppendText("=== 数据解密 ===\r\n");
                richTextBox1.AppendText($"输入数据长度: {encryptedBase64.Length} 字符\r\n");
                richTextBox1.AppendText($"公钥: {publicKey}\r\n");
                richTextBox1.AppendText("开始解密...\r\n");

                // 验证并转换Base64数据
                byte[] encryptedData;
                try
                {
                    encryptedData = Convert.FromBase64String(encryptedBase64);
                    richTextBox1.AppendText($"Base64解码成功: {encryptedData.Length} 字节\r\n");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("输入的数据不是有效的Base64格式！请确保输入的是加密后的Base64字符串。");
                }

                // 调用解密方法
                byte[] decryptedData = EncodeLibManager.Instance.DecryptData(encryptedData, publicKey);

                // 将解密后的字节数组转换为字符串
                string decryptedText = Encoding.UTF8.GetString(decryptedData);

                richTextBox1.AppendText($"解密成功！\r\n");
                richTextBox1.AppendText($"解密后数据长度: {decryptedData.Length} 字节\r\n");
                richTextBox1.AppendText($"解密后文本长度: {decryptedText.Length} 字符\r\n");
                richTextBox1.AppendText("\r\n=== 解密结果 ===\r\n");
                richTextBox1.AppendText(decryptedText);

                // 将解密结果设置到textBox1中
                textBox1.Text = decryptedText;

                MessageBox.Show($"数据解密成功！\n\n加密数据长度: {encryptedData.Length} 字节\n解密后长度: {decryptedData.Length} 字节\n解密后文本: {decryptedText.Length} 字符\n\n解密结果已显示在日志中并自动填入输入框",
                    "解密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"\r\n解密失败: {ex.Message}\r\n");
                MessageBox.Show($"数据解密失败: {ex.Message}", "解密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
