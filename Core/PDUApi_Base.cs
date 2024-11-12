using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{

    public static class DoipConstants
    {
        // 类型定义
        public const byte uint8 = 0xFF; // 示例值，实际使用时不需要赋值
        public const UInt32 UInt32 = 0xFFFFFFFF; // 示例值，实际使用时不需要赋值

        // p-pdu api参数
        public const int DOIP_testaddress = 0x20000;
        public const int DOIP_eculogiacladdress = 0x20001;
        public const int DOIP_ecufunctionaddress = 0x20002;
        public const int DOIP_ecuipaddress = 0x20003;
        public const int DOIP_datapins = 0x20004;

        public const int DOIP_CP_RC78Handling = 0x20005;
        public const int DOIP_CP_P6Max = 0x20006;
        public const int DOIP_CP_P6Star = 0x20007;
        public const int DOIP_CP_TesterPresentMessage = 0x20008;
        public const int DOIP_CP_IsHaveGateway = 0x20009;
        public const int DOIP_CP_GatewayAddress = 0x20010;
        public const int DOIP_TPTYPE = 0x20011;
        public const int DOIP_TESTERPRESENTDATA = 0x20012;
        public const int DOIP_S3CLIENTTIME = 0x20013;
    }

    public enum PduStatus
    {
        PDU_STATUS_NOERROR = 0x00000000,
        PDU_ERR_FCT_FAILED = 0x00000001,
        PDU_ERR_RESERVED_1 = 0x00000010,
        PDU_ERR_COMM_PC_TO_VCI_FAILED = 0x00000011,
        PDU_ERR_PDUAPI_NOT_CONSTRUCTED = 0x00000020,
        PDU_ERR_SHARING_VIOLATION = 0x00000021,
        PDU_ERR_RESOURCE_BUSY = 0x00000030,
        PDU_ERR_RESOURCE_TABLE_CHANGED = 0x00000031,
        PDU_ERR_RESOURCE_ERROR = 0x00000032,
        PDU_ERR_CLL_NOT_CONNECTED = 0x00000040,
        PDU_ERR_CLL_NOT_STARTED = 0x00000041,
        PDU_ERR_INVALID_PARAMETERS = 0x00000050,
        PDU_ERR_INVALID_HANDLE = 0x00000060,
        PDU_ERR_VALUE_NOT_SUPPORTED = 0x00000061,
        PDU_ERR_ID_NOT_SUPPORTED = 0x00000062,
        PDU_ERR_COMPARAM_NOT_SUPPORTED = 0x00000063,
        PDU_ERR_COMPARAM_LOCKED = 0x00000064,
        PDU_ERR_TX_QUEUE_FULL = 0x00000070,
        PDU_ERR_EVENT_QUEUE_EMPTY = 0x00000071,
        PDU_ERR_VOLTAGE_NOT_SUPPORTED = 0x00000080,
        PDU_ERR_MUX_RSC_NOT_SUPPORTED = 0x00000081,
        PDU_ERR_CABLE_UNKNOWN = 0x00000082,
        PDU_ERR_NO_CABLE_DETECTED = 0x00000083,
        PDU_ERR_CLL_CONNECTED = 0x00000084,
        PDU_ERR_TEMPPARAM_NOT_ALLOWED = 0x00000090,
        PDU_ERR_RSC_LOCKED = 0x000000A0,
        PDU_ERR_RSC_LOCKED_BY_OTHER_CLL = 0x000000A1,
        PDU_ERR_RSC_NOT_LOCKED = 0x000000A2,
        PDU_ERR_MODULE_NOT_CONNECTED = 0x000000A3,
        PDU_ERR_API_SW_OUT_OF_DATE = 0x000000A4,
        PDU_ERR_MODULE_FW_OUT_OF_DATE = 0x000000A5,
        PDU_ERR_PIN_NOT_CONNECTED = 0x000000A6
    }

    public enum ProtocolResult
    {
        PROTOCOL_NOERROR = 0,
        PROTOCOL_GENERALERROR,
        PROTOCOL_INVALID_HANDLE,
        PROTOCOL_INVALID_DEVICE,
        PROTOCOL_CREATEPROTOCOLINSTANCEFAIL,
        PROTOCOL_OPENDEVICEFAIL,
        PROTOCOL_ADDCHANNELFAIL,
        PROTOCOL_NORESPONSEMSG,
        PROTOCOL_NEGATIVEMSG,
        PROTOCOL_PENDINGMSG,
        PROTOCOL_POSITIVEMSG,
        PROTOCOL_NOSESSION,
        PROTOCOL_INVALID_PARAM,
        PROTOCOL_TIMEOUT,
        PROTOCOL_CHANNELEXIST,
        PROTOCOL_REQUESTFAIL,
        PROTOCOL_ALLRESPONSEOUTPUTED,     // All response has been output
        PROTOCOL_NEGATIVEPENDMSG,         // Negative response with pend
        PROTOCOL_POSITIVEPENDMSG,         // Positive response with pend
        PROTOCOL_TIMEOUTPENDMSG           // Positive response with pend
    }

    // 定义数据类型
    [StructLayout(LayoutKind.Sequential)]
    public struct PDU_VERSION_DATA
    {
        public UInt32 MVCI_Part1StandardVersion;
        public UInt32 MVCI_Part2StandardVersion;
        public UInt32 HwSerialNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] HwName;
        public UInt32 HwVersion;
        public UInt32 HwDate;
        public UInt32 HwInterface;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] FwName;
        public UInt32 FwVersion;
        public UInt32 FwDate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] VendorName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] PDUApiSwName;
        public UInt32 PDUApiSwVersion;
        public UInt32 PDUApiSwDate;
    }

    public enum T_PDU_STATUS
    {
        PDU_COPST_IDLE = 0x8010,
        PDU_COPST_EXECUTING = 0x8011,
        PDU_COPST_FINISHED = 0x8012,
        PDU_COPST_CANCELLED = 0x8013,
        PDU_COPST_WAITING = 0x8014,
        PDU_CLLST_OFFLINE = 0x8050,
        PDU_CLLST_ONLINE = 0x8051,
        PDU_CLLST_COMM_STARTED = 0x8052,
        PDU_MODST_READY = 0x8060,
        PDU_MODST_NOT_READY = 0x8061,
        PDU_MODST_NOT_AVAIL = 0x8062,
        PDU_MODST_AVAIL = 0x8063,
    }

    // 定义枚举
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
    public struct PduModuleData
    {
        public UInt32 ModuleTypeId;
        public UInt32 hMod;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pVendorModuleName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pVendorAdditionalInfo;
        public PduStatus ModuleStatus;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduModuleItem
    {
        public T_PDU_IT ItemType;
        public UInt32 NumEntrie;
        public IntPtr pModuleData;  //PduModuleData类型    PDU_MODULE_DATA *pModuleData;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduPinData
    {
        public UInt32 DlcpinNumber; /* Pin number on DLC */
        public UInt32 DlcpinTypeId; /* Pin ID */
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduRscData
    {
        public UInt32 BusTypeId; /* Bus Type Id (IN parameter) */
        public UInt32 ProtocolId; /* Protocol Id (IN parameter) */
        public UInt32 NumPinData; /* Number of items in the following array */
        public IntPtr pDlcpinData; //PduPinData 指针
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduRscIdItemData
    {
        public UInt32 hMod; /* MVCI protocol module Handle */
        public UInt32 NumIds; /* number of resources that match PDU_RSC_DATA */
        public IntPtr pResourceIdArray; //ULONG 指针
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduRscIdItem
    {
        public T_PDU_IT ItemType; /* value = PDU_IT_RSC_ID (IN parameter)*/
        public UInt32 NumModules; /* number of entries in pResourceIdDataArray. */
        public IntPtr pResourceIdDataArray; //PduRscIdItemData指针
    }

    public enum PduObjt
    {
        PDU_OBJT_PROTOCOL = 0x8021, /* Object type for object PROTOCOL of MDF.*/
        PDU_OBJT_BUSTYPE = 0x8022, /* Object type for object BUSTYPE of MDF.*/
        PDU_OBJT_IO_CTRL = 0x8023, /* Object type for object IO_CTRL of MDF.*/
        PDU_OBJT_COMPARAM = 0x8024, /* Object type for object COMPARAM of MDF.*/
        PDU_OBJT_PINTYPE = 0x8025, /* Object type for object PINTYPE of MDF.*/
        PDU_OBJT_RESOURCE = 0x8026
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduFlagData
    {
        public UInt32 NumFlagBytes; /* number of bytes in pFlagData array*/
        public IntPtr pFlagData;//uint8 指针
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduRscStatusData
    {
        public UInt32 hMod; /* Handle of a MVCI protocol module (IN parameter) */
        public UInt32 ResourceId; /* Resource ID (IN parameter) */
        public UInt32 ResourceStatus; /* Resource Information Status (OUT Parameter):(see D.1.6 for specific values.)*/
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduRscStatusItem
    {
        public T_PDU_IT ItemType; /* value= PDU_IT_RSC_STATUS (IN parameter)*/
        public UInt32 NumEntries; /* (IN Parameter) = number of entries in pResourceStatusDataarray. */
        public IntPtr pResourceStatusData;//PduRscStatusData指针
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduDataItem
    {
        public T_PDU_IT ItemType; /* value= one of the IOCTL constants from D.1.1 */
        public IntPtr pData; /* pointer to the specific IOCTL data structure */
    }

    public enum PduCopt
    {
        PDU_COPT_STARTCOMM = 0x8001,
        PDU_COPT_STOPCOMM = 0x8002,
        PDU_COPT_UPDATEPARAM = 0x8003,
        PDU_COPT_SENDRECV = 0x8004,
        PDU_COPT_DELAY = 0x8005,
        PDU_COPT_RESTORE_PARAM = 0x8006
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduExpRespData
    {
        public UInt32 ResponseType; /* 0 = positive response; 1 = negative response */
        public UInt32 AcceptanceId; /* ID assigned by application to be returned in PDU_RESULT_DATA,which indicates which expected response matched */
        public UInt32 NumMaskPatternBytes; /* number of bytes in the Mask Data and Pattern Data*/
        public byte[] pMaskData; /* Pointer to Mask Data. Bits set to a ‘1' are care bits, ‘0' are don't carebits uint8 *. */
        public byte[] pPatternData; /* Pointer to Pattern Data. Bytes to compare after the mask is applied */
        public UInt32 NumUniqueRespIds;
        public UInt32[] pUniqueRespIds;//ULONG *
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduCopCtrlData
    {
        public UInt32 Time;
        public Int32 NumSendCycles;
        public Int32 NumReceiveCycles;
        public UInt32 TempParamUpdate;
        public PduFlagData TxFlag;
        public UInt32 NumPossibleExpectedResponses;
        public IntPtr pExpectedResponseArray; //PduExpRespData *
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduIpAddrInfo
    {
        public UInt32 IpVersion;
        public byte[] pAdddress;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduIoctlVehicleIdRequest
    {
        public UInt32 PreselectionMode;
        [MarshalAs(UnmanagedType.LPStr)]
        public string PreselectionValue;
        public UInt32 CombinationMode;
        public UInt32 VehicleDiscoveryTime;
        public UInt32 NumDestinationAddresses;
        public IntPtr pDestinationAddresses;//PduIpAddrInfo *指针
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduIoctlEthSwitchState
    {
        public UInt32 EthernetSenseState;
        public UInt32 EthernetActPinNumber;
    }

    public enum PduEvtData
    {
        PDU_EVT_DATA_AVAILABLE = 0x0801,
        PDU_EVT_DATA_LOST = 0x0802
    }

    public enum PduPt
    {
        PDU_PT_UNUM8 = 0x00000101, /* Unsigned byte */
        PDU_PT_SNUM8 = 0x00000102, /* Signed byte */
        PDU_PT_UNUM16 = 0x00000103, /* Unsigned two bytes */
        PDU_PT_SNUM16 = 0x00000104, /* Signed two bytes */
        PDU_PT_UNUM32 = 0x00000105, /* Unsigned four bytes */
        PDU_PT_SNUM32 = 0x00000106, /* Signed four bytes */
        PDU_PT_BYTEFIELD = 0x00000107,
        PDU_PT_STRUCTFIELD = 0x00000108,
        PDU_PT_LONGFIELD = 0x00000109
    }

    public enum PduPc
    {
        PDU_PC_TIMING = 1,
        PDU_PC_INIT = 2,
        PDU_PC_COM = 3,
        PDU_PC_ERRHDL = 4,
        PDU_PC_BUSTYPE = 5,
        PDU_PC_UNIQUE_ID = 6,
        PDU_PC_TESTER_PRESENT = 7
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PduParamItem
    {
        public T_PDU_IT ItemType;
        public UInt32 ComParamId;
        public PduPt ComParamDataType;
        public PduPc ComParamClass;
        public IntPtr pComParamData;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct PduItem
    {
        public T_PDU_IT ItemType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduEventItem
    {
        public T_PDU_IT ItemType;
        public UInt32 hCop;
        public IntPtr pCoPTag;
        public UInt32 Timestamp;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduExtraInfo
    {
        public UInt32 NumHeaderBytes;
        public UInt32 NumFooterBytes;
        public byte[] pHeaderBytes;
        public byte[] pFooterBytes;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PduResultData
    {
        public PduFlagData RxFlag;
        public UInt32 UniqueRespIdentifier;
        public UInt32 AcceptanceId;
        public PduFlagData TimestampFlags;
        public UInt32 TxMsgDoneTimestamp;
        public UInt32 StartMsgTimestamp;
        public IntPtr pExtraInfo; // PduExtraInfo *
        public UInt32 NumDataBytes;
        public byte[] pDataBytes;
    }

    public enum PduErrEvt
    {
        PDU_ERR_EVT_NOERROR = 0x00000000,
        PDU_ERR_EVT_FRAME_STRUCT = 0x00000100,
        PDU_ERR_EVT_TX_ERROR = 0x00000101,
        PDU_ERR_EVT_TESTER_PRESENT_ERROR = 0x00000102,
        PDU_ERR_EVT_RSC_LOCKED = 0x00000109,
        PDU_ERR_EVT_RX_TIMEOUT = 0x00000103,
        PDU_ERR_EVT_RX_ERROR = 0x00000104,
        PDU_ERR_EVT_PROT_ERR = 0x00000105,
        PDU_ERR_EVT_LOST_COMM_TO_VCI = 0x00000106,
        PDU_ERR_EVT_VCI_HARDWARE_FAULT = 0x00000107,
        PDU_ERR_EVT_INIT_ERROR = 0x00000108
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduEcuUniqueRespData
    {
        public UInt32 UniqueRespIdentifier; // filled out by application
        public UInt32 NumParamItems; // number of ComParams for the Unique Identifier
        public IntPtr pParams; //PduParamItem * pointer to array of ComParam items to uniquely define a ECU response. The list is protocol specific
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduUniqueRespIdTableItem
    {
        public T_PDU_IT ItemType; // value= PDU_IT_UNIQUE_RESP_ID_TABLE
        public UInt32 NumEntries; // number of entries in the table
        /// <summary>
        /// 指针
        /// <see cref="PduEcuUniqueRespData"/>
        /// </summary>
        public IntPtr pUniqueData; //pointer to array of table entries for each ECU response

        [MarshalAs(UnmanagedType.IUnknown)]
        public PduEcuUniqueRespData UniqueData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduIoProgVoltageData
    {
        public UInt32 ProgVoltage_mv; // programming voltage in mV
        public UInt32 PinOnDLC; // pin number on Data Link Connector
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PduParamBytefieldData
    {
        public UInt32 ParamMaxLen;
        public UInt32 ParamActLen;
        public byte[] pDataArray;
    }


}
