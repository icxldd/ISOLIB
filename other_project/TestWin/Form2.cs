using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using EncodeLib;

namespace TestWin
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            
            // 初始化界面
            InitializeForm();
        }

        /// <summary>
        /// 初始化界面设置
        /// </summary>
        private void InitializeForm()
        {
            try
            {
                // 设置窗口标题
                this.Text = "自包含式加密系统测试界面 - EncodeLib";
                
                // 检查EncodeLib是否已加载
                if (EncodeLibManager.Instance.IsLoaded)
                {
                    AppendLog("✓ EncodeLib加载成功！自包含式加密系统就绪。");
                    AppendLog("✓ 此系统无需预设私钥，每次加密都会自动生成2048位高强度私钥。");
                    AppendLog("ℹ 提示：默认公钥已设置，您可以修改或使用默认值。");
                }
                else
                {
                    AppendLog("✗ EncodeLib未加载！请检查DLL文件。");
                    DisableAllButtons();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 初始化失败: {ex.Message}");
                DisableAllButtons();
            }
        }

        /// <summary>
        /// 禁用所有按钮
        /// </summary>
        private void DisableAllButtons()
        {
            btnSelfEncryptFile.Enabled = false;
            btnSelfDecryptFile.Enabled = false;
            btnSelfEncryptData.Enabled = false;
            btnSelfDecryptData.Enabled = false;
            btnGeneratePrivateKey.Enabled = false;
        }

        /// <summary>
        /// 添加日志到显示区域
        /// </summary>
        /// <param name="message">日志消息</param>
        private void AppendLog(string message)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(AppendLog), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            richTextBox1.AppendText($"[{timestamp}] {message}\r\n");
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            Application.DoEvents();
        }

        /// <summary>
        /// 检查EncodeLib是否可用
        /// </summary>
        /// <returns></returns>
        private bool CheckEncodeLibAvailable()
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

        /// <summary>
        /// 进度回调方法
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="progress">进度(0.0-1.0)</param>
        private void OnProgress(string filePath, double progress)
        {
            int percent = (int)(progress * 100);
            string fileName = Path.GetFileName(filePath);
            AppendLog($"📊 处理进度: {fileName} - {percent}%");
        }

        /// <summary>
        /// 选择文件按钮点击事件
        /// </summary>
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "所有文件 (*.*)|*.*|文本文件 (*.txt)|*.txt|自包含式加密文件 (*.selfenc)|*.selfenc|加密文件 (*.encrypted)|*.encrypted";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "选择要处理的文件";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = openFileDialog.FileName;
                    AppendLog($"📁 已选择文件: {openFileDialog.FileName}");
                    
                    // 显示文件信息
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                    AppendLog($"📄 文件大小: {FormatFileSize(fileInfo.Length)}");
                    AppendLog($"📅 修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 选择文件失败: {ex.Message}");
                MessageBox.Show($"选择文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 自包含式文件加密按钮点击事件
        /// </summary>
        private void btnSelfEncryptFile_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibAvailable()) return;

            try
            {
                string inputFile = textBox1.Text.Trim();
                string publicKey = textBox2.Text.Trim();

                // 验证输入
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要加密的文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(inputFile))
                {
                    MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 生成输出文件路径
                string directory = Path.GetDirectoryName(inputFile);
                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string extension = Path.GetExtension(inputFile);
                string outputFile = Path.Combine(directory, fileName + extension + ".selfenc");

                AppendLog("🔐 ===== 开始自包含式文件加密 =====");
                AppendLog($"📁 输入文件: {Path.GetFileName(inputFile)}");
                AppendLog($"🔑 公钥: {publicKey}");
                AppendLog($"📁 输出文件: {Path.GetFileName(outputFile)}");
                AppendLog("⚡ 正在自动生成2048位私钥...");

                // 调用自包含式加密
                int result = EncodeLibManager.Instance.SelfContainedEncryptFile(inputFile, outputFile, publicKey, OnProgress);

                if (result == 0)
                {
                    FileInfo outputInfo = new FileInfo(outputFile);
                    AppendLog($"✅ 加密成功！");
                    AppendLog($"📁 输出文件: {outputFile}");
                    AppendLog($"📄 输出大小: {FormatFileSize(outputInfo.Length)}");
                    AppendLog($"🔒 私钥已自动生成并嵌入到加密文件中");
                    
                    MessageBox.Show($"自包含式文件加密成功！\n\n输出文件: {outputFile}\n文件大小: {FormatFileSize(outputInfo.Length)}\n\n私钥已自动生成并嵌入，无需额外保存。", 
                        "加密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = EncodeLibManager.GetErrorMessage(result);
                    AppendLog($"✗ 加密失败！错误码: {result} - {errorMsg}");
                    MessageBox.Show($"自包含式文件加密失败！\n错误码: {result}\n错误信息: {errorMsg}", 
                        "加密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 加密异常: {ex.Message}");
                MessageBox.Show($"加密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 自包含式文件解密按钮点击事件
        /// </summary>
        private void btnSelfDecryptFile_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibAvailable()) return;

            try
            {
                string inputFile = textBox1.Text.Trim();
                string publicKey = textBox2.Text.Trim();

                // 验证输入
                if (string.IsNullOrEmpty(inputFile))
                {
                    MessageBox.Show("请选择要解密的文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(inputFile))
                {
                    MessageBox.Show("输入文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 生成输出文件路径
                string outputFile;
                if (inputFile.EndsWith(".selfenc", StringComparison.OrdinalIgnoreCase))
                {
                    outputFile = inputFile.Substring(0, inputFile.Length - ".selfenc".Length);
                }
                else
                {
                    string directory = Path.GetDirectoryName(inputFile);
                    string fileName = Path.GetFileNameWithoutExtension(inputFile);
                    string extension = Path.GetExtension(inputFile);
                    outputFile = Path.Combine(directory, fileName + extension + ".decrypted");
                }

                AppendLog("🔓 ===== 开始自包含式文件解密 =====");
                AppendLog($"📁 输入文件: {Path.GetFileName(inputFile)}");
                AppendLog($"🔑 公钥: {publicKey}");
                AppendLog($"📁 输出文件: {Path.GetFileName(outputFile)}");
                AppendLog("⚡ 正在从文件中读取并验证私钥...");

                // 调用自包含式解密
                int result = EncodeLibManager.Instance.SelfContainedDecryptFile(inputFile, outputFile, publicKey, OnProgress);

                if (result == 0)
                {
                    FileInfo outputInfo = new FileInfo(outputFile);
                    AppendLog($"✅ 解密成功！");
                    AppendLog($"📁 输出文件: {outputFile}");
                    AppendLog($"📄 输出大小: {FormatFileSize(outputInfo.Length)}");
                    AppendLog($"🔓 私钥验证通过，文件完整性确认");
                    
                    MessageBox.Show($"自包含式文件解密成功！\n\n输出文件: {outputFile}\n文件大小: {FormatFileSize(outputInfo.Length)}", 
                        "解密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = EncodeLibManager.GetErrorMessage(result);
                    AppendLog($"✗ 解密失败！错误码: {result} - {errorMsg}");
                    
                    string detailMsg = "";
                    if (result == -4) // 解密失败
                    {
                        detailMsg = "\n\n可能原因:\n• 公钥不正确\n• 文件已损坏\n• 私钥被篡改\n• 文件格式不正确";
                    }
                    
                    MessageBox.Show($"自包含式文件解密失败！\n错误码: {result}\n错误信息: {errorMsg}{detailMsg}", 
                        "解密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 解密异常: {ex.Message}");
                MessageBox.Show($"解密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 自包含式数据加密按钮点击事件
        /// </summary>
        private void btnSelfEncryptData_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibAvailable()) return;

            try
            {
                string inputData = textBox1.Text.Trim();
                string publicKey = textBox2.Text.Trim();

                // 验证输入
                if (string.IsNullOrEmpty(inputData))
                {
                    MessageBox.Show("请在文本框中输入要加密的数据！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AppendLog("🔐 ===== 开始自包含式数据加密 =====");
                AppendLog($"📝 原始数据长度: {inputData.Length} 字符");
                AppendLog($"🔑 公钥: {publicKey}");
                AppendLog("⚡ 正在自动生成2048位私钥...");

                // 将字符串转换为字节数组
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
                AppendLog($"🔄 转换为字节数组: {inputBytes.Length} 字节");

                // 调用自包含式数据加密
                byte[] encryptedData = EncodeLibManager.Instance.SelfContainedEncryptData(inputBytes, publicKey);

                // 转换为Base64以便显示
                string encryptedBase64 = Convert.ToBase64String(encryptedData);

                AppendLog($"✅ 加密成功！");
                AppendLog($"📊 加密数据长度: {encryptedData.Length} 字节");
                AppendLog($"📊 Base64长度: {encryptedBase64.Length} 字符");
                AppendLog($"🔒 私钥已自动生成并嵌入到加密数据中");
                AppendLog("");
                AppendLog("📋 ===== 加密结果 (Base64编码) =====");
                AppendLog(encryptedBase64);
                AppendLog("=======================================");

                // 将加密结果设置到输入框，方便进行解密测试
                textBox1.Text = encryptedBase64;

                MessageBox.Show($"自包含式数据加密成功！\n\n原始长度: {inputData.Length} 字符\n加密长度: {encryptedData.Length} 字节\nBase64长度: {encryptedBase64.Length} 字符\n\n加密结果已显示在日志中并自动填入输入框\n私钥已自动生成并嵌入，无需额外保存", 
                    "加密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 加密异常: {ex.Message}");
                MessageBox.Show($"数据加密失败: {ex.Message}", "加密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 自包含式数据解密按钮点击事件
        /// </summary>
        private void btnSelfDecryptData_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibAvailable()) return;

            try
            {
                string encryptedBase64 = textBox1.Text.Trim();
                string publicKey = textBox2.Text.Trim();

                // 验证输入
                if (string.IsNullOrEmpty(encryptedBase64))
                {
                    MessageBox.Show("请在文本框中输入要解密的数据（Base64格式）！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                AppendLog("🔓 ===== 开始自包含式数据解密 =====");
                AppendLog($"📝 输入数据长度: {encryptedBase64.Length} 字符");
                AppendLog($"🔑 公钥: {publicKey}");
                AppendLog("⚡ 正在验证Base64格式...");

                // 验证并转换Base64数据
                byte[] encryptedData;
                try
                {
                    encryptedData = Convert.FromBase64String(encryptedBase64);
                    AppendLog($"✅ Base64解码成功: {encryptedData.Length} 字节");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("输入的数据不是有效的Base64格式！请确保输入的是自包含式加密后的Base64字符串。");
                }

                AppendLog("⚡ 正在从数据中读取并验证私钥...");

                // 调用自包含式数据解密
                byte[] decryptedData = EncodeLibManager.Instance.SelfContainedDecryptData(encryptedData, publicKey);

                // 转换为字符串
                string decryptedText = Encoding.UTF8.GetString(decryptedData);

                AppendLog($"✅ 解密成功！");
                AppendLog($"📊 解密数据长度: {decryptedData.Length} 字节");
                AppendLog($"📊 解密文本长度: {decryptedText.Length} 字符");
                AppendLog($"🔓 私钥验证通过，数据完整性确认");
                AppendLog("");
                AppendLog("📋 ===== 解密结果 =====");
                AppendLog(decryptedText);
                AppendLog("========================");

                // 将解密结果设置到输入框
                textBox1.Text = decryptedText;

                MessageBox.Show($"自包含式数据解密成功！\n\n加密长度: {encryptedData.Length} 字节\n解密长度: {decryptedData.Length} 字节\n解密文本: {decryptedText.Length} 字符\n\n解密结果已显示在日志中并自动填入输入框", 
                    "解密成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 解密异常: {ex.Message}");
                MessageBox.Show($"数据解密失败: {ex.Message}", "解密失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 生成2048位私钥按钮点击事件
        /// </summary>
        private void btnGeneratePrivateKey_Click(object sender, EventArgs e)
        {
            if (!CheckEncodeLibAvailable()) return;

            try
            {
                AppendLog("🔑 ===== 生成2048位随机私钥 =====");
                AppendLog("⚡ 正在使用Windows CryptoAPI生成高质量随机数...");

                // 生成私钥
                byte[] privateKey = EncodeLibManager.Instance.Generate2048BitPrivateKey();

                // 转换为Base64以便显示
                string privateKeyBase64 = Convert.ToBase64String(privateKey);

                AppendLog(privateKeyBase64);
            }
            catch (Exception ex)
            {
                AppendLog($"✗ 私钥生成异常: {ex.Message}");
                MessageBox.Show($"私钥生成失败: {ex.Message}", "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 清空日志按钮点击事件
        /// </summary>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            AppendLog("📝 日志已清空");
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的大小字符串</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string dd = EncodeLibManager.Instance.GetMachineFingerprint();

            AppendLog(dd);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string inputFile = textBox1.Text.Trim();
            string publicKey = textBox2.Text.Trim();
            var ddd = EncodeLibManager.Instance.ExtractPrivateKeyFromFile(inputFile,publicKey);

            AppendLog(ddd);



        }

        private void button3_Click(object sender, EventArgs e)
        {
            string encryptedBase64 = textBox1.Text.Trim();
            string publicKey = textBox2.Text.Trim();
            // 验证并转换Base64数据
            byte[] encryptedData;
            try
            {
                encryptedData = Convert.FromBase64String(encryptedBase64);
                AppendLog($"✅ Base64解码成功: {encryptedData.Length} 字节");
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("输入的数据不是有效的Base64格式！请确保输入的是自包含式加密后的Base64字符串。");
            }
            var ddd = EncodeLibManager.Instance.ExtractPrivateKeyFromData(encryptedData, publicKey);

            AppendLog(ddd);
        }
    }
}
