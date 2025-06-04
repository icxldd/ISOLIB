#pragma once

//#include <Windows.h>

#ifdef PDUDLL_EXPORTS
#define PDUDLL_API __declspec(dllexport)
#else
#define PDUDLL_API __declspec(dllimport)
#endif
typedef enum E_PDU_IT
{
	PDU_IT_IO_UNUM32 = 0x1000, /* IOCTL UNUM32 item. */
	PDU_IT_IO_PROG_VOLTAGE = 0x1001, /* IOCTL Program Voltage item. */
	PDU_IT_IO_BYTEARRAY = 0x1002, /* IOCTL Byte Array item. */
	PDU_IT_IO_FILTER = 0x1003, /* IOCTL Filter item. */
	PDU_IT_IO_EVENT_QUEUE_PROPERTY = 0x1004, /* IOCTL Event Queue Property item. */
	PDU_IT_RSC_STATUS = 0x1100, /* Resource Status item */
	PDU_IT_PARAM = 0x1200, /* ComParam item */
	PDU_IT_RESULT = 0x1300, /* Result item */
	PDU_IT_STATUS = 0x1301, /* Status notification item */
	PDU_IT_ERROR = 0x1302, /* Error notification item */
	PDU_IT_INFO = 0x1303, /* Information notification item */
	PDU_IT_RSC_ID = 0x1400, /* Resource ID item */
	PDU_IT_RSC_CONFLICT = 0x1500, /* Resource Conflict Item */
	PDU_IT_MODULE_ID = 0x1600, /* Module ID item */
	PDU_IT_UNIQUE_RESP_ID_TABLE = 0x1700, /* Unique Response Id Table Item */
	PDU_IT_VEHICLE_ID = 0x1800,
	PDU_IT_ETH_SWITCH_STATE = 0x1801
} T_PDU_IT;
typedef struct {
	ULONG hMod;
	ULONG ResourceId;
	ULONG ResourceStatus;
} PDU_RSC_STATUS_DATA;

typedef struct {
	T_PDU_IT ItemType;
	char* name;
	ULONG NumEntries;
	PDU_RSC_STATUS_DATA pResourceStatusData;
	//PDU_RSC_STATUS_DATA* pResourceStatusData;
} PDU_RSC_STATUS_ITEM;

extern "C" {
	void HelloWord(PDU_RSC_STATUS_ITEM** pItems, PDU_RSC_STATUS_ITEM* pItem, UINT8* p2, ULONG* p3, PDU_RSC_STATUS_DATA P4, char* name);



	void HelloWord2(int hhh);
}