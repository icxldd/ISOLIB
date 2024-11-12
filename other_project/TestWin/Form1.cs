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

        private void button1_Click(object sender, EventArgs e)
        {
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
        public class StructWrapBase
        {
            IntPtr GetIntPtr()
            {
                IntPtr pItemPtr = Marshal.AllocHGlobal(Marshal.SizeOf(this));
                Marshal.StructureToPtr(this, pItemPtr, false);
                return pItemPtr;
            }
        }

        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HelloWord(IntPtr pItem, byte[] p2, UInt32[] p3,PDU_RSC_STATUS_DATA p4, [MarshalAs(UnmanagedType.LPStr)] string PreselectionValue);


        [DllImport("TestExportLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HelloWord2(UInt32 hh);

        static void lala()
        {
            // 创建 PDU_RSC_STATUS_ITEM 结构体实例
            PDU_RSC_STATUS_DATA data;
            data.hMod = 1;
            data.ResourceId = 2;
           
            data.ResourceStatus = 4;

            PDU_RSC_STATUS_ITEM item = new PDU_RSC_STATUS_ITEM();
            item.ItemType = T_PDU_IT.PDU_IT_RSC_STATUS;
            item.NumEntries = 2;
            item.name = "hhhhh";
            item.pResourceStatusData = data;

            IntPtr pItemPtr = Marshal.AllocHGlobal(Marshal.SizeOf(item));
            Marshal.StructureToPtr(item, pItemPtr, false);

            //// 创建 PDU_RSC_STATUS_DATA 数组并初始化
            //item.pResourceStatusData = new PDU_RSC_STATUS_DATA[item.NumEntries];
            //for (int i = 0; i < item.NumEntries; ++i)
            //{
            //    item.pResourceStatusData[i].hMod = (uint)i;
            //    item.pResourceStatusData[i].ResourceId = (uint)i;
            //    item.pResourceStatusData[i].ResourceStatus = (uint)i;
            //}

            // 调用 C++ 函数
            HelloWord(pItemPtr, new byte[] { 1,2,3,0x12,11 },new UInt32[] { 1,23,4,5,2,1,1}, data,"啊啊啊");
            HelloWord2(13);

        }
    }
}
