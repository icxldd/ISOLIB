using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core.Extensions
{
    public static class PduStatusExtensions
    {
        public static uint GetValue(this PduStatus status)
        {
            return (uint)status;
        }
    }
}
