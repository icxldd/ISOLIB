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
        public Form1()
        {
            InitializeComponent();
        }
        //加密
        private void button1_Click(object sender, EventArgs e)
        {
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

                // 加密文件
                int result = StreamEncryptFile(inputFile, encryptedFile, key);
                if (result == 0)
                {
                    MessageBox.Show($"文件加密成功！\n加密文件保存在：{encryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"文件加密失败！错误码: {result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加密过程中发生异常: {ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            lala();
        }

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
     
 

        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HelloWord( IntPtr intPtr,ref PDU_RSC_STATUS_ITEM pItem, byte[] p2, UInt32[] p3,PDU_RSC_STATUS_DATA p4, [MarshalAs(UnmanagedType.LPStr)] string PreselectionValue);


        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HelloWord2(UInt32 hh);

        // 加密解密函数声明
        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StreamEncryptFile(
            [MarshalAs(UnmanagedType.LPStr)] string filePath, 
            [MarshalAs(UnmanagedType.LPStr)] string outputPath, 
            [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StreamDecryptFile(
            [MarshalAs(UnmanagedType.LPStr)] string filePath, 
            [MarshalAs(UnmanagedType.LPStr)] string outputPath, 
            [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ValidateEncryptedFile(
            [MarshalAs(UnmanagedType.LPStr)] string filePath, 
            [MarshalAs(UnmanagedType.LPStr)] string key);

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

                // 解密文件
                int result = StreamDecryptFile(inputFile, decryptedFile, key);
                if (result == 0)
                {
                    MessageBox.Show($"文件解密成功！\n解密文件保存在：{decryptedFile}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = GetErrorMessage(result);
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

                // 验证加密文件
                int result = ValidateEncryptedFile(inputFile, key);
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
