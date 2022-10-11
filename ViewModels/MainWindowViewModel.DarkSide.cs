using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace TroveSkip.ViewModels
{
    public sealed partial class MainWindowViewModel
    {
        private readonly int[] _speedOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x1BC };
        
//1F8 - Battle Factor
//1F4 - Critical Damage
//1F0 - Experience gain
//1DC - Flasks amount
//1D8 - Attack Speed
//1D4 - Lasermancy
//1D0 - Magic Find
//1C0 - Jumps amount
//1B8 - Critical Hit
//1AC - Health Regeneration
//1B0 - Energy Regeneration
//1A4 - Maximum Health
//1A0 - Magic Damage
//1A8 - Maximum Energy
//19C - Physiscal Damage
//238 - Light

//baseAddress + (0xCFC | 0xD1C) = Players amount in current world

        private static readonly int[] CoordsOffsets = { 0x8, 0x28, 0xC4, 0x4 };
        private static readonly int[] ViewOffsets = { 0x4, 0x24, 0x84, 0x0 };
        private static readonly int[] AccelOffsets = { 0x4, 0x11C, 0xC4, 0x4 };
        private static readonly int[] NameOffests = { 0x8, 0x28, 0x1B4, 0x0 };
	    private static readonly int[] ChatOpenedOffsets = { 0x20, 0x1C };
        private static readonly int[] PowerRankOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x200 };
        private static readonly int[] StatsEncKeyOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x21C };

        private readonly int[] _xPosition = CoordsOffsets.Join(0x60);
        private readonly int[] _yPosition = CoordsOffsets.Join(0x64);
        private readonly int[] _xVelocity = AccelOffsets.Join(0x90);
        private readonly int[] _xView = ViewOffsets.Join(0x100);

        //private readonly int[] MaxCamDist = { 0x4, 0x3C };
        //private readonly int[] MinCamDist = { 0x4, 0x38 };
        
        #region Signatures

        private readonly int[] _zoomHack = { 0xF3, 0x0F, 0x11, 0x5F, 0x2C };

        private readonly int[] _zoomHackEnabled = { 0xF3, 0x0F, 0x11, 0x57, 0x2C };

        private readonly int[] _fovHack =
            {0xF3, 0x0F, 0x10, 0x41, 0x28, 0xF3, 0x0F, 0x59, 0x05, -1, -1, -1, -1, 0x8B, 0x4D, 0xF4};

        private readonly int[] _fovHackEnabled =
            {0xF3, 0x0F, 0x10, 0x41, 0x28, 0xF3, 0x0F, 0x59, 0x0D, -1, -1, -1, -1, 0x8B, 0x4D, 0xF4};

        private readonly int[] _antiAfk = {0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0xD9, 0xEE, 0x83, 0xEC, 0x58};
        private readonly byte[] _antiAfkCave = { 0xC3, 0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8 };

        private readonly int[] _mapHack =
        {
            0x77, -1, 0xB8, -1, -1, -1, -1, 0xF3, 0x0F, 0x10, 0x08, 0xF3, 0x0F, 0x11, 0x89, -1, -1, -1, -1, 0x8B, 0x89,
            0xA8, 0x00, 0x00, 0x00, 0x85, 0xC9, 0x74, -1
        };
        
        private readonly int[] _mapHackEnabled =
        {
            0xEB, -1, 0xB8, -1, -1, -1, -1, 0xF3, 0x0F, 0x10, 0x08, 0xF3, 0x0F, 0x11, 0x89, -1, -1, -1, -1,
            0x8B, 0x89, 0xA8, 0x00, 0x00, 0x00, 0x85, 0xC9, 0x74, -1
        };
        
        private readonly int[] _geodeTool = { 0xDF, 0xF1, 0xDD, 0xD8, 0x72, 0x35, 0x8D, 0x64, 0x24 };
        
	    private readonly int[] _geodeToolEnabled = { 0xDF, 0xF0, 0xDD, 0xD8, 0x72, 0x35, 0x8D, 0x64, 0x24 };
        
	    private readonly int[] _mining = { 0xDF, 0xF1, 0xDD, 0xD8, 0x72, 0x61 };
        
	    private readonly int[] _miningEnabled = { 0xDF, 0xF0, 0xDD, 0xD8, 0x72, 0x61 };

        private readonly int[] _chamsMonsters = { 0x0F, 0x29, 0x07, 0x8B, 0xC7, 0x5F, 0x5E, 0x8B, 0xE5, 0x5D, 0x8B };

        private readonly int[] _chamsMonstersEnabled = { 0x0F, 0x29, 0x0F, 0x8B, 0xC7, 0x5F, 0x5E, 0x8B, 0xE5, 0x5D, 0x8B };

        #endregion

        private unsafe int ReadInt(int[] offsets)
        {
            fixed (byte* p = GetBuffer(offsets))
            {
                return *(int*) p;
            }
        }

        private unsafe uint ReadUInt(int[] offsets)
        {
            fixed (byte* p = GetBuffer(offsets))
            {
                return *(uint*) p;
            }
        }

        private unsafe float ReadFloat(int[] offsets)
        {
            fixed (byte* p = GetBuffer(offsets))
            {
                return *(float*) p;
            }
        }

        private unsafe float ReadFloat(int address)
        {
            fixed (byte* p = GetBuffer(address))
            {
                return *(float*) p;
            }
        }

        private string ReadString(int[] offsets) => Encoding.ASCII.GetString(GetBuffer(offsets, 16));

        private void WriteInt(int[] offsets, int value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes(value));
        private void WriteUInt(int[] offsets, uint value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes((int)value));
        private void WriteFloat(int[] offsets, float value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes(value));
        private void WriteFloat(int address, float value) => WriteMemory(address, BitConverter.GetBytes(value));

        private unsafe void OverwriteBytes(int[] pattern, int[] bytes)
        {
            var address = FindSignature(pattern);
            //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
            if (address == 0) return;

            var val = new byte[bytes.Length];
            var buffer = new byte[pattern.Length];
            ReadMemory(address, buffer);

            fixed (byte* newCode = val)
            {
                fixed (int* code = bytes, offset = pattern)
                {
                    for (var i = 0; i < pattern.Length; i++)
                    {
                        *(newCode + i) = *(offset + i) == -1 || *(code + i) == -1 ? buffer[i] : (byte) *(code + i);
                    }
                }
            }

            WriteMemory(address, val);
        }

        private unsafe int FindSignature(int[] pattern, Process process = null)
        {
            process ??= HookModel.Process;
            var module = process.MainModule;
            if (module == null)
            {
                MessageBox.Show("Module is NULL at 'FindSignature', doesn't it surprise?");
                return 0;
            }
            var startAdd = (int) module.BaseAddress;
            var allocSize = module.ModuleMemorySize;
            var buffer = new byte[allocSize];
            ReadProcessMemory(process.Handle, startAdd, buffer, allocSize, out _);
            var compare = 0;
            var count = pattern.Length;

            fixed (byte* code = buffer)
            {
                fixed (int* offset = pattern)
                {
                    var firstCode = *offset;
                    var isFirstEmpty = firstCode == -1;
                    for (var i = 0; i < allocSize; i++)
                    {
                        if (*(code + i) != firstCode && !isFirstEmpty) continue;
                        for (int g = 1; g < count; g++)
                        {
                            if (*(code + i + g) != *(offset + g) && *(offset + g) != -1)
                            {
                                compare = 1;
                                break;
                            }

                            compare++;
                            if (compare == count)
                                return i + startAdd;
                        }
                    }
                }
            }

            return 0;
        }
        
        private void ReadMemory(int address, byte[] buffer) => ReadProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private unsafe void ReadMemory(int address, byte* buffer) => ReadProcessMemory(_handle, address, buffer, 4, out _);
        private void WriteMemory(int address, byte[] buffer) => WriteProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private void ReadMemory(IntPtr handle, int address, byte[] buffer) => ReadProcessMemory(handle, address, buffer, buffer.Length, out _);
        private void WriteMemory(IntPtr handle, int address, byte[] buffer) => WriteProcessMemory(handle, address, buffer, buffer.Length, out _);

        private unsafe int GetAddress(IEnumerable<int> offsets)
        {
            var bytes = new byte[4];
            var address = _currentBaseAddress;
            fixed (byte* p = bytes)
            {
                var num = (int*)p;
                foreach (var offset in offsets)
                {
                    ReadMemory(address, bytes);
                    address = *num + offset;
                }
                return address;
            }
        }
        private unsafe int GetAddress(IntPtr handle, IEnumerable<int> offsets)
        {
            var bytes = new byte[4];
            var address = _currentBaseAddress;
            fixed (byte* p = bytes)
            {
                var num = (int*)p;
                foreach (var offset in offsets)
                {
                    ReadProcessMemory(handle, address, bytes, 4, out _);
                    address = *num + offset;
                }
                return address;
            }
        }

        private byte[] GetBuffer(IEnumerable<int> offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddress(offsets), bytes);
            return bytes;
        }
        private byte[] GetBuffer(int address)
        {
            var bytes = new byte[4];
            ReadMemory(address, bytes);
            return bytes;
        }
        
        // private static unsafe byte[] AsmJump(ulong destination, ulong origin, byte[] cave = null)
        // {
        //     var jumpStart = destination - origin - 5;
        //     var str = jumpStart.ToString("x");
        //     var builder = new StringBuilder();
        //     if (str.Length == 7)
        //         builder.Append("0");
        //     builder.Append(str).Append("E9");
        //
        //     var hex = new byte[builder.Length / 2];
        //     fixed (byte* hexByte = hex)
        //     {
        //         var dump = builder.ToString();
        //         fixed (char* code = dump)
        //         {
        //             for (var i = 0; i < hex.Length; i++)
        //                 *(hexByte + i) = Convert.ToByte(new string(code, i * 2, 2), 16);
        //         }
        //     }
        //
        //     Array.Reverse(hex);
        //     if (cave != null)
        //         hex = cave.Join(hex);
        //     
        //     return hex;
        // }
        private static unsafe byte[] AsmJump(ulong destination, ulong origin, byte[] cave = null)
        {
            var jumpStart = destination - origin - 5;
            var bytes = new byte[4];
            var address = BitConverter.GetBytes(jumpStart);
            var addressLength = address.Length;
            if (addressLength > bytes.Length)
                Array.Resize(ref bytes, addressLength + 1);
            Array.Copy(address, 0, bytes, 1, addressLength - 1);
            bytes[0] = 0xE9;
            int newLength;
            for (newLength = bytes.Length - 1; newLength > 0; newLength--)
            {
                if (bytes[newLength] != 0) break;
            }
            Array.Resize(ref bytes, newLength + 1);

            if (cave != null)
                bytes = cave.Join(bytes);
            
            return bytes;
        }
        
        #region Dlls
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 2035711,
            Terminate = 1,
            CreateThread = 2,
            VirtualMemoryOperation = 8,
            VirtualMemoryRead = 16,
            VirtualMemoryWrite = 32,
            DuplicateHandle = 64,
            CreateProcess = 128,
            SetQuota = 256,
            SetInformation = 512,
            QueryInformation = 1024,
            QueryLimitedInformation = 4096,
            Synchronize = 1048576
        }
        [Flags]
        public enum PositionFlags
        {
            AsyncWindowPos= 0x4000,
            DeferErase= 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            NoActivate = 0x0010,
            NoCopyBits = 0x0100,
            NoMove = 0x0002,
            NoOwnerZOrder = 0x0200,
            NoRedraw = 0x0008,
            NoReposition = 0x0200,
            NoSendChanging = 0x0400,
            NoSize = 0x0001,
            NoZOrder = 0x0004,
            ShowWindow = 0x0040
        }
        [Flags]
        
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }
        
        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll")]
        static extern bool VirtualFreeEx(
            IntPtr handle,
            int lpAddress,
            int dwSize,
            AllocationType allocationType);
        
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(
            IntPtr handle,
            int lpAddress,
            int dwSize,
            MemoryProtection flNewProtect,
            out MemoryProtection oldProtection);
        
        [DllImport("kernel32.dll")]
        static extern int VirtualAllocEx(
            IntPtr hProcess,
            int lpAddress,
            int dwSize,
            AllocationType allocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess, 
            int lpBaseAddress, 
            byte[] lpBuffer, 
            int dwSize, 
            out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        private static extern unsafe bool ReadProcessMemory(
            IntPtr hProcess, 
            int lpBaseAddress, 
            byte* lpBuffer, 
            int dwSize, 
            out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(
          IntPtr hProcess,
          int lpBaseAddress,
          byte[] lpBuffer,
          int nSize,
          out IntPtr lpNumberOfBytesWritten);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(
            IntPtr handle,
            out int processId);
        
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);

        #endregion
    }
}