#pragma once
#include <Windows.h>

#define HOOKCODESIZE 5

LPVOID OriFunc;
byte HookCode[HOOKCODESIZE];
byte OriCode[HOOKCODESIZE];

void Install_HOOK(LPVOID oriFunc, LPVOID Callback_Func)
{
	OriFunc = oriFunc;
	HookCode[0] = 0xE9;
	DWORD deltaddr = (DWORD)Callback_Func - (DWORD)oriFunc - 5;
	RtlMoveMemory(HookCode + 1, &deltaddr, 4);

	DWORD protect;
	if (VirtualProtect(oriFunc, HOOKCODESIZE, PAGE_EXECUTE_READWRITE, &protect))
	{
		RtlMoveMemory(OriCode, oriFunc, HOOKCODESIZE);
		RtlMoveMemory(oriFunc, HookCode, HOOKCODESIZE);
		VirtualProtect(oriFunc, HOOKCODESIZE, protect, &protect);
	}
}

void WriteCode(byte* code)
{
	DWORD protect;
	if (VirtualProtect(OriFunc, HOOKCODESIZE, PAGE_EXECUTE_READWRITE, &protect))
	{
		RtlMoveMemory(OriFunc, code, HOOKCODESIZE);
		VirtualProtect(OriFunc, HOOKCODESIZE, protect, &protect);
	}
}

void Suspend_HOOK()
{
	WriteCode(OriCode);
}



void Recovery_HOOK()
{
	WriteCode(HookCode);
}