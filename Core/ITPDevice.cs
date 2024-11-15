using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{
    using System;
    using System.Runtime.InteropServices;

    public interface ITPDevice
    {
        // Open方法
        bool Open(string driveDllPath);

        // Colse方法，注意这里可能是拼写错误，应该是Close
        bool Close();

        // DisConnect方法
        bool DisConnect(IntPtr tpHandle);

        // Connect方法
        bool Connect(out IntPtr pTPHandle, uint protocolID, uint flags, uint baudRate);

        // ReadVersion方法
        bool ReadVersion(out string firmwareVersion, out string dllVersion, out string apiVersion);

        // DeviceIOCtrl方法
        bool DeviceIOCtrl(uint controlId, IntPtr pInput, IntPtr pOutput);

        // VehicleIdRequest方法
        bool VehicleIdRequest(out IntPtr pTPHandle, uint protocolEnum, out IntPtr handleMod);
    }

}
