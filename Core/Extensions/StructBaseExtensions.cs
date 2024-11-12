using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core.Extensions
{
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
