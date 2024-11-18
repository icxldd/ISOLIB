using ISOLib.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{

public class PDUDevice : ITPDevice
    {
        private int maxArrayLen = 65536;
        private PduApiManager pduApiManager;
        // 类的构造函数和析构函数
        public PDUDevice()
        {
            // 初始化代码（如果有的话）
            pduApiManager = PduApiManager.getInstance();
            this.m_DeviceID =DoipConstants. INVALID_ID;
            this.DO_loopback = 0;
            this.DO_testaddress = 0;
            this.DO_eculogiacladdress = 0;
            this.DO_ecufunctionaddress = 0;
            this.DO_ecuipaddress = 0;
            this.DO_datapins = 0;
            this.open = false;
        }

        ~PDUDevice()
        {
            // 析构代码（如果有的话）
            if (m_DeviceID != DoipConstants. INVALID_ID)
            {
                Close();
            }
        }

        // 实现ITPDevice接口的方法
        public bool Open(string driveDllPath)
        {
            bool bReturnValue = false;
            string strDLLPath = driveDllPath;
            if (pduApiManager.openDevice(strDLLPath) == PduStatus.PDU_STATUS_NOERROR.GetValue())
            {
                uint openResult = PduStatus.PDU_STATUS_NOERROR.GetValue();
                try
                {
                    openResult =pduApiManager.m_PConstructMethod(null, IntPtr.Zero);
                    if (openResult == PduStatus.PDU_ERR_SHARING_VIOLATION.GetValue())
                    {
                        pduApiManager.m_PDestructMethod();
                        pduApiManager.m_PConstructMethod(null, IntPtr.Zero);
                    }
                    pduApiManager.m_PRegisterEventCallbackMethod(0xFFFFFFFF, 0xFFFFFFFF, null);
                    PduModuleItem[] ModuleIds=new PduModuleItem [5];
                    var ptr = DefinePtrToStructure.ToIntPtrByArr<PduModuleItem>(ModuleIds).GetIntPtr();
                    openResult = pduApiManager.m_PGetModuleIdsMethod(ptr);
                    if ( ModuleIds[0].NumEntrie > 0)
                    {
                       var item =  DefinePtrToStructure.PtrToStructureT<PduModuleData>(ModuleIds[0].pModuleData);

                        this.HMod = item.hMod;
                    }

                    IntPtr itemPtr = ModuleIds[0].ToIntPtr();
                    pduApiManager.m_PDestroyItemMethod(itemPtr);

                    openResult = pduApiManager.m_PModuleConnectMethod(this.HMod);
                    if (openResult != PduStatus.PDU_STATUS_NOERROR.GetValue())
                    {
                        bReturnValue = false;
                    }
                    if (openResult == PduStatus.PDU_STATUS_NOERROR.GetValue())
                    {
                        bReturnValue = true;
                    }
                    pduApiManager.m_PRegisterEventCallbackMethod(HMod, 0xFFFFFFFF, null);

                    DO_PIN();

                }
                catch (Exception e)
                {
                    bReturnValue = false;
                }
            }
            if (bReturnValue == false)
            {
                pduApiManager.Unload();
            }
            return bReturnValue;
        }

        public bool Close()
        {
            bool bReturnValue = false;
            uint CloseResult =pduApiManager.m_PModuleDisconnectMethod(this.HMod);
            CloseResult =pduApiManager.m_PDestructMethod();
            if (CloseResult ==PduStatus.PDU_STATUS_NOERROR.GetValue()|| CloseResult == PduStatus.PDU_ERR_COMM_PC_TO_VCI_FAILED.GetValue() || CloseResult == PduStatus.PDU_ERR_INVALID_HANDLE.GetValue())
            {
                if (pduApiManager.Unload() == PduStatus.PDU_STATUS_NOERROR.GetValue())
                {
                    bReturnValue = true;
                }
            }
            this.m_DeviceID = DoipConstants.INVALID_ID;
            this.open = false;
            return bReturnValue;
        }

        public bool Connect(out IntPtr pTPHandle, uint protocolEnum, uint flags, uint baudRate)
        {
            // 实现代码
            throw new NotImplementedException();
        }
        
        public bool DisConnect(IntPtr tpHandle)
        {
            return false;
        }

        public bool ReadVersion(out string firmwareVersion, out string dllVersion, out string apiVersion)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        public bool DeviceIOCtrl(uint controlId, IntPtr pInput, IntPtr pOutput)
        {
            //uint Status =PduStatus. PDU_ERR_FCT_FAILED.GetValue();
            //switch (controlId)
            //{
            //    case READ_VBATT:
            //        Status = this.IOCtrlReadVBATT(out uint output);
            //        Marshal.WriteIntPtr(pOutput, Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint))));
            //        Marshal.StructureToPtr(output, pOutput, false);
            //        break;
            //    case Pre_IOCtl:
            //        Status = this.PreIOCtrlIOCtrl(Marshal.PtrToStructure<SCONFIG_LIST>(pInput));
            //        break;
            //    default:
            //        break;
            //}
            //if (Status != PDU_STATUS_NOERROR)
            //{
            //    string TmpChar = new string('\0', 128);
            //    // 这里原本的代码是 char TmpChar[128]，但在C#中我们通常不会这样使用字符串。
            //    // 你可能需要根据实际情况来处理这个字符串。
            //    return false;
            //}
            return true;
        }

        public bool VehicleIdRequest(out IntPtr pTPHandle, uint protocolEnum, out IntPtr handleMod)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        // 额外的方法实现
        public uint PreIOCtrlIOCtrl(IntPtr pInput)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        public uint DO_PIN()
        {
            uint RetStatus;
            string pShortname = "PDU_IOCTL_GET_ETH_PIN_OPTION";
            T_Pdu_Objt pduObjectType = T_Pdu_Objt.PDU_OBJT_IO_CTRL;
            uint pPduObjectId = 0;
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            pShortname = "PDU_IOCTL_SET_ETH_SWITCH_STATE";
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            pShortname = "PDU_IOCTL_MS_SET_BRIDGE_SWITCH_STATE";
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            pShortname = "PDU_IOCTL_VEHICLE_ID_REQUEST";
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);

            PduDataItem pInputData2 = new PduDataItem();
            pInputData2.ItemType =T_PDU_IT.PDU_IT_IO_UNUM32;
            uint[] pins = new uint[1];
            pins[0] = 1;
            pInputData2.pData = pins.ToIntPtrByArr(); // PDU_IOCTL_GET_ETH_PIN_OPTION
            //PduDataItem pOutputData = new PduDataItem() ;
            //var pOutputDataPtr = pOutputData.GetIntPtr();   //会报错  申请的内存小了

            PduDataItem[] pOutputData = new PduDataItem[maxArrayLen];
            var pOutputDataPtr = DefinePtrToStructure.ToIntPtrByArr<PduDataItem>(pOutputData);

            pShortname = "PDU_IOCTL_GET_ETH_PIN_OPTION";
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData2, pOutputDataPtr); // PDU_IOCTL_GET_ETH_PIN_OPTION 22
            if (RetStatus > 0)
            {
                System.Threading.Thread.Sleep(300);
                RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData2, pOutputDataPtr);
            };

            PduDataItem pInputData1 = new PduDataItem();
            pInputData1.ItemType = T_PDU_IT.PDU_IT_ETH_SWITCH_STATE;
            PduIoctlEthSwitchState [] a1 = new PduIoctlEthSwitchState[1]; // 19
            a1[0].EthernetSenseState = 1;
            a1[0].EthernetActPinNumber = 8;
            pInputData1.pData = a1.ToIntPtrByArr();

            PduDataItem[] pOutputData2 = new PduDataItem[maxArrayLen];
            var pOutputDataPtr2 = DefinePtrToStructure.ToIntPtrByArr<PduDataItem>(pOutputData2);

            System.Threading.Thread.Sleep(300); // delay 1000
            pShortname = "PDU_IOCTL_ETH_SWITCH_STATE"; // PDU_IOCTL_ETH_SWITCH_STATE PDU_IOCTL_MS_SET_BRIDGE_SWITCH_STATE
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, 19, ref pInputData1,  pOutputDataPtr2); // PDU_IOCTL_ETH_SWITCH_STATE // 6531:19 VDI-III :19
            if (RetStatus == PduStatus. PDU_ERR_FCT_FAILED.GetValue())
            {
                System.Threading.Thread.Sleep(300);
                RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData1, pOutputDataPtr2);
            };

            return (uint)RetStatus;
        }


        // 保护方法
        //protected virtual CInstanceDriver BuildDeviceDriver()
        //{
        //    // 实现代码
        //    throw new NotImplementedException();
        //}

        protected virtual bool GetSupportProtocolIDByFlag(uint protocolEnum, out uint protocolID, string firmwareVersion, string dllVersion, string apiVersion)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        protected uint IOCtrlReadVBATT(out uint pOutput)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        // 类的成员变量
        protected uint m_DeviceID;
        protected uint hMod;
        protected uint HMod;
        protected uint phCLL;
        protected uint DO_loopback;
        protected uint DO_testaddress;
        protected uint DO_eculogiacladdress;
        protected uint DO_ecufunctionaddress;
        protected uint DO_ecuipaddress;
        protected uint DO_datapins;
        protected bool open;

        // ADD BY MXL value
        protected uint DO_CP_RC78Handling;
        protected uint DO_CP_P6Max;
        protected uint DO_CP_P6Star;
        protected uint DO_CP_TesterPresentMessage;
        protected uint DO_CP_IsHaveGateway;
        protected uint DO_CP_GatewayAddress;
        protected uint DO_TPTYPE;
        protected uint DO_TESTERPRESENTDATA;
        protected uint DO_S3CLIENTTIME;
    }

}
