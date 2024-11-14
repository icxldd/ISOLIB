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

    // 定义回调函数委托
    public delegate void CALLBACKFNC(IntPtr lpv);

    public delegate uint PConstructDelegate([MarshalAs(UnmanagedType.LPStr)] string optionStr, IntPtr pApiTag);

    // 定义委托
    public delegate uint PRegisterEventCallbackDelegate(uint hMod, uint hCLL, CALLBACKFNC callback);
    public delegate uint PGetModuleIdsDelegate(IntPtr pduGetModelIds);//PDU_MODULE_ITEM **
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
        public static readonly string PDUConstruct = "PDUConstruct";
        public static readonly string PDURegisterEventCallback = "PDURegisterEventCallback";
        public static readonly string PDUGetModuleIds = "PDUGetModuleIds";
        public static readonly string PDUModuleConnect = "PDUModuleConnect";
        public static readonly string PDUGetVersion = "PDUGetVersion";
        public static readonly string PDUGetResourceIds = "PDUGetResourceIds";
        public static readonly string PDUGetObjectId = "PDUGetObjectId";
        public static readonly string PDUCreateComLogicalLink = "PDUCreateComLogicalLink";
        public static readonly string PDUGetResourceStatus = "PDUGetResourceStatus";
        public static readonly string PDUConnect = "PDUConnect";
        public static readonly string PDUIoCtl = "PDUIoCtl";
        public static readonly string PDUStartComPrimitive = "PDUStartComPrimitive";
        public static readonly string PDUSetComParam = "PDUSetComParam";
        public static readonly string PDUGetComParam = "PDUGetComParam";
        public static readonly string PDUGetEventItem = "PDUGetEventItem";
        public static readonly string PDUDestroyItem = "PDUDestroyItem";
        public static readonly string PDUDestruct = "PDUDestruct";
        public static readonly string PDUDisconnect = "PDUDisconnect";
        public static readonly string PDUModuleDisconnect = "PDUModuleDisconnect";
        public static readonly string PDUGetLastError = "PDUGetLastError";
        public static readonly string PDUDestroyComLogicalLink = "PDUDestroyComLogicalLink";
        public static readonly string PDUGetUniqueRespIdTable = "PDUGetUniqueRespIdTable";
        public static readonly string PDUSetUniqueRespIdTable = "PDUSetUniqueRespIdTable";


    }
    public class PduApiManager
    {

        private LoadDll m_loadDll = new LoadDll();
        public IntPtr m_DeviceHandle = IntPtr.Zero;
        private PConstructDelegate m_PConstructMethod = null;
        private PRegisterEventCallbackDelegate m_PRegisterEventCallbackMethod = null;
        private PGetModuleIdsDelegate m_PGetModuleIdsMethod = null;
        private PModuleConnectDelegate m_PModuleConnectMethod = null;
        private PGetVersionDelegate m_PGetVersionMethod = null;
        private PGetResourceIdsDelegate m_PGetResourceIdsMethod = null;
        private PGetObjectIdDelegate m_PGetObjectIdMethod = null;
        private PCreateComLogicalLinkDelegate m_PCreateComLogicalLinkMethod = null;
        private PGetResourceStatusDelegate m_PGetResourceStatusMethod = null;
        private PConnectDelegate m_PConnectMethod = null;
        private PIoCtlDelegate m_PIoCtlMethod = null;
        private PStartComPrimitiveDelegate m_PStartComPrimitiveMethod = null;
        private PSetComParamDelegate m_PSetComParamMethod = null;
        private PGetComParamDelegate m_PGetComParamMethod = null;
        private PGetEventItemDelegate m_PGetEventItemMethod = null;
        private PDestroyItemDelegate m_PDestroyItemMethod = null;
        private PDestructDelegate m_PDestructMethod = null;
        private PDisconnectDelegate m_PDisconnectMethod = null;
        private PModuleDisconnectDelegate m_PModuleDisconnectMethod = null;
        private PGetLastErrorDelegate m_PGetLastErrorMethod = null;
        private PDestroyComLogicalLinkDelegate m_PDestroyComLogicalLinkMethod = null;
        private PGetUniqueRespIdTableDelegate m_PGetUniqueRespIdTableMethod = null;
        private PSetUniqueRespIdTableDelegate m_PSetUniqueRespIdTableMethod = null;

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
            m_PConstructMethod = (PConstructDelegate)m_loadDll.InvokeMethod(DefineConst.PDUConstruct, typeof(PConstructDelegate));
            m_PRegisterEventCallbackMethod = (PRegisterEventCallbackDelegate)m_loadDll.InvokeMethod(DefineConst.PDURegisterEventCallback, typeof(PRegisterEventCallbackDelegate));
            m_PGetModuleIdsMethod = (PGetModuleIdsDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetModuleIds, typeof(PGetModuleIdsDelegate));
            m_PModuleConnectMethod = (PModuleConnectDelegate)m_loadDll.InvokeMethod(DefineConst.PDUModuleConnect, typeof(PModuleConnectDelegate));
            m_PGetVersionMethod = (PGetVersionDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetVersion, typeof(PGetVersionDelegate));
            m_PGetResourceIdsMethod = (PGetResourceIdsDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetResourceIds, typeof(PGetResourceIdsDelegate));
            m_PGetObjectIdMethod = (PGetObjectIdDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetObjectId, typeof(PGetObjectIdDelegate));
            m_PCreateComLogicalLinkMethod = (PCreateComLogicalLinkDelegate)m_loadDll.InvokeMethod(DefineConst.PDUCreateComLogicalLink, typeof(PCreateComLogicalLinkDelegate));
            m_PGetResourceStatusMethod = (PGetResourceStatusDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetResourceStatus, typeof(PGetResourceStatusDelegate));
            m_PConnectMethod = (PConnectDelegate)m_loadDll.InvokeMethod(DefineConst.PDUConnect, typeof(PConnectDelegate));
            m_PIoCtlMethod = (PIoCtlDelegate)m_loadDll.InvokeMethod(DefineConst.PDUIoCtl, typeof(PIoCtlDelegate));
            m_PStartComPrimitiveMethod = (PStartComPrimitiveDelegate)m_loadDll.InvokeMethod(DefineConst.PDUStartComPrimitive, typeof(PStartComPrimitiveDelegate));
            m_PSetComParamMethod = (PSetComParamDelegate)m_loadDll.InvokeMethod(DefineConst.PDUSetComParam, typeof(PSetComParamDelegate));
            m_PGetComParamMethod = (PGetComParamDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetComParam, typeof(PGetComParamDelegate));
            m_PGetEventItemMethod = (PGetEventItemDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetEventItem, typeof(PGetEventItemDelegate));
            m_PDestroyItemMethod = (PDestroyItemDelegate)m_loadDll.InvokeMethod(DefineConst.PDUDestroyItem, typeof(PDestroyItemDelegate));
            m_PDestructMethod = (PDestructDelegate)m_loadDll.InvokeMethod(DefineConst.PDUDestruct, typeof(PDestructDelegate));
            m_PDisconnectMethod = (PDisconnectDelegate)m_loadDll.InvokeMethod(DefineConst.PDUDisconnect, typeof(PDisconnectDelegate));
            m_PModuleDisconnectMethod = (PModuleDisconnectDelegate)m_loadDll.InvokeMethod(DefineConst.PDUModuleDisconnect, typeof(PModuleDisconnectDelegate));
            m_PGetLastErrorMethod = (PGetLastErrorDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetLastError, typeof(PGetLastErrorDelegate));
            m_PDestroyComLogicalLinkMethod = (PDestroyComLogicalLinkDelegate)m_loadDll.InvokeMethod(DefineConst.PDUDestroyComLogicalLink, typeof(PDestroyComLogicalLinkDelegate));
            m_PGetUniqueRespIdTableMethod = (PGetUniqueRespIdTableDelegate)m_loadDll.InvokeMethod(DefineConst.PDUGetUniqueRespIdTable, typeof(PGetUniqueRespIdTableDelegate));
            m_PSetUniqueRespIdTableMethod = (PSetUniqueRespIdTableDelegate)m_loadDll.InvokeMethod(DefineConst.PDUSetUniqueRespIdTable, typeof(PSetUniqueRespIdTableDelegate));

        }

    }
}
