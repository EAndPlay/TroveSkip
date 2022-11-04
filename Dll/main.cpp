#include "main.h"
#include <iostream>

HINSTANCE handle;

BOOL APIENTRY DllMain (HINSTANCE hInst, DWORD reason, LPVOID reserved)
{
    string text;
    switch (reason)
    {
        case DLL_PROCESS_ATTACH:
            text = "Process attach";
            break;

        case DLL_PROCESS_DETACH:
            text = "Process detach";
            break;

        case DLL_THREAD_ATTACH:
            text = "Thread attach";
            break;

        case DLL_THREAD_DETACH:
            text = "Thread Detach";
            break;

        default:
            string convert = to_string(reason);
            text = "unhandled " + convert;
            delete(&convert);
            break;
    }

    float x;
    ReadProcessMemory(handle, (void*)0x22E00660, &x, 4, nullptr);
    string str = to_string(x);
    MessageBox(nullptr, str.data(), "dll", MB_ICONINFORMATION);
    //delete(&str);

    MessageBox(nullptr, text.data(), "dll", MB_ICONINFORMATION);
    handle = hInst;
    //delete(&text);
    return TRUE;
}

void Hack()
{
    float x;
    ReadProcessMemory(handle, (void*)0x22E00660, &x, 4, nullptr);
    string str = to_string(x);
    MessageBox(nullptr, str.data(), "dll", MB_ICONINFORMATION);
    delete(&str);
}