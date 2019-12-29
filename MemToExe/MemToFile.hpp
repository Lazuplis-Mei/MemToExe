#pragma once
#include <windows.h>

void GetFileFullPath(LPSTR exepath, LPCSTR file)
{
	LPSTR index = strrchr(exepath, '\\');
	exepath[index - exepath + 1] = '\0';
	strcat_s(exepath, MAX_PATH, file);
}

void InjectDll(HANDLE hProcess, LPCSTR dllname)
{
	if (hProcess)
	{
		LPVOID address = VirtualAllocEx(hProcess, 0, MAX_PATH, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
		if (address)
		{
			if (WriteProcessMemory(hProcess, address, (LPVOID)dllname, MAX_PATH, NULL))
			{
				HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)LoadLibrary, address, 0, NULL);
				if (hThread)
				{
					WaitForSingleObject(hThread, INFINITE);
					VirtualFreeEx(hProcess, address, MAX_PATH, MEM_COMMIT);
					CloseHandle(hThread);
				}
			}
		}
	}
}
