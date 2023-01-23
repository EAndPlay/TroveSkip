using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TroveSkip.Memory;
using TroveSkip.Memory.Camera;
using TroveSkip.Memory.Player;
using TroveSkip.Memory.Player.Character;
using TroveSkip.Memory.World.Players;
using TroveSkip.Models;

namespace TroveSkip.ViewModels
{
    public sealed partial class MainWindowViewModel
    {
        // baseAddress: [0x8, 0x28] = [0x4, 0x11C]

        // IsOnFeet (get/set) CharacterOffset + [84 + 208] | [9C + 68]

        private const int MinimalDrawDistance = 32;
        private const int MaximalDrawDistance = 210;
        private const int DefaultObjectValue = 150;
        
        private const int MinimalModuleOffset = 16_200_000;
        private const int MaximalModuleOffset = 19_000_000;
        
        private const byte PlayerOffsetInPlayersArray = 2;
        
        private const int MaxStringLength = 64;
        private const byte CaveOffset = 5;
        // private const int PlayersStartOffset = 0xC; // or 0x4
        // private const int NetworkPlayerStructureSize = 0x10;

        private static readonly int[] CharacterPositionX =
        {
            (int) PlayerOffset.Character, 
            (int) CharacterOffset.Controller, 
            (int) ControllerOffset.PositionX
        };

        private static readonly int[] NameOffsets =
        {
            (int) PlayerOffset.Name,
            0x0
        };

        private static readonly int[] LocalPlayerOffsets =
        {
            (int) GameOffset.LocalPlayer, 
            0x28 // TODO: as enum
        };
        private static readonly int[] LocalCharactersOffsets = LocalPlayerOffsets.Join((int) PlayerOffset.Character);
        private static readonly int[] LocalPlayerNameOffsets = LocalPlayerOffsets.Join(NameOffsets);

        private static readonly int[] ViewOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.LocalCamera,
            0x84, // TODO: as enum
            0x0
        }; // float
        
