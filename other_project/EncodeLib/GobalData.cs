using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EncodeLib
{
    /// <summary>
    /// 全局数据和常量定义
    /// </summary>
    public static class GlobalData
    {
        /// <summary>
        /// 加密库DLL文件名
        /// </summary>
        public const string DLL_NAME = "ExportLib.dll";
        
        /// <summary>
        /// 备用DLL文件名
        /// </summary>
        public const string DLL_NAME_BACKUP = "TestExportLib.vmp.dll";
    }

    // 进度回调委托定义
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProgressCallback([MarshalAs(UnmanagedType.LPStr)] string filePath, double progress);
}
