using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{
    internal class PduApi
    {
        ////// 定义回调函数委托
        //public delegate void CALLBACKFNC(IntPtr lpv);

        //// 加载 DLL
        //[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern IntPtr LoadLibrary(string lpFileName);

        //// 卸载 DLL
        //[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern bool FreeLibrary(IntPtr hModule);

        //// 注册事件回调函数
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PRegisterEventCallback(uint hMod, uint hCLL, CALLBACKFNC callback);

        //// 构造函数
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PConstruct([MarshalAs(UnmanagedType.LPStr)] string lpFileName, IntPtr lpv);

        //// 连接模块
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PModuleConnect(uint hMod);

        //// 获取版本信息
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetVersion(uint hMod, ref PDU_VERSION_DATA pVersionData);

        //// 获取资源 ID
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetResourceIds(uint hMod, PduRscData pResourceIdData, ref PduRscIdItem pResourceIdList);

        //// 获取对象 ID
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetObjectId(T_PDU_OBJT pduObjectType, [MarshalAs(UnmanagedType.LPStr)] string pShortname, ref uint pPduObjectId);

        //// 创建通信逻辑链路
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PCreateComLogicalLink(uint hMod, PDU_RSC_DATA pRscData, uint resourceId, IntPtr pCllTag, ref uint phCLL, PDU_FLAG_DATA pCllCreateFlag);

        //// 获取资源状态
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetResourceStatus(ref PDU_RSC_STATUS_ITEM pResourceStatus);

        //// 连接
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PConnect(uint hMod, uint hCLL);

        //// IO 控制
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PIoCtl(uint hMod, uint hCLL, uint IoCtlCommandId, PDU_DATA_ITEM pInputData, ref PDU_DATA_ITEM pOutputData);

        //// 启动通信原语
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PStartComPrimitive(uint hMod, uint hCLL, T_PDU_COPT CoPType, uint CoPDataSize, byte[] pCoPData, PDU_COP_CTRL_DATA pCopCtrlData, IntPtr pCoPTag, ref uint phCoP);

        //// 设置通信参数
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PSetComParam(uint hMod, uint hCLL, PDU_PARAM_ITEM pParamItem);

        //// 获取通信参数
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetComParam(uint hMod, uint hCLL, uint ParamId, ref PDU_PARAM_ITEM pParamItem);

        //// 获取事件项
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetEventItem(uint hMod, uint hCLL, ref PDU_EVENT_ITEM pEventItem);

        //// 销毁项
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PDestroyItem(PDU_ITEM pItem);

        //// 析构
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PDestruct();

        //// 断开连接
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PDisconnect(uint hMod, uint hCLL);

        //// 模块断开连接
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PModuleDisconnect(uint hMod);

        //// 获取最后一个错误
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetLastError(uint hMod, uint hCLL, ref T_PDU_ERR_EVT pErrorCode, ref uint phCoP, ref uint pTimestamp, ref uint pExtraErrorInfo);

        //// 销毁通信逻辑链路
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PDestroyComLogicalLink(uint hMod, uint hCLL);

        //// 获取唯一响应 ID 表
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PGetUniqueRespIdTable(uint hMod, uint hCLL, ref PDU_UNIQUE_RESP_ID_TABLE_ITEM pUniqueRespIdTable);

        //// 设置唯一响应 ID 表
        //[DllImport("PDUDll.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern uint PSetUniqueRespIdTable(uint hMod, uint hCLL, PDU_UNIQUE_RESP_ID_TABLE_ITEM pUniqueRespIdTable);

    }
}
