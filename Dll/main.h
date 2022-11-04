#ifndef DLL_MAIN_H
#define DLL_MAIN_H

#include "main.h"
#include <winuser.h>
#include <windows.h>

using namespace std;

BOOL APIENTRY DllMain (HINSTANCE hInst, DWORD reason, LPVOID reserved);

void Hack();

#endif //DLL_MAIN_H
