#include "mtfcore.hpp"

HWND hwnd;

BOOL WINAPI Hook_WriteProcessMemory(HANDLE hProcess, LPVOID lpBaseAddress, LPCVOID lpBuffer, SIZE_T nSize, SIZE_T* lpNumberOfBytesWritten)
{
	Suspend_HOOK();
	if (!IsWindow(hwnd))
		hwnd = FindWindow(NULL, L"MsgMonitor");
	if (IsWindow(hwnd))
	{
		COPYDATASTRUCT data;
		data.dwData = GetProcessId(hProcess);
		data.cbData = nSize + 4;
		int* buffer = (int*)((int)lpBuffer + nSize);
		*buffer = (int)lpBaseAddress;
		data.lpData = (PVOID)lpBuffer;
		SendMessage(hwnd, WM_COPYDATA, NULL, (LPARAM)&data);
		BOOL result = WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, nSize, lpNumberOfBytesWritten);
		Recovery_HOOK();
		return result;
	}
	else
		return WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, nSize, lpNumberOfBytesWritten);
}


BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	if (ul_reason_for_call == DLL_PROCESS_ATTACH)
	{
		hwnd = FindWindow(NULL, L"MsgMonitor");
		if (hwnd)Install_HOOK(WriteProcessMemory, Hook_WriteProcessMemory);
	}
    return TRUE;
}

