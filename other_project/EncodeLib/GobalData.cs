using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EncodeLib
{
    // 进度回调委托定义
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProgressCallback([MarshalAs(UnmanagedType.LPStr)] string filePath, double progress);
}
