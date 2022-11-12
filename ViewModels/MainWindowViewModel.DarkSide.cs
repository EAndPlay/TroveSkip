using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using TroveSkip.Memory;
using TroveSkip.Memory.Camera;
using TroveSkip.Models;

namespace TroveSkip.ViewModels
{
    public sealed partial class MainWindowViewModel
    {
        //baseAddress + (0xCFC | 0xD1C) = Players amount in current world (not actual)
        //baseAddress: [0x8, 0x28] = [0x4, 0x11C]

        //On feet (sets) CharacterOffset + [84 + 208] | [9C + 68]

        private const int PlayersStartOffset = 0xC; // or 0x4
        private const int NetworkPlayerStructureSize = 0x10;

        private static readonly int[] CharacterPositionX =
            { (int) PlayerOffset.Character, (int) CharacterOffset.Controller, (int) ControllerOffset.PositionX };
        private static readonly int[] NameOffsets = { (int) PlayerOffset.Name, 0x0 };

        private static readonly int[] LocalPlayerOffsets = { 0x8, 0x28 };
        private static readonly int[] LocalCharactersOffsets = LocalPlayerOffsets.Join((int) PlayerOffset.Character);
        private static readonly int[] ViewOffsets = { (int) GameOffset.Camera, (int) CameraOffset.LocalCamera, 0x84, 0x0 }; // float
        private static readonly int[] LocalPlayerNameOffsets = LocalPlayerOffsets.Join(NameOffsets);
        
        private static readonly int[] LocalMoveOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Controller);
        private static readonly int[] LocalXPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionX); // float
        private static readonly int[] LocalYPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionY);
        private static readonly int[] LocalXVelocity = LocalMoveOffsets.Join((int) ControllerOffset.VelocityX); // float
        private static readonly int[] GravityOffsets = LocalMoveOffsets.Join((int) ControllerOffset.Gravity); // float

        private static readonly int[] StatsOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Stats);
        private static readonly int[] SpeedOffsets = StatsOffsets.Join((int) StatOffset.MovementSpeed); // encoded float
        private static readonly int[] PowerRankOffsets = StatsOffsets.Join((int) StatOffset.PowerRank); // encoded float
        private static readonly int[] StatsEncKeyOffsets = StatsOffsets.Join((int) StatOffset.EncryptionKey); // encoder (uint)

        private static readonly int[] XView = ViewOffsets.Join(0x100);

        private static readonly int[] PlayerInWorld = { 0x7C, -1, 0x30 };
        
        private static readonly int[] ChatOpenedOffsets = { 0x20, 0x1C };

        // private static readonly int[] WorldIdUnstableOffsets = { 0xB4, 0x14, 0x64, 0x8 };
        private static readonly int[] WorldIdStableOffsets = { 0xBC, 0x14, 0x34, 0x8 }; // stable in world
        
        private static readonly int[] HalfDrawDistance = { (int) SettingOffset.Grama };
        private static readonly int[] IdkObject = { (int) SettingOffset.ObjectsDrawDistance };
        private static readonly int[] DrawDistance = { (int) SettingOffset.DrawDistance };

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
        
	    private readonly int[] _miningSlow = { 0xDF, 0xF1, 0xDD, 0xD8, 0x72, 0x61 };
        
	    private readonly int[] _miningSlowEnabled = { 0xDF, 0xF0, 0xDD, 0xD8, 0x72, 0x61 };

        // private readonly int[] _miningFast = { 0x77, 0x02, 0x8B, 0xC7, 0xDD, 0x00, 0xDD, 0x17, 0xDF, 0xF1, 0xDD, 0xD8, 0x72, 0x61 };
        
        // private readonly int[] _miningFastEnabled = { 0x72, 0x02, 0x8B, 0xC7, 0xDD, 0x00, 0xDD, 0x17, 0xDF, 0xF1, 0xDD, 0xD8, 0x72, 0x61 };

        private readonly int[] _chamsMonsters = { 0x0F, 0x29, 0x07, 0x8B, 0xC7, 0x5F, 0x5E, 0x8B, 0xE5, 0x5D, 0x8B };

        private readonly int[] _chamsMonstersEnabled = { 0x0F, 0x29, 0x0F, 0x8B, 0xC7, 0x5F, 0x5E, 0x8B, 0xE5, 0x5D, 0x8B };

        private readonly int[] _noClip = { 0x0F, 0x84, 0xF0, 0x05, 0x00, 0x00, 0x0F, 0x28, 0x45, 0xE0, 0x0F, 0x28, 0x65, 0xD0 };
        
        private readonly int[] _noClipEnabled = { 0x0F, 0x87, 0xF0, 0x05, 0x00, 0x00, 0x0F, 0x28, 0x45, 0xE0, 0x0F, 0x28, 0x65, 0xD0 };
        
        #endregion

        private unsafe int ReadInt(int[] offsets)
        {
            fixed (byte* p = GetBuffer(offsets))
            {
                return *(int*) p;
            }
        }
        
        private unsafe int ReadInt(IntPtr handle, int[] offsets)
        {
            fixed (byte* p = GetBuffer(handle, offsets))
            {
                return *(int*) p;
            }
        }
        
        
        private unsafe int ReadInt(IntPtr handle, int baseAddress, int[] offsets)
        {
            fixed (byte* p = GetBuffer(handle, baseAddress, offsets))
            {
                return *(int*) p;
            }
        }
        
        private unsafe int ReadInt(int address)
        {
            fixed (byte* p = GetBuffer(address))
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
            // var add = _currentBaseAddress;
            // ReadProcessMemory(_handle, add, &add, 4, out _);
            // foreach (var offset in offsets)
            // {
            //     ReadProcessMemory(_handle, add + offset, &add, 4, out _);
            // }
            //
            // return *(float*) &add;
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
        
        private unsafe float ReadFloat(IntPtr handle, int address)
        {
            var buffer = stackalloc byte[4];
            ReadMemory(handle, address, buffer);
            return *(float*) buffer;
        }

        private string ReadString(int[] offsets) => Encoding.ASCII.GetString(GetBuffer(offsets, 28));

        private void WriteInt(int[] offsets, int value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes(value));
        private void WriteUInt(int[] offsets, uint value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes((int)value));
        private void WriteFloat(int[] offsets, float value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes(value));
        private void WriteFloat(IntPtr handle, int[] offsets, float value) => WriteMemory(handle, GetAddressFromLocalPlayer(handle, offsets), BitConverter.GetBytes(value));
        private void WriteFloat(int address, float value) => WriteMemory(address, BitConverter.GetBytes(value));
        private void WriteFloat(IntPtr handle, int address, float value) => WriteMemory(handle, address, BitConverter.GetBytes(value));
        
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
        
        private unsafe void OverwriteBytes(HookModel hook, int[] pattern, int[] bytes)
        {
            var address = FindSignature(pattern, hook.Process);
            //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
            if (address == 0) return;

            var handle = hook.Handle;
            var val = new byte[bytes.Length];
            var buffer = new byte[pattern.Length];
            ReadMemory(handle, address, buffer);

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

            WriteMemory(handle, address, val);
        }

        private void OverwriteBytes(IntPtr handle, int address, int[] bytes)
        {
            var length = bytes.Length;
            var buffer = new byte[length];
            ReadMemory(handle, address, buffer);
            
            for (var i = 0; i < length; i++)
            {
                if (bytes[i] != -1)
                    buffer[i] = (byte) bytes[i];
            }

            WriteMemory(handle, address, buffer);
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
            
            var moduleAddress = (int) module.BaseAddress;
            var moduleSize = module.ModuleMemorySize;
            var buffer = new byte[moduleSize];
            ReadMemory(process.Handle, moduleAddress, buffer);
            //ReadProcessMemory(process.Handle, moduleAddress, buffer, moduleSize, out _);
            
            var compare = 0;
            var count = pattern.Length;

            fixed (byte* code = buffer)
            {
                fixed (int* offset = pattern)
                {
                    var firstCode = *offset;
                    var isFirstEmpty = firstCode == -1;
                    for (var i = 0; i < moduleSize; i++)
                    {
                        if (*(code + i) != firstCode && !isFirstEmpty) continue;
                        for (byte g = 1; g < count; g++)
                        {
                            if (*(code + i + g) != *(offset + g) && *(offset + g) != -1)
                            {
                                compare = 1;
                                break;
                            }

                            compare++;
                            if (compare == count)
                                return i + moduleAddress;
                        }
                    }
                }
            }

            return 0;
        }
        
        private bool ReadMemory(int address, byte[] buffer) => ReadProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private unsafe bool ReadMemory(int address, byte* buffer) => ReadProcessMemory(_handle, address, buffer, 4, out _);
        private void WriteMemory(int address, byte[] buffer) => WriteProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private bool ReadMemory(IntPtr handle, int address, byte[] buffer) => ReadProcessMemory(handle, address, buffer, buffer.Length, out _);
        private unsafe bool ReadMemory(IntPtr handle, int address, byte* buffer) => ReadProcessMemory(handle, address, buffer, 4, out _);
        private void WriteMemory(IntPtr handle, int address, byte[] buffer) => WriteProcessMemory(handle, address, buffer, buffer.Length, out _);

        private int GetAddressFromLocalPlayer(int[] offsets)
        {
            return GetAddress(_currentPlayerAddress, offsets);
        }
        
        private int GetAddressFromLocalPlayer(IntPtr handle, int[] offsets)
        {
            return GetAddress(handle, _currentPlayerAddress, offsets);
        }

        private unsafe int GetAddress(IntPtr handle, int baseAddress, int[] offsets)
        {
            var bytes = stackalloc byte[4];
            var num = (int*) bytes;

            foreach (var offset in offsets)
            {
                ReadMemory(handle, baseAddress, bytes);
                baseAddress = *num + offset;
            }
            return baseAddress;
        }

        private int GetAddress(int baseAddress, int[] offsets) => GetAddress(_handle, baseAddress, offsets);

        private byte[] GetBuffer(IntPtr handle, int baseAddress, int[] offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddress(handle, baseAddress, offsets), bytes);
            return bytes;
        }
        
        private byte[] GetBuffer(IntPtr handle, int[] offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddressFromLocalPlayer(handle, offsets), bytes);
            return bytes;
        }
        
        private byte[] GetBuffer(int[] offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddressFromLocalPlayer(offsets), bytes);
            return bytes;
        }
        
        private byte[] GetBuffer(int address)
        {
            var bytes = new byte[4];
            ReadMemory(address, bytes);
            return bytes;
        }
        
        private byte[] GetBuffer(IntPtr handle, int address)
        {
            var bytes = new byte[4];
            ReadMemory(handle, address, bytes);
            return bytes;
        }
        
        private static byte[] AsmJump(ulong destination, ulong origin, byte[] cave = null)
        {
            var jumpStart = destination - origin - 5;
            var bytes = new byte[4];
            var address = BitConverter.GetBytes(jumpStart);
            var addressLength = address.Length;
            if (addressLength > bytes.Length)
                Array.Resize(ref bytes, addressLength + 1);
            Array.Copy(address, 0, bytes, 1, addressLength - 1);
            bytes[0] = 0xE9; // jmp instruction
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
            int address,
            int size,
            AllocationType allocationType);
        
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(
            IntPtr handle,
            int address,
            int size,
            MemoryProtection newProtection,
            out MemoryProtection oldProtection);
        
        [DllImport("kernel32.dll")]
        static extern int VirtualAllocEx(
            IntPtr handle,
            int address,
            int size,
            AllocationType allocationType,
            MemoryProtection protection);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr handle, 
            int address, 
            byte[] buffer, 
            int size, 
            out IntPtr numberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        private static extern unsafe bool ReadProcessMemory(
            IntPtr handle, 
            int address, 
            byte* buffer, 
            int size, 
            out IntPtr numberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(
          IntPtr handle,
          int address,
          byte[] buffer,
          int size,
          out IntPtr numberOfBytesWritten);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(
            IntPtr handle,
            out int processId);
        
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(
            IntPtr handle,
            uint message,
            IntPtr wParam,
            IntPtr lParam);

        #endregion
    }
}