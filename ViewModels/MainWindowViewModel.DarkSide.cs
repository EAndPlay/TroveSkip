using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;

namespace TroveSkipFramework.ViewModels
{
    public sealed partial class MainWindowViewModel
    {
        private readonly int[] _speedOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x1BC };
//1F8 - battle factor;
//1F4 - crit dmg;
//1F0 - experience;
//1DC - flasks;
//1D8 - AS;
//1D4 - lasermancy;
//1D0 - magicF;
//1C0 - jumps;
//1B8 - crit hit;
//1AC - health regen;
//1B0 - energy regen;
//1A4 - max health;
//1A0 - magicD;
//1A8 - max energy;
//19C - physD;
//238 - light; 
        private static readonly int[] CoordsOffsets = { 0x8, 0x28, 0xC4, 0x4 };
        private static readonly int[] ViewOffsets = { 0x4, 0x24, 0x84, 0x0 };
        private static readonly int[] AccelOffsets = { 0x4, 0x11C, 0xC4, 0x4 };
        private static readonly int[] NameOffests = { 0x8, 0x28, 0x1B4, 0x0 };
	    private static readonly int[] ChatOpenedOffsets = { 0x20, 0x1C };
        private static readonly int[] PowerRankOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x200 };
        private static readonly int[] StatsEncKeyOffsets = { 0x8, 0x28, 0xC4, 0x2D4, 0x21C };

        private readonly int[] _xPosition = CoordsOffsets.Join(0x60);
        private readonly int[] _yPosition = CoordsOffsets.Join(0x64);
        // private readonly int[] ZPosition = CoordsOffsets.Join(0x68);
        private readonly int[] _xVelocity = AccelOffsets.Join(0x90);
        // private readonly int[] YVelocity = AccelOffsets.Join(0x94);
        // private readonly int[] ZVelocity = AccelOffsets.Join(0x98);
        private readonly int[] _xView = ViewOffsets.Join(0x100);
        // private readonly int[] YView = ViewOffsets.Join(0x104);
        // private readonly int[] ZView = ViewOffsets.Join(0x108);
        
        private readonly Dictionary<int[], int> _addresses = new();

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

        private const int MemoryProtection = 0x40;

        private int ReadInt(int[] offsets) => BitConverter.ToInt32(GetBuffer(offsets), 0);
        private uint ReadUInt(int[] offsets) => BitConverter.ToUInt32(GetBuffer(offsets), 0);
        private float ReadFloat(int[] offsets) => BitConverter.ToSingle(GetBuffer(offsets), 0);
        private float ReadFloat(IntPtr address) => BitConverter.ToSingle(GetBuffer(address), 0);
        private string ReadString(int[] offsets) => Encoding.ASCII.GetString(GetBuffer(offsets, 16));

        private void WriteInt(int[] offsets, int value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes(value));
        private void WriteUInt(int[] offsets, uint value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes((int)value));
        private void WriteFloat(int[] offsets, float value) => WriteMemory(GetAddress(offsets), BitConverter.GetBytes(value));
        private void WriteFloat(IntPtr address, float value) => WriteMemory(address, BitConverter.GetBytes(value));

        private void OverwriteBytes(int[] pattern, IReadOnlyList<int> bytes)
        {
            var address = (IntPtr) FindSignature(pattern);
            //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
            if (address == IntPtr.Zero) return;

            var val = new byte[bytes.Count];
            var buffer = new byte[pattern.Length];
            ReadMemory(address, buffer);

            for (var i = 0; i < pattern.Length; i++)
            {
                val[i] = pattern[i] == -1 || bytes[i] == -1 ? 
                    buffer[i] : (byte) bytes[i];
            }

            WriteMemory(address, val);
        }

        private int FindSignature(IReadOnlyList<int> pattern, Process process = null)
        {
            process ??= HookModel.Process;
            var module = process.MainModule;
            if (module == null)
            {
                MessageBox.Show("Module is NULL at 'FindSignature', doesn't it surprise?");
                return 0;
            }
            var startAdd = module.BaseAddress;
            var allocSize = module.ModuleMemorySize;
            var buffer = new byte[allocSize];
            ReadProcessMemory(process.Handle, startAdd, buffer, allocSize, out _);
            var compare = 0;
            var count = pattern.Count;
            
            for (var i = 0; i < buffer.Length; i++)
            {
                if (!(buffer[i] == pattern[0] || pattern[0] == -1)) continue;
                for (int g = 1; g < count; g++)
                {
                    if (buffer[i + g] != pattern[g] && pattern[g] != -1)
                    {
                        compare = 1;
                        break;
                    }

                    compare++;
                    if (compare == count)
                        return i + (int) startAdd;
                }
                // for (var k = 0; k < count; k++)
                // {
                //     if (buffer[i + k] == pattern[k] || pattern[k] == -1)
                //     {
                //         compare++;
                //         if (compare == count)
                //         {
                //             return i + (int) startAdd;
                //         }
                //     }
                //     else
                //     {
                //         compare = 0;
                //     }
                // }
            }

            return 0;
        }
        
        private void ReadMemory(IntPtr address, byte[] buffer) => ReadProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private void WriteMemory(IntPtr address, byte[] buffer) => WriteProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private void ReadMemory(IntPtr handle, IntPtr address, byte[] buffer) => ReadProcessMemory(handle, address, buffer, buffer.Length, out _);
        private void WriteMemory(IntPtr handle, IntPtr address, byte[] buffer) => WriteProcessMemory(handle, address, buffer, buffer.Length, out _);

        private IntPtr GetAddress(IEnumerable<int> offsets)
        {
            var bytes = new byte[4];
            var address = _currentBaseAddress;
            foreach (var offset in offsets)
            {
                ReadMemory(address, bytes);
                address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + offset);
            }
            return address;
        }
        private IntPtr GetAddress(IntPtr handle, IEnumerable<int> offsets)
        {
            var bytes = new byte[4];
            var address = _currentBaseAddress;
            foreach (var offset in offsets)
            {
                ReadProcessMemory(handle, address, bytes, bytes.Length, out _);
                address = (IntPtr)(BitConverter.ToInt32(bytes, 0) + offset);
            }
            return address;
        }

        private byte[] GetBuffer(IEnumerable<int> offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddress(offsets), bytes);
            return bytes;
        }
        private byte[] GetBuffer(IntPtr address)
        {
            var bytes = new byte[4];
            ReadMemory(address, bytes);
            return bytes;
        }
        
        private static byte[] AsmJump(ulong destination, ulong origin, byte[] cave = null)
        {
            var jumpEnd = destination - origin;
            var jumpStart = jumpEnd - 5;
            var dump = jumpStart.ToString("x");

            if (dump.Length == 7)
                dump = "0" + dump;
            
            dump += "E9";

            var hex = new byte[dump.Length / 2];
            for (var i = 0; i < hex.Length; i++)
                hex[i] = Convert.ToByte(dump.Substring(i * 2, 2), 16);

            Array.Reverse(hex);
            if (cave != null)
                hex = cave.Join(hex);
            
            return hex;
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
        public enum Instructions : byte
        {
            Jp = 0xE9,
            Nop = 0x90
        }

        [DllImport("kernel32.dll")]
        static extern bool VirtualFreeEx(IntPtr handle, IntPtr lpAddress,
            int dwSize, AllocationType dwFreeType);
        
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr handle, IntPtr lpAddress,
            UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        
        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, AllocationType allocationType, int flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          byte[] lpBuffer,
          int nSize,
          out IntPtr lpNumberOfBytesWritten);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
        
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}