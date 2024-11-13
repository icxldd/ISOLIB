using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{
    class LoadDllAPI
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
        public extern static IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
        public extern static IntPtr GetProcAddress(IntPtr lib, string funcName);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        public extern static bool FreeLibrary(IntPtr lib);

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle")]
        public static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        public static extern uint GetLastError();

        [DllImport("user32", EntryPoint = "CallWindowProc")]
        public static extern int CallWindowProc(IntPtr lpPrevWndFunc, int hwnd, int MSG, int wParam, int lParam);
    }

    public class LoadDll
    {
        IntPtr m_hDllLib = IntPtr.Zero;//DLL文件名柄    

        public IntPtr GethDllLib()
        {

            return m_hDllLib;
        }
        public LoadDll()
        {
            m_hDllLib = IntPtr.Zero;
        }

        /// <summary>      
        /// 析构函数      
        /// </summary>      
        ~LoadDll()
        {
            if (m_hDllLib != IntPtr.Zero)
            {
                LoadDllAPI.FreeLibrary(m_hDllLib);//释放名柄      
            }
        }
        public void UnLoad()
        {
            try
            {
                if (m_hDllLib != IntPtr.Zero)
                {
                    LoadDllAPI.FreeLibrary(m_hDllLib);//释放名柄      
                }
                m_hDllLib = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                //Log4NetLibrary.LogHelper.WriteLog("error", ex);
            }
        }
        public bool Load(string dllpath)
        {
            if (!File.Exists(dllpath))
            {
                return false;
            }
            if (m_hDllLib == IntPtr.Zero)
            {
                m_hDllLib = LoadDllAPI.LoadLibrary(dllpath);
                if (m_hDllLib == IntPtr.Zero)
                {
                    return false;
                }
            }
            if (m_hDllLib != IntPtr.Zero)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>      
        /// 获取ＤＬＬ中一个方法的委托      
        /// </summary>      
        /// <param name="methodname"></param>      
        /// <param name="methodtype"></param>      
        /// <returns></returns>      
        public Delegate InvokeMethod(string methodname, Type methodtype)
        {
            if (m_hDllLib == IntPtr.Zero)
            {
                return null;
            }
            IntPtr MethodPtr = LoadDllAPI.GetProcAddress(m_hDllLib, methodname);
            if (MethodPtr != IntPtr.Zero)
            {
                return (Delegate)Marshal.GetDelegateForFunctionPointer(MethodPtr, methodtype);
            }
            else
            {
                return null;
            }
        }
        public static uint GetLastError()
        {

            return LoadDllAPI.GetLastError();
        }

    }
    public delegate UInt32 PConstruct([MarshalAs(UnmanagedType.LPStr)] string optionStr, IntPtr pApiTag);
    public delegate UInt32 PDestruct();
    // 定义回调函数委托
    public delegate void CALLBACKFNC(IntPtr lpv);
    // 定义委托
    public delegate uint PRegisterEventCallbackDelegate(uint hMod, uint hCLL, CALLBACKFNC callback);
    public delegate uint PConstructDelegate([MarshalAs(UnmanagedType.LPStr)] string lpFileName, IntPtr lpv);
    public delegate uint PModuleConnectDelegate(uint hMod);
    public delegate uint PGetVersionDelegate(uint hMod, ref PDU_VERSION_DATA pVersionData);
    public delegate uint PGetResourceIdsDelegate(uint hMod, PduRscData pResourceIdData, ref PduRscIdItem pResourceIdList);
    public delegate uint PGetObjectIdDelegate(T_Pdu_Objt pduObjectType, [MarshalAs(UnmanagedType.LPStr)] string pShortname, ref uint pPduObjectId);
    public delegate uint PCreateComLogicalLinkDelegate(uint hMod, PduRscData pRscData, uint resourceId, IntPtr pCllTag, 
        ref uint phCLL, PduFlagData pCllCreateFlag);
    public delegate uint PGetResourceStatusDelegate(ref PduRscStatusItem pResourceStatus);
    public delegate uint PConnectDelegate(uint hMod, uint hCLL);
    public delegate uint PIoCtlDelegate(uint hMod, uint hCLL, uint IoCtlCommandId, PduDataItem pInputData, ref PduDataItem pOutputData);
    public delegate uint PStartComPrimitiveDelegate(uint hMod, uint hCLL, T_Pdu_Copt CoPType, uint CoPDataSize, byte[] pCoPData,
        PduCopCtrlData  pCopCtrlData, IntPtr pCoPTag, ref uint phCoP);
    public delegate uint PSetComParamDelegate(uint hMod, uint hCLL,PduParamItem  pParamItem);
    public delegate uint PGetComParamDelegate(uint hMod, uint hCLL, uint ParamId, ref PduParamItem pParamItem);
    public delegate uint PGetEventItemDelegate(uint hMod, uint hCLL, ref PduEventItem pEventItem);
    public delegate uint PDestroyItemDelegate(PduItem pItem);
    public delegate uint PDestructDelegate();
    public delegate uint PDisconnectDelegate(uint hMod, uint hCLL);
    public delegate uint PModuleDisconnectDelegate(uint hMod);
    public delegate uint PGetLastErrorDelegate(uint hMod, uint hCLL, ref T_Pdu_Err_Evt pErrorCode, ref uint phCoP, ref uint pTimestamp, ref uint pExtraErrorInfo);
    public delegate uint PDestroyComLogicalLinkDelegate(uint hMod, uint hCLL);
    public delegate uint PGetUniqueRespIdTableDelegate(uint hMod, uint hCLL, ref PduUniqueRespIdTableItem pUniqueRespIdTable);
    public delegate uint PSetUniqueRespIdTableDelegate(uint hMod, uint hCLL,PduUniqueRespIdTableItem  pUniqueRespIdTable);


    public class DefineConst 
    {
        public static readonly string Construct = "PDUConstruct";
        public static readonly string Destruct = "PDUDestruct";
    }
    public class PduApiManager
    {

        private LoadDll m_loadDll = new LoadDll();
        public IntPtr m_DeviceHandle = IntPtr.Zero;
        private PConstruct m_PConstructMethod = null;
        private PDestruct m_PDestructMethod = null;

        public PduApiManager()
        {

            m_DeviceHandle = IntPtr.Zero;
        }

        public IntPtr openDevice(string deviceName, string Dllpath)
        {
            m_loadDll.UnLoad();
            m_loadDll.Load(Dllpath);

            m_DeviceHandle = m_loadDll.GethDllLib();
            InitMethod();
            return m_DeviceHandle;
        }
        public void Unload()
        {

            m_loadDll.UnLoad();
            m_DeviceHandle = IntPtr.Zero;
            m_loadDll.UnLoad();
        }
        private void InitMethod()
        {


            m_PConstructMethod = (PConstruct)m_loadDll.InvokeMethod(DefineConst.Construct, typeof(PConstruct));
            m_PDestructMethod = (PDestruct)m_loadDll.InvokeMethod(DefineConst.Destruct, typeof(PDestruct));




        }


        ////// 定义回调函数委托
        //public delegate void CALLBACKFNC(IntPtr lpv);

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