        private static readonly int[] LocalMoveOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Controller);
        private static readonly int[] LocalXPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionX); // float
        private static readonly int[] LocalYPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionY);
        private static readonly int[] LocalXVelocity = LocalMoveOffsets.Join((int) ControllerOffset.VelocityX); // float
        private static readonly int[] GravityOffsets = LocalMoveOffsets.Join((int) ControllerOffset.Gravity); // float

        private static readonly int[] StatsOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Stats);
        private static readonly int[] SpeedOffsets = StatsOffsets.Join((int) StatOffset.MovementSpeed); // encoded float
        private static readonly int[] PowerRankOffsets = StatsOffsets.Join((int) StatOffset.PowerRank); // encoded float
        private static readonly int[] StatsEncryptionKeyOffsets = StatsOffsets.Join((int) StatOffset.EncryptionKey); // encoder (uint)

        private static readonly int[] XView = ViewOffsets.Join(0x100);

        private static readonly int[] PlayersInWorld = { (int) Memory.World.WorldOffset.Players };

        private static readonly int[] PlayersArray = PlayersInWorld.Join((int) PlayersOffset.PlayersArray, 0x0); // local player

        private static readonly int[] PlayersCountInWorldOffsets = PlayersInWorld.Join((int) PlayersOffset.Count);

        private static readonly int[] FirstPlayerXPosition = PlayersArray.Join(CharacterPositionX);

        private static readonly int[] WorldIdOffsets = { (int) Memory.World.WorldOffset.Id };
        
        private static readonly int[] ChatOpenedOffsets =
        {
            0x20, 
            0x1C
        };

        // start offsets belong to visual
        
        // // actual in loading state
        // private static readonly int[] WorldIdUnstableOffsets =
        // {
        //     0xB4, 
        //     0x14, 
        //     0x64, 
        //     0x8
        // };
        
        // stable in world
        private static readonly int[] WorldIdStableOffsets =
        {
            0xBC, 
            0x14, 
            0x34, 
            0x8
        };
        
        private static readonly int[] HalfDrawDistanceOffsets = { (int) SettingOffset.Grama };
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
        
        private readonly int[] _antiDismount = { 0x74, 0x0B, 0x8B, 0x07, 0x8B, 0xCF, 0x6A, 0x00, 0x6A };

        private readonly int[] _antiDismountEnabled = { 0xEB, 0x0B, 0x8B, 0x07, 0x8B, 0xCF, 0x6A, 0x00, 0x6A };

        #endregion

        private unsafe int ReadIntFromLocalPlayer(int[] offsets)
        {
            fixed (byte* p = GetBufferFromLocalPlayer(offsets))
            {
                return *(int*) p;
            }
        }
        
        private unsafe int ReadIntFromLocalPlayer(IntPtr handle, int[] offsets)
        {
            fixed (byte* p = GetBuffer(handle, _currentLocalPlayerPointer, offsets))
            {
                return *(int*) p;
            }
        }

        private unsafe float ReadFloatFromLocalPlayer(int[] offsets)
        {
            // var add = _currentBaseAddress;
            // ReadProcessMemory(_handle, add, &add, 4, out _);
            // foreach (var offset in offsets)
            // {
            //     ReadProcessMemory(_handle, add + offset, &add, 4, out _);
            // }
            //
            // return *(float*) &add;
            fixed (byte* p = GetBufferFromLocalPlayer(offsets))
            {
                return *(float*) p;
            }
        }

        private static int GetForegroundWindowProcessId() => GetProcessIdFromWindowHandle(GetForegroundWindow());
        
        private static int GetProcessIdFromWindowHandle(IntPtr windowHandle)
        {
            GetWindowThreadProcessId(windowHandle, out var processId);
            return processId;
        }

        private static void SendKeyboardKey(IntPtr windowHandle, Key key, bool keyDown)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var scanCode = MapVirtualKey(virtualKey, 0) << 16;
            PostMessage(windowHandle, keyDown ? SystemMessage.KeyboardKeyDown : SystemMessage.KeyboardKeyUp, virtualKey, scanCode | 1);
        }

        private static void SendKeyboardKeyDown(IntPtr windowHandle, Key key) =>
            SendKeyboardKey(windowHandle, key, true);
        private static void SendKeyboardKeyUp(IntPtr windowHandle, Key key) =>
            SendKeyboardKey(windowHandle, key, false);

        private static void SendKeyboardKeyPress(IntPtr windowHandle, Key key)
        {
            SendKeyboardKeyDown(windowHandle, key);
            SendKeyboardKeyUp(windowHandle, key);
        }
        
        private string ReadStringToEnd(int address, Encoding encoding, int maxLength = MaxStringLength) =>
            ReadStringToEnd(_handle, address, encoding, maxLength);
        
        private unsafe string ReadStringToEnd(IntPtr handle, int address, Encoding encoding, int maxLength = MaxStringLength)
        {
                var bytesInChar = encoding.GetMaxByteCount(0);
                var buffer = stackalloc byte[1];
                for (short i = 0; i < maxLength; i++)
                {
                    if (!ReadMemory(handle, address + i * bytesInChar, buffer, 1))
                        break;
                    if (*buffer != 0)
                        continue;

                    return i == 0 ? null : encoding.GetString(GetBuffer(handle, address, (byte) (i * bytesInChar)));
                }

                return null;
        }

        private string ReadString(int address, byte length, Encoding encoding) =>
            ReadString(_handle, address, length, encoding);
        
        private unsafe string ReadString(IntPtr handle, int address, byte length, Encoding encoding)
        {
            try
            {
                var bytesInChar = encoding.GetMaxByteCount(0);
                var buffer = GetBuffer(handle, address, (byte) (length * bytesInChar));
                
                fixed (byte* pointer = buffer)
                {
                    if (*pointer == 0)
                        return null;

                    var bufferLength = buffer.Length + bytesInChar;
                    for (var i = 0; i < bufferLength; i += bytesInChar)
                    {
                        if (*(pointer + i) != 0) continue;
                        if (i == 0)
                            break;
                        
                        return encoding.GetString(pointer, i);
                    }
                
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        
        private void WriteIntToLocalPlayer(int[] offsets, int value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes(value));
        private void WriteUIntToLocalPlayer(int[] offsets, uint value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes((int)value));
        private void WriteFloatToLocalPlayer(int[] offsets, float value) => WriteMemory(GetAddressFromLocalPlayer(offsets), BitConverter.GetBytes(value));
        private void WriteFloatToLocalPlayer(IntPtr handle, int[] offsets, float value) => WriteMemory(handle, GetAddressFromLocalPlayer(handle, offsets), BitConverter.GetBytes(value));
        
        #region Read/Write types
        
        #region Read
        
        private byte ReadByte(int baseAddress, int[] offsets) => 
            ReadByte(GetAddress(baseAddress, offsets));
        private byte ReadByte(int address) => 
            ReadByte(_handle, address);
        private byte ReadByte(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadByte(handle, GetAddress(handle, baseAddress, offsets));
        private byte ReadByte(IntPtr handle, int address) => 
            GetBuffer(handle, address, sizeof(byte))[0];
        
        private bool ReadBool(int baseAddress, int[] offsets) => 
            ReadBool(GetAddress(baseAddress, offsets));
        private bool ReadBool(int address) => 
            ReadBool(_handle, address);
        private bool ReadBool(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadBool(handle, GetAddress(handle, baseAddress, offsets));
        private bool ReadBool(IntPtr handle, int address) => 
            GetBuffer(handle, address, sizeof(byte))[0] == 1;

        private short ReadShort(int baseAddress, int[] offsets) => 
            ReadShort(GetAddress(baseAddress, offsets));
        private short ReadShort(int address) => 
            ReadShort(_handle, address);
        private short ReadShort(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadShort(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe short ReadShort(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address, sizeof(short)))
            {
                return *(short*) pointer;
            }
        }
        
        private ushort ReadUShort(int baseAddress, int[] offsets) => 
            ReadUShort(GetAddress(baseAddress, offsets));
        private ushort ReadUShort(int address) => 
            ReadUShort(_handle, address);
        private ushort ReadUShort(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadUShort(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe ushort ReadUShort(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address, sizeof(ushort)))
            {
                return *(ushort*) pointer;
            }
        }
        
        private int ReadInt(int baseAddress, int[] offsets) => 
            ReadInt(GetAddress(baseAddress, offsets));
        private int ReadInt(int address) => 
            ReadInt(_handle, address);
        private int ReadInt(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadInt(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe int ReadInt(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(int*) pointer;
            }
        }

        private uint ReadUInt(int baseAddress, int[] offsets) => 
            ReadUInt(GetAddress(baseAddress, offsets));
        private uint ReadUInt(int address) => 
            ReadUInt(_handle, address);
        private uint ReadUInt(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadUInt(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe uint ReadUInt(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(uint*) pointer;
            }
        }

        private float ReadFloat(int baseAddress, int[] offsets) => 
            ReadFloat(GetAddress(baseAddress, offsets));
        private float ReadFloat(int address) => 
            ReadFloat(_handle, address);
        private float ReadFloat(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadFloat(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe float ReadFloat(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(float*) pointer;
            }
        }

        private double ReadDouble(int baseAddress, int[] offsets) => 
            ReadDouble(GetAddress(baseAddress, offsets));
        private double ReadDouble(int address) => 
            ReadDouble(_handle, address);
        private double ReadDouble(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadDouble(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe double ReadDouble(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(double*) pointer;
            }
        }
        
        private long ReadLong(int baseAddress, int[] offsets) => 
            ReadLong(GetAddress(baseAddress, offsets));
        private long ReadLong(int address) => 
            ReadLong(_handle, address);
        private long ReadLong(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadLong(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe long ReadLong(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(long*) pointer;
            }
        }
        
        private ulong ReadULong(int baseAddress, int[] offsets) => 
            ReadULong(GetAddress(baseAddress, offsets));
        private ulong ReadULong(int address) => 
            ReadULong(_handle, address);
        private ulong ReadULong(IntPtr handle, int baseAddress, int[] offsets) =>
            ReadULong(handle, GetAddress(handle, baseAddress, offsets));
        private unsafe ulong ReadULong(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(ulong*) pointer;
            }
        }

        private T ReadStructure<T>(int address, byte size = 0) => 
            ReadStructure<T>(_handle, address, size);
        private T ReadStructure<T>(int baseAddress, int[] offsets, byte size = 0) => 
            ReadStructure<T>(_handle, baseAddress, offsets, size);
        private T ReadStructure<T>(IntPtr handle, int baseAddress, int[] offsets, byte size = 0) =>
            ReadStructure<T>(handle, GetAddress(handle, baseAddress, offsets), size);
        private unsafe T ReadStructure<T>(IntPtr handle, int address, byte size = 0)
        {
            fixed (byte* pointer = GetBuffer(handle, address, size == 0 ? (byte) Marshal.SizeOf(default(T)) : size))
            {
                return Marshal.PtrToStructure<T>((IntPtr) pointer);
            }
        }

        #endregion
        
        #region Write
        
        private void WriteByte(int baseAddress, int[] offsets, byte value) =>
            WriteByte(GetAddress(baseAddress, offsets), value);
        private void WriteByte(int address, byte value) =>
            WriteByte(_handle, address, value);
        private void WriteByte(IntPtr handle, int baseAddress, int[] offsets, byte value) =>
            WriteByte(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteByte(IntPtr handle, int address, byte value) =>
            WriteMemory(handle, address, new[] { value });

        // TODO: convert Byte <-> SByte
        // private void WriteSByte(int baseAddress, int[] offsets, sbyte value) =>
        //     WriteSByte(GetAddress(baseAddress, offsets), value);
        // private void WriteSByte(int address, sbyte value) =>
        //     WriteSByte(_handle, address, value);
        // private void WriteSByte(IntPtr handle, int baseAddress, int[] offsets, sbyte value) =>
        //     WriteSByte(handle, GetAddress(handle, baseAddress, offsets), value);
        // private void WriteSByte(IntPtr handle, int address, sbyte value) =>
        //     WriteMemory(handle, address, Convert.ToSByte((byte)value));
        
        private void WriteShort(int baseAddress, int[] offsets, short value) =>
            WriteShort(GetAddress(baseAddress, offsets), value);
        private void WriteShort(int address, short value) =>
            WriteShort(_handle, address, value);
        private void WriteShort(IntPtr handle, int baseAddress, int[] offsets, short value) =>
            WriteShort(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteShort(IntPtr handle, int address, short value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteUShort(int baseAddress, int[] offsets, ushort value) =>
            WriteUShort(GetAddress(baseAddress, offsets), value);
        private void WriteUShort(int address, ushort value) =>
            WriteUShort(_handle, address, value);
        private void WriteUShort(IntPtr handle, int baseAddress, int[] offsets, ushort value) =>
            WriteUShort(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteUShort(IntPtr handle, int address, ushort value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteInt(int baseAddress, int[] offsets, int value) =>
            WriteInt(GetAddress(baseAddress, offsets), value);
        private void WriteInt(int address, int value) =>
            WriteInt(_handle, address, value);
        private void WriteInt(IntPtr handle, int baseAddress, int[] offsets, int value) =>
            WriteInt(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteInt(IntPtr handle, int address, int value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteUInt(int baseAddress, int[] offsets, uint value) =>
            WriteUInt(GetAddress(baseAddress, offsets), value);
        private void WriteUInt(int address, uint value) =>
            WriteUInt(_handle, address, value);
        private void WriteUInt(IntPtr handle, int baseAddress, int[] offsets, uint value) =>
            WriteUInt(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteUInt(IntPtr handle, int address, uint value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteFloat(int baseAddress, int[] offsets, float value) =>
            WriteFloat(GetAddress(baseAddress, offsets), value);
        private void WriteFloat(int address, float value) =>
            WriteFloat(_handle, address, value);
        private void WriteFloat(IntPtr handle, int baseAddress, int[] offsets, float value) =>
            WriteFloat(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteFloat(IntPtr handle, int address, float value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteDouble(int baseAddress, int[] offsets, double value) =>
            WriteDouble(GetAddress(baseAddress, offsets), value);
        private void WriteDouble(int address, double value) =>
            WriteDouble(_handle, address, value);
        private void WriteDouble(IntPtr handle, int baseAddress, int[] offsets, double value) =>
            WriteDouble(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteDouble(IntPtr handle, int address, double value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteLong(int baseAddress, int[] offsets, long value) =>
            WriteLong(GetAddress(baseAddress, offsets), value);
        private void WriteLong(int address, long value) =>
            WriteLong(_handle, address, value);
        private void WriteLong(IntPtr handle, int baseAddress, int[] offsets, long value) =>
            WriteLong(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteLong(IntPtr handle, int address, long value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        private void WriteULong(int baseAddress, int[] offsets, ulong value) =>
            WriteULong(GetAddress(baseAddress, offsets), value);
        private void WriteULong(int address, ulong value) =>
            WriteULong(_handle, address, value);
        private void WriteULong(IntPtr handle, int baseAddress, int[] offsets, ulong value) =>
            WriteULong(handle, GetAddress(handle, baseAddress, offsets), value);
        private void WriteULong(IntPtr handle, int address, ulong value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));
        
        #endregion
        
        #endregion

        private void OverwriteBytes(int[] pattern, int[] signature) =>
            OverwriteBytes(_hookModel, pattern, signature);
        // {
        //     var address = FindSignature(pattern, _hookModel);
        //     //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
        //     if (address == 0) return;
        //
        //     var val = new byte[signature.Length];
        //     var buffer = new byte[pattern.Length];
        //     ReadMemory(address, buffer);
        //
        //     fixed (byte* newCode = val)
        //     {
        //         fixed (int* code = signature, offset = pattern)
        //         {
        //             for (var i = 0; i < pattern.Length; i++)
        //             {
        //                 *(newCode + i) = *(offset + i) == -1 || *(code + i) == -1 ? buffer[i] : (byte) *(code + i);
        //             }
        //         }
        //     }
        //
        //     WriteMemory(address, val);
        // }
        
        private unsafe void OverwriteBytes(HookModel hook, int[] pattern, int[] signature)
        {
            var address = FindSignature(pattern, hook);
            //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
            if (address == 0) return;

            var handle = hook.Handle;
            var val = new byte[signature.Length];
            var buffer = new byte[pattern.Length];
            ReadMemory(handle, address, buffer);

            fixed (byte* newCode = val)
            {
                fixed (int* code = signature, offset = pattern)
                {
                    for (var i = 0; i < pattern.Length; i++)
                    {
                        *(newCode + i) = *(offset + i) == -1 || *(code + i) == -1 ? buffer[i] : (byte) *(code + i);
                    }
                }
            }

            WriteMemory(handle, address, val);
        }

        private void OverwriteBytes(IntPtr handle, int address, int[] signature)
        {
            var length = signature.Length;
            var buffer = new byte[length];
            ReadMemory(handle, address, buffer);
            
            for (var i = 0; i < length; i++)
            {
                if (signature[i] != -1)
                    buffer[i] = (byte) signature[i];
            }

            WriteMemory(handle, address, buffer);
        }

        private unsafe int FindSignature(int[] pattern, HookModel hook = null)
        {
            hook ??= HookModel;
            var module = hook.Module;
            
            var moduleAddress = (int) module.BaseAddress;
            var moduleSize = module.ModuleMemorySize;
            var buffer = new byte[moduleSize];
            ReadMemory(hook.Handle, moduleAddress, buffer);
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
        
        private byte[] GetBufferFromLocalPlayer(IntPtr handle, int[] offsets, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddressFromLocalPlayer(handle, offsets), bytes);
            return bytes;
        }

        private byte[] GetBufferFromLocalPlayer(int[] offsets, byte size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(GetAddressFromLocalPlayer(offsets), bytes);
            return bytes;
        }
        
        private bool ReadMemory(int address, byte[] buffer) => ReadProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private bool ReadMemory(IntPtr handle, int address, byte[] buffer) => ReadProcessMemory(handle, address, buffer, buffer.Length, out _);
        private unsafe bool ReadMemory(int address, byte* buffer, int size = 4) => ReadProcessMemory(_handle, address, buffer, size, out _);
        private unsafe bool ReadMemory(IntPtr handle, int address, byte* buffer, int size = 4) => ReadProcessMemory(handle, address, buffer, size, out _);
        
        private void WriteMemory(int address, byte[] buffer) => WriteProcessMemory(_handle, address, buffer, buffer.Length, out _);
        private void WriteMemory(IntPtr handle, int address, byte[] buffer) => WriteProcessMemory(handle, address, buffer, buffer.Length, out _);
        
        // TODO: remove
        private int GetAddressFromLocalPlayer(int[] offsets) => GetAddress(_currentLocalPlayerPointer, offsets);
        private int GetAddressFromLocalPlayer(IntPtr handle, int[] offsets) => GetAddress(handle, _currentLocalPlayerPointer, offsets);
        
        private byte[] GetBuffer(int baseAddress, int[] offsets, byte size = 4) => 
            GetBuffer(GetAddress(baseAddress, offsets), size);
        private byte[] GetBuffer(int address, byte size = 4) => 
            GetBuffer(_handle, address, size);
        private byte[] GetBuffer(IntPtr handle, int baseAddress, int[] offsets, byte size = 4) => 
            GetBuffer(handle, GetAddress(handle, baseAddress, offsets), size);
        private byte[] GetBuffer(IntPtr handle, int address, byte size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(handle, address, bytes);
            return bytes;
            //TODO: make it nullable, bc yes
            //return ReadMemory(handle, address, bytes) ? bytes : null;
        }

        private int GetAddress(int baseAddress, int[] offsets) => GetAddress(_handle, baseAddress, offsets);
        
        private unsafe int GetAddress(IntPtr handle, int baseAddress, int[] offsets)
        {
            var bytes = stackalloc byte[4];
            var num = (int*) bytes;

            foreach (var offset in offsets)
            {
                if (!ReadMemory(handle, baseAddress, bytes))
                    return 0;
                baseAddress = *num + offset;
            }
            return baseAddress;
        }

        private static byte[] AsmJump(ulong destination, ulong origin, byte[] cave = null)
        {
            var jumpStart = destination - origin - CaveOffset;
            var bytes = new byte[4];
            var address = BitConverter.GetBytes(jumpStart);
            var addressLength = address.Length;
            if (addressLength > bytes.Length)
                Array.Resize(ref bytes, addressLength + 1);
            Array.Copy(address, 0, bytes, 1, addressLength - 1);
            bytes[0] = 0xE9; // jmp
            int newLength;
            for (newLength = bytes.Length - 1; newLength > 0; newLength--)
            {
                if (bytes[newLength] != 0)
                    break;
            }
            Array.Resize(ref bytes, newLength + 1);

            if (cave != null)
                bytes = cave.Join(bytes);
            
            return bytes;
        }
        
        #region Dlls
        
        #region Kernel
        
        [Flags]
        private enum ProcessAccessFlags : uint
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
        private enum PositionFlags
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
        private enum AllocationType
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
        private enum MemoryProtection
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
        private static extern bool VirtualFreeEx(
            IntPtr handle,
            int address,
            int size,
            AllocationType allocationType);
        
        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(
            IntPtr handle,
            int address,
            int size,
            MemoryProtection newProtection,
            out MemoryProtection oldProtection);
        
        [DllImport("kernel32.dll")]
        private static extern int VirtualAllocEx(
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

        #endregion

        #region User32
        
        private enum SystemMessage : uint
        {
            WindowMove = 0x3, // low - x, high - y
            WindowClose = 0x10,
            //WindowQuit = 0x12, // low - exit code | useless message anyway
            WindowShow = 0x18,
            SetCursor = 0x20, // low - position, high - event which triggered (ingoing)
            KeyboardKeyDown = 0x100,
            KeyboardKeyUp = 0x101,
            MouseMove = 0x200,
            MouseLeftButtonDown = 0x201,
            MouseLeftButtonUp = 0x202,
            MouseLeftButtonDoubleClick = 0x203,
            MouseRightButtonDown = 0x204,
            MouseRightButtonUp = 0x205,
            MouseRightButtonDoubleClick = 0x206,
            MouseMiddleButtonDown = 0x207,
            MouseMiddleButtonUp = 0x208,
            MouseMiddleButtonDoubleClick = 0x209,
            MouseNonClientMove = 0xA0,
            MouseNonClientLeftButtonDown = 0xA1,
            MouseNonClientLeftButtonUp = 0xA2,
            MouseNonClientLeftButtonDoubleClick = 0xA3,
            MouseNonClientRightButtonDown = 0xA4,
            MouseNonClientRightButtonUp = 0xA5,
            MouseNonClientRightButtonDoubleClick = 0xA6,
            MouseNonClientMiddleButtonDown = 0xA7,
            MouseNonClientMiddleButtonUp = 0xA8,
            MouseNonClientMiddleButtonDoubleClick = 0xA9,
            MouseWheel = 0x20E,
            MouseCaptureChanged = 0x215,
            MouseNonClientWindowHover = 0x2A0,
            MouseWindowHover = 0x2A1,
            MouseNonClientWindowLeave = 0x2A2,
            MouseWindowLeave = 0x2A3,
            ClipboardClear = 0x303,
            ClipboardCopy = 0x301,
            ClipboardCut = 0x300, // wtf it does
            ClipboardPaste = 0x302,
            ClipboardUpdate = 0x31D
        }

        private enum ClipboardFormat : uint
        {
            Text = 1, // ANSI
            BitMap = 2,
            OemText = 7,
            UnicodeText = 13, 
            DspText = 0x81
        }
        
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(
            IntPtr handle,
            out int processId);

        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(
            int virtualKey, 
            int mapType);
        
        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(
            IntPtr windowHandle,
            SystemMessage message,
            int wParam,
            int lParam);

        [DllImport("user32.dll")]
        private static extern int GetMessage(
            out MSG message, 
            IntPtr windowHandle, 
            uint wMsgFilterMin, 
            uint wMsgFilterMax);
        
        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG message);
        
        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG message);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(ClipboardFormat format);

        [DllImport("user32.dll", ExactSpelling=true)]
        private static extern IntPtr SetTimer(IntPtr windowHandle, IntPtr nIDEvent, uint uElapse, TimerProc lpTimerFunc);
        private delegate void TimerProc(IntPtr windowHandle, uint uMsg, IntPtr nIDEvent, uint dwTime);
        
        #endregion
        #endregion
    }
}