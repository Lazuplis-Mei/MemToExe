#include "MemToFile.hpp"
#pragma comment(linker,"/subsystem:windows /entry:mainCRTStartup")

int main(int argc, char** argv)
{
	if (argc > 1)
	{
		int pid = atoi(argv[1]);
		if (pid)
		{
			HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
			if (hProcess)
			{
				char filepath[MAX_PATH];
				lstrcpy(filepath, argv[0]);
				GetFileFullPath(filepath, "mtfcore.dll");
				InjectDll(hProcess, filepath);
				CloseHandle(hProcess);
			}
		}
		else
		{
			STARTUPINFO stStartUpInfo;
			ZeroMemory(&stStartUpInfo, sizeof(STARTUPINFO));
			stStartUpInfo.cb = sizeof(stStartUpInfo);
			PROCESS_INFORMATION stProcessInfo;
			ZeroMemory(&stProcessInfo, sizeof(PROCESS_INFORMATION));
			if (CreateProcess(argv[1], NULL, NULL, NULL, FALSE, CREATE_NEW_CONSOLE, NULL, NULL, &stStartUpInfo, &stProcessInfo))
			{
				char filepath[MAX_PATH];
				lstrcpy(filepath, argv[0]);
				GetFileFullPath(filepath, "mtfcore.dll");
				InjectDll(stProcessInfo.hProcess, filepath);
				CloseHandle(stProcessInfo.hProcess);
				CloseHandle(stProcessInfo.hThread);
			}
		}
	}
	return 0;
}

