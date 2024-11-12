using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{
    public  struct StructWrapBase
    {
        IntPtr GetIntPtr() {
            IntPtr pItemPtr = Marshal.AllocHGlobal(Marshal.SizeOf(this));
             Marshal.StructureToPtr(this, pItemPtr, false);
            return pItemPtr;
        }
    }
}
