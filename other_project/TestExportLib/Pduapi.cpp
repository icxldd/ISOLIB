#include "pch.h"
#include "Pduapi.h"
#include <windows.h>
#include <wchar.h>

void HelloWord(PDU_RSC_STATUS_ITEM** pItems,PDU_RSC_STATUS_ITEM* pItem,UINT8 *p2, ULONG* p3, PDU_RSC_STATUS_DATA P4,char* name)
{
    wchar_t debugString[256]; // ����ÿ���������������256���ַ�
    OutputDebugStringA(("����"));
    pItem->NumEntries = 0x11;
    auto p1 = pItem->ItemType;
    auto p222 = pItem->NumEntries;

    swprintf(debugString, 256, L"itemType:%lu,numEntries:%lu",  p1, p222);
    pItems[0]->NumEntries = 2;
    OutputDebugString(debugString);

    // ������ʵ�ֺ����Ĺ���
    // ���磬��ӡ�ṹ��ĳ�Ա
    /*for (ULONG i = 0; i < pItem->NumEntries; ++i)
    {
       
        PDU_RSC_STATUS_DATA* pData = &pItem->pResourceStatusData[i];
       
        swprintf(debugString, 256, L"hMod: %lu, ResourceId: %lu, ResourceStatus: %lu,itemType:%lu,numEntries:%lu", pData->hMod, pData->ResourceId, pData->ResourceStatus, p1, p2);

        OutputDebugString(debugString);
    }*/
}



void HelloWord2(int hhh)
{

    OutputDebugStringA(("����2"));

}