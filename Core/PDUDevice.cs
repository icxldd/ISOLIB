using ISOLib.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{

public class PDUDevice : ITPDevice
    {
        private int maxArrayLen = int.MaxValue;
        private PduApiManager pduApiManager;
        // 类的构造函数和析构函数
        public PDUDevice()
        {
            // 初始化代码（如果有的话）
            pduApiManager = PduApiManager.getInstance();
        }

        ~PDUDevice()
        {
            // 析构代码（如果有的话）
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
                    openResult =pduApiManager.m_PConstructMethod(null, null);
                    if (openResult == PduStatus.PDU_ERR_SHARING_VIOLATION.GetValue())
                    {
                        pduApiManager.m_PDestructMethod();
                        pduApiManager.m_PConstructMethod(null, null);
                    }
                    pduApiManager.m_PRegisterEventCallbackMethod(0xFFFFFFFF, 0xFFFFFFFF, null);
                    PduModuleItem[] ModuleIds=new PduModuleItem [maxArrayLen];
                    var ptr = DefinePtrToStructure.ToIntPtrByArr<PduModuleItem>(ModuleIds);
                    openResult = pduApiManager.m_PGetModuleIdsMethod(ptr);
                    if (openResult == PduStatus.PDU_STATUS_NOERROR.GetValue() && ModuleIds[0].NumEntrie > 0)
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
            // 实现代码
            throw new NotImplementedException();
        }

        public bool Connect(out IntPtr pTPHandle, uint protocolEnum, uint flags, uint baudRate)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        public bool DisConnect(IntPtr tpHandle)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        public bool ReadVersion(out string firmwareVersion, out string dllVersion, out string apiVersion)
        {
            // 实现代码
            throw new NotImplementedException();
        }

        public bool DeviceIOCtrl(uint controlId, IntPtr pInput, IntPtr pOutput)
        {
            // 实现代码
            throw new NotImplementedException();
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
            PduDataItem pOutputData = new PduDataItem() ;
            var pOutputDataPtr = pOutputData.GetIntPtr();
            pShortname = "PDU_IOCTL_GET_ETH_PIN_OPTION";
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData2, ref pOutputDataPtr); // PDU_IOCTL_GET_ETH_PIN_OPTION 22
            if (RetStatus > 0)
            {
                System.Threading.Thread.Sleep(300);
                RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData2, ref pOutputDataPtr);
            };

            PduDataItem pInputData1 = new PduDataItem();
            pInputData1.ItemType = T_PDU_IT.PDU_IT_ETH_SWITCH_STATE;
            PduIoctlEthSwitchState [] a1 = new PduIoctlEthSwitchState[1]; // 19
            a1[0].EthernetSenseState = 1;
            a1[0].EthernetActPinNumber = 8;
            pInputData1.pData = a1.ToIntPtrByArr();

            PduDataItem pOutputData2 = new PduDataItem();
            var pOutputDataPtr2 = pOutputData2.GetIntPtr();
            System.Threading.Thread.Sleep(300); // delay 1000
            pShortname = "PDU_IOCTL_ETH_SWITCH_STATE"; // PDU_IOCTL_ETH_SWITCH_STATE PDU_IOCTL_MS_SET_BRIDGE_SWITCH_STATE
            pduApiManager.m_PGetObjectIdMethod(pduObjectType, pShortname, ref pPduObjectId);
            RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, 19, ref pInputData1, ref pOutputDataPtr2); // PDU_IOCTL_ETH_SWITCH_STATE // 6531:19 VDI-III :19
            if (RetStatus == PduStatus. PDU_ERR_FCT_FAILED.GetValue())
            {
                System.Threading.Thread.Sleep(300);
                RetStatus = pduApiManager.m_PIoCtlMethod(HMod, 0xFFFFFFFF, pPduObjectId, ref pInputData1, ref pOutputDataPtr2);
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
