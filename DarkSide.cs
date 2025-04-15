using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TroveSkip.Memory;
using TroveSkip.Memory.Camera;
using TroveSkip.Memory.Player;
using TroveSkip.Memory.Player.Character;
using TroveSkip.Memory.World.Players;
using TroveSkip.Models;

namespace TroveSkip
{
    public static class DarkSide
    {
        // baseAddress: [0x8, 0x28] = [0x4, 0x11C]

        // IsOnFeet (get/set) CharacterOffset + [84 + 208] | [9C + 68]

        // public static int[] _autoLootSignature =
        // {
        //     0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF0, 0xD9, 0xEE, 0x83, 0xEC, 0x38, 0x56, 0x57, 0x8B, 0xF9, 0xDD, 0x87, 0x78, 0x03, 00, 00, 0xDF, 0xF1, 0xDD, 0xD8, 0x76, 0x45
        // };
        //
        // public static int[] _autoLootEnabledSignature =
        // {
        //     0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF0, 0xD9, 0xEE, 0x83, 0xEC, 0x38, 0x56, 0x57, 0x8B, 0xF9, 0xDD, 0x87, 0x78, 0x03, 00, 00, 0xDF, 0xF1, 0xDD, 0xD8, 0x76, 0x27
        // };

        public static Input MouseInputSettings { get; private set; }
        
        public static int[] func_sign =
        {
            0x55, 0x8b, 0xec, 0x83, 0xe4, 0xf0, 0xd9, 0xee, 0x83, 0x83, 0xec, 0x38, 0x56, 0x57, 0x8b, 0xf9, 0xdd, 0x87,
            0x78,
            0x03, 00, 00, 0xdf, 0xf1, 0xdd, 0xd8, 0x76
        };

        public static int[] StatsEncryptionKeySignature =
        {
            0x55, 0x8B, 0xEC, 0x51, 0x8B, 0x01, 0x35, -1, -1, -1, -1, 0x89, 0x45, 0xFC, 0xD9, 0x45, 0xFC, 0x8B, 0xE5,
            0x5D
        };
        
        public static int[] PlayerPointerSignature =
        {
            0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0x83, 0xEC, 0x08, 0xF3, 0x0F, 0x2A, 0x45, 0x10, 0x56, 0x8B, 0xF1,
            0x57, 0x8B, 0x3D
        };

        public static int[] WorldPointerSignature =
        {
            0x55, 0x8B, 0xEC, 0x83, 0x7D, 0x08, 0x04, 0x75, 0x10, 0xA1, -1, -1, -1, -1, 0x85, 0xC0, 0x74, 0x07,
            0xC6, 0x80, 0x59, 0x01, 0x00, 0x00, 0x01, 0x5D, 0xC2, 0x04, 0x00
        };

        public static int[] SettingsPointerSignature =
        {
            0x89, 0x45, 0xF4, 0x8B, 0x11, 0xFF, 0x52, 0x0C, 0x8B, 0x0D, -1, -1, -1, -1, 0x8B, 0xD8,
            0x6A, 0x03, 0x68, -1, -1, -1, -1, 0x8B, 0x11, 0xFF, 0x52, 0x0C, 0x8B, 0x0D
        };

        public static int[] ChatStateOffsetSignature =
        {
            0x8B, 0x0D, -1, -1, -1, -1, 0x6A, 0x00, 0x6A, 0x01, 0xC6, 0x41, 0x20, 0x00, 0xE8, -1,
            -1, -1, -1, 0x6A, 0x08, 0x8D, 0x8D, 0x60, 0xFF, 0xFF, 0xFF, 0xC7, 0x85, 0x60, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0xC7, 0x85, 0x64, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xC7, 0x85, 0x68, 0xFF,
            0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xC6, 0x45, 0xF0, 0x01
        };

        public static int[] MarketMasteryCheckSignature =
        {
            0x8B, 0xD6, 0x8D, 0x86, 0x40, 0x02, 0x00, 0x00, 0xF7, 0xDA, 0x1B, 0xD2, 0x23, 0xD0, 0x52, 0xE8, -1, -1, -1,
            -1, 0x8B, 0xCE, 0xE8, -1, -1, -1, -1, 0xE8, -1, -1, -1, -1, 0xFF, 0x35, -1, -1, -1, -1, 0x8B, 0xC8, 0xE8,
            -1, -1, -1, -1, 0x8B, 0xF8, 0x8B, 0xCF, 0xE8, -1, -1, -1, -1, 0x84, 0xC0, 0x0F, 0x84, 0xAD, 0x03, 0, 0
        };

        public const int MinimalDrawDistance = 31;
        public const int MaximalDrawDistance = 211;
        public const int MaxGrama = 96;
        public const int DefaultObjectsDistance = 150;
        public const int NoGraphicsValue = 0;

        public const int MinimalModuleOffset = 16_200_000;
        public const int MaximalModuleOffset = 19_000_000;

        public const byte PlayerOffsetInPlayersArray = 2;

        public const int MaxStringLength = 64;

        public const byte CaveOffset = 5;
        // public const int PlayersStartOffset = 0xC; // or 0x4
        // public const int NetworkPlayerStructureSize = 0x10;

        public static readonly int[] CharacterPositionX =
        {
            (int) PlayerOffset.Character,
            (int) CharacterOffset.Controller,
            (int) ControllerOffset.PositionX
        };

        public static readonly int[] NameOffsets =
        {
            (int) PlayerOffset.Name,
            0x0
        };

        public static readonly int[] LocalPlayerOffsets =
        {
            (int) GameOffset.LocalPlayer,
            0x28 // TODO: as enum
        };

        public static readonly int[] LocalCharactersOffsets = LocalPlayerOffsets.Join((int) PlayerOffset.Character);
        public static readonly int[] LocalPlayerNameOffsets = LocalPlayerOffsets.Join(NameOffsets);

        public static readonly int[] ViewOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.LocalCamera,
            0x84, // TODO: as enum
            0x0
        }; // float

        public static readonly int[] CameraVerticalRotationOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.VerticalMove
        };
        
        public static readonly int[] CameraHorizontalRotationOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.HorizontalMove
        };

        public static readonly int[] CharacterIdOffsets = LocalCharactersOffsets.Join(0xfc, 0x334);
        public static readonly int[] CurrentHealthOffsets = LocalCharactersOffsets.Join(0xC, 0x38, 0x10, 0x138);

        public static readonly int[] LocalMoveOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Controller);
        public static readonly int[] LocalXPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionX); // float
        public static readonly int[] LocalYPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionY);
        public static readonly int[] LocalXVelocity = LocalMoveOffsets.Join((int) ControllerOffset.VelocityX); // float
        public static readonly int[] GravityOffsets = LocalMoveOffsets.Join((int) ControllerOffset.Gravity); // float

        public static readonly int[] StatsOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Stats);
        public static readonly int[] SpeedOffsets = StatsOffsets.Join((int) StatOffset.MovementSpeed); // encoded float
        public static readonly int[] PowerRankOffsets = StatsOffsets.Join((int) StatOffset.PowerRank); // encoded float
        public static readonly int[] MaxHealthStatOffsets = StatsOffsets.Join((int) StatOffset.MaximumHealth); // encoded float

        public static readonly int[]
            StatsEncryptionKeyOffsets = StatsOffsets.Join((int) StatOffset.EncryptionKey); // encoder (uint)

        public static readonly int[] XView = ViewOffsets.Join(0x100);

        public static readonly int[] PlayersInWorld = {(int) Memory.World.WorldOffset.Players};

        public static readonly int[]
            PlayersArray = PlayersInWorld.Join((int) PlayersOffset.PlayersArray, 0x0); // local player

        public static readonly int[] PlayersCountInWorldOffsets = PlayersInWorld.Join((int) PlayersOffset.Count);

        public static readonly int[] FirstPlayerXPosition = PlayersArray.Join(CharacterPositionX);

        public static readonly int[] WorldIdOffsets = {(int) Memory.World.WorldOffset.Id};

        public static readonly int[] ChatOpenedOffsets =
        {
            0x20,
            0x1C
        };

        // public static readonly Dictionary<Instructions, ASMInstruction[]> _instructions = new()
        // {
        //     {
        //         Instructions.Push, new[0]
        //         {
        //             new ASMInstruction(InstructionsEnum.PushEax)
        //         }
        //     }
        // };

        // start offsets belong to visual

        // // actual in loading state
        // public static readonly int[] WorldIdUnstableOffsets =
        // {
        //     0xB4, 
        //     0x14, 
        //     0x64, 
        //     0x8
        // };

        // stable in world
        public static readonly int[] WorldIdStableOffsets =
        {
            0xBC,
            0x14,
            0x34,
            0x8
        };

        public static readonly int[] HalfDrawDistanceOffsets = {(int) SettingOffset.Grama};
        public static readonly int[] IdkObject = {(int) SettingOffset.ObjectsDrawDistance};
        public static readonly int[] DrawDistanceOffsets = {(int) SettingOffset.DrawDistance};

        //public readonly int[] MaxCamDist = { 0x4, 0x3C };
        //public readonly int[] MinCamDist = { 0x4, 0x38 };

        static DarkSide()
        {
            var mouseInputSettings = new Input()
            {
                type = InputType.Mouse
            };
            mouseInputSettings.MouseInput = new MouseInput();
            mouseInputSettings.MouseInput.dx =
                mouseInputSettings.MouseInput.dy =
                    mouseInputSettings.MouseInput.mouseData = 0;
            mouseInputSettings.MouseInput.time = 0;
            mouseInputSettings.MouseInput.dwFlags = MouseEventFlags.ABSOLUTE | MouseEventFlags.MOVE |
                                                    MouseEventFlags.LEFTDOWN |
                                                    MouseEventFlags.LEFTUP |
                                                    MouseEventFlags.RIGHTDOWN |
                                                    MouseEventFlags.RIGHTUP;
            mouseInputSettings.MouseInput.dwExtraInfo = UIntPtr.Zero;
            MouseInputSettings = mouseInputSettings;
        }

        public static int GetForegroundWindowProcessId() => GetProcessIdFromWindowHandle(GetForegroundWindow());

        public static int GetProcessIdFromWindowHandle(IntPtr windowHandle)
        {
            GetWindowThreadProcessId(windowHandle, out var processId);
            return processId;
        }

        public static void SendKeyboardKey(IntPtr windowHandle, Key key, bool keyDown)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var scanCode = MapVirtualKey(virtualKey, 0) << 16;
            PostMessage(windowHandle, keyDown ? SystemMessage.KeyboardKeyDown : SystemMessage.KeyboardKeyUp, virtualKey,
                scanCode | 1);
        }

        public static void SendKeyboardKeyDown(IntPtr windowHandle, Key key) =>
            SendKeyboardKey(windowHandle, key, true);

        public static void SendKeyboardKeyUp(IntPtr windowHandle, Key key) =>
            SendKeyboardKey(windowHandle, key, false);

        public static void SendKeyboardKeyPress(IntPtr windowHandle, Key key)
        {
            new Thread(() =>
            {
                SendKeyboardKeyDown(windowHandle, key);
                Thread.Sleep(10);
                SendKeyboardKeyUp(windowHandle, key);
            }) {IsBackground = true}.Start();
        }

        public static int GetCursorPositionLParam(int x, int y)
        {
            return (y << 16) | (x & 0xFFFF);
        }

        public static void SendMouseKey(IntPtr windowHandle, MouseButton mouseButton, bool keyDown, int x = -1, int y = -1)
        {
            //SendMessage((int) HookModel.WindowHandle, SystemMessage.MouseLeftButtonDown, KeyDownMessage.LeftButton, MAKELPARAM(345, 165));
            var lParam = 0;
            if (y != -1) lParam = y << 16;
            if (x != -1) lParam |= x & 0xFFFF;
            
            // SystemMessage GetSystemMessage(MouseButton mouseButton, bool keyDown) => mouseButton switch
            // {
            //     MouseButton.LeftButton => keyDown ? SystemMessage.MouseLeftButtonDown : SystemMessage.MouseLeftButtonUp,
            //     MouseButton.RightButton => keyDown ? SystemMessage.MouseRightButtonDown : SystemMessage.MouseRightButtonUp,
            //     MouseButton.MiddleButton => keyDown ? SystemMessage.MouseMiddleButtonDown : SystemMessage.MouseMiddleButtonUp,
            //     _ => throw new NotImplementedException()
            // };

            var systemMessage = mouseButton switch
            {
                MouseButton.LeftButton => keyDown ? SystemMessage.MouseLeftButtonDown : SystemMessage.MouseLeftButtonUp,
                MouseButton.RightButton => keyDown ? SystemMessage.MouseRightButtonDown : SystemMessage.MouseRightButtonUp,
                MouseButton.MiddleButton => keyDown ? SystemMessage.MouseMiddleButtonDown : SystemMessage.MouseMiddleButtonUp,
                _ => throw new NotImplementedException("Unknown system message for mouse")
            };//GetSystemMessage(mouseButton, keyDown);
            
            if (!keyDown)
                mouseButton &= (MouseButton)(0x7FFFFFFF ^ (int)MouseButton.LeftButton);
            //SendMessage(windowHandle, SystemMessage.MouseMove, 0, lParam);
            SendMessage(windowHandle, systemMessage, (int) mouseButton, lParam);
        }
        
        public static void SendMouseMove(IntPtr windowHandle, int x = -1, int y = -1)
        {
            //SendMessage((int) HookModel.WindowHandle, SystemMessage.MouseLeftButtonDown, KeyDownMessage.LeftButton, MAKELPARAM(345, 165));
            var lParam = 0;
            if (y != -1) lParam = y << 16;
            if (x != -1) lParam |= x & 0xFFFF;
            
            // SystemMessage GetSystemMessage(MouseButton mouseButton, bool keyDown) => mouseButton switch
            // {
            //     MouseButton.LeftButton => keyDown ? SystemMessage.MouseLeftButtonDown : SystemMessage.MouseLeftButtonUp,
            //     MouseButton.RightButton => keyDown ? SystemMessage.MouseRightButtonDown : SystemMessage.MouseRightButtonUp,
            //     MouseButton.MiddleButton => keyDown ? SystemMessage.MouseMiddleButtonDown : SystemMessage.MouseMiddleButtonUp,
            //     _ => throw new NotImplementedException()
            // };

            // var systemMessage = mouseButton switch
            // {
            //     MouseButton.LeftButton => keyDown ? SystemMessage.MouseLeftButtonDown : SystemMessage.MouseLeftButtonUp,
            //     MouseButton.RightButton => keyDown ? SystemMessage.MouseRightButtonDown : SystemMessage.MouseRightButtonUp,
            //     MouseButton.MiddleButton => keyDown ? SystemMessage.MouseMiddleButtonDown : SystemMessage.MouseMiddleButtonUp,
            //     _ => throw new NotImplementedException("Unknown system message for mouse")
            // };//GetSystemMessage(mouseButton, keyDown);
            
            //SendMessage(windowHandle, SystemMessage.MouseMove, 0, lParam);
            SendMessage(windowHandle, SystemMessage.SetCursor, 0, lParam);
        }
        
        //TODO: find way to click on exact coordinates
        public static void SendMouseClick(IntPtr windowHandle, MouseButton mouseButton, int x = -1, int y = -1)
        {
            SendMouseKey(windowHandle, mouseButton, true, x, y);
            SendMouseKey(windowHandle, mouseButton, false, x, y);
        }

        // public string ReadStringToEnd(int address, Encoding encoding, int maxLength = MaxStringLength) =>
        //     ReadStringToEnd(_handle, address, encoding, maxLength);

        public static unsafe string ReadStringToEnd(IntPtr handle, int address, Encoding encoding,
            int maxLength = MaxStringLength)
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

        // public string ReadString(int address, byte length, Encoding encoding) =>
        //     ReadString(_handle, address, length, encoding);

        public static unsafe string ReadString(IntPtr handle, int address, int length, Encoding encoding)
        {
            try
            {
                var bytesInChar = encoding.GetMaxByteCount(0);
                var buffer = GetBuffer(handle, address, length * bytesInChar);

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

        #region Read/Write types

        #region Read

        public static byte ReadByte(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadByte(handle, GetAddress(handle, baseAddress, offsets));

        public static byte ReadByte(IntPtr handle, int address) =>
            GetBuffer(handle, address, sizeof(byte))[0];

        public static bool ReadBool(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadBool(handle, GetAddress(handle, baseAddress, offsets));

        public static bool ReadBool(IntPtr handle, int address) =>
            ReadByte(handle, address) == 1; //GetBuffer(handle, address, sizeof(byte))[0] == 1;

        public static byte[] ReadBytes(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadBytes(handle, GetAddress(handle, baseAddress, offsets));

        public static byte[] ReadBytes(IntPtr handle, int address) =>
            GetBuffer(handle, address, sizeof(byte));

        public static short ReadShort(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadShort(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe short ReadShort(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address, sizeof(short)))
            {
                return *(short*) pointer;
            }
        }

        public static ushort ReadUShort(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadUShort(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe ushort ReadUShort(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address, sizeof(ushort)))
            {
                return *(ushort*) pointer;
            }
        }

        public static int ReadInt(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadInt(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe int ReadInt(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(int*) pointer;
            }
        }

        public static uint ReadUInt(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadUInt(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe uint ReadUInt(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(uint*) pointer;
            }
        }

        public static float ReadFloat(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadFloat(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe float ReadFloat(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(float*) pointer;
            }
        }

        public static double ReadDouble(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadDouble(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe double ReadDouble(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(double*) pointer;
            }
        }

        public static long ReadLong(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadLong(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe long ReadLong(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(long*) pointer;
            }
        }

        public static ulong ReadULong(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadULong(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe ulong ReadULong(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address))
            {
                return *(ulong*) pointer;
            }
        }

        public static T ReadStructure<T>(IntPtr handle, int baseAddress, params int[] offsets) =>
            ReadStructure<T>(handle, GetAddress(handle, baseAddress, offsets));

        public static unsafe T ReadStructure<T>(IntPtr handle, int address)
        {
            fixed (byte* pointer = GetBuffer(handle, address, Marshal.SizeOf(default(T))))
            {
                return Marshal.PtrToStructure<T>((IntPtr) pointer);
            }
        }

        #endregion

        #region Write

        public static void WriteByte(IntPtr handle, int baseAddress, int[] offsets, byte value) =>
            WriteByte(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteByte(IntPtr handle, int address, byte value) =>
            WriteMemory(handle, address, new[] {value});

        public static void WriteSByte(IntPtr handle, int baseAddress, int[] offsets, sbyte value) =>
            WriteSByte(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteSByte(IntPtr handle, int address, sbyte value) =>
            WriteMemory(handle, address, new[] {(byte) value});

        public static void WriteBool(IntPtr handle, int baseAddress, int[] offsets, bool value) =>
            WriteBool(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteBool(IntPtr handle, int address, bool value) =>
            WriteByte(handle, address, (byte) (value ? 1 : 0));

        public static void WriteShort(IntPtr handle, int baseAddress, int[] offsets, short value) =>
            WriteShort(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteShort(IntPtr handle, int address, short value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteUShort(IntPtr handle, int baseAddress, int[] offsets, ushort value) =>
            WriteUShort(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteUShort(IntPtr handle, int address, ushort value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteInt(IntPtr handle, int baseAddress, int[] offsets, int value) =>
            WriteInt(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteInt(IntPtr handle, int address, int value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteUInt(IntPtr handle, int baseAddress, int[] offsets, uint value) =>
            WriteUInt(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteUInt(IntPtr handle, int address, uint value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteFloat(IntPtr handle, int baseAddress, int[] offsets, float value) =>
            WriteFloat(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteFloat(IntPtr handle, int address, float value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteDouble(IntPtr handle, int baseAddress, int[] offsets, double value) =>
            WriteDouble(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteDouble(IntPtr handle, int address, double value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteLong(IntPtr handle, int baseAddress, int[] offsets, long value) =>
            WriteLong(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteLong(IntPtr handle, int address, long value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        public static void WriteULong(IntPtr handle, int baseAddress, int[] offsets, ulong value) =>
            WriteULong(handle, GetAddress(handle, baseAddress, offsets), value);

        public static void WriteULong(IntPtr handle, int address, ulong value) =>
            WriteMemory(handle, address, BitConverter.GetBytes(value));

        #endregion

        #endregion

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

        // public unsafe void OverwriteBytes(HookModel hook, int[] pattern, int[] signature)
        // {
        //     var address = FindSignatureAddress(pattern, hook);
        //     //_dispatcher.Invoke(() => address = Addresses.ContainsKey(pattern) ? (IntPtr) Addresses[pattern] : (IntPtr) FindSignature(pattern));
        //     if (address == 0) return;
        //
        //     var handle = hook.Handle;
        //     var val = new byte[signature.Length];
        //     var buffer = new byte[pattern.Length];
        //     ReadMemory(handle, address, buffer);
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
        //     WriteMemory(handle, address, val);
        // }

        public static void OverwriteBytes(IntPtr handle, int address, int[] signature)
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

        public static unsafe int FindSignatureAddress(HookModel hook, int[] pattern)
        {
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
        
        public static int InvokeFunction(HookModel hook, uint functionAddress, IntPtr param)
        {
            var threadHandle = CreateRemoteThread(hook.Handle, IntPtr.Zero, 0, functionAddress, param, 0, out _);
            WaitForSingleObject(threadHandle, uint.MaxValue);
            GetExitCodeThread(threadHandle, out var returnValue);

            return (int) returnValue;
        }

        public static bool ReadMemory(IntPtr handle, int address, byte[] buffer) =>
            ReadProcessMemory(handle, address, buffer, buffer.Length, out _);

        public static unsafe bool ReadMemory(IntPtr handle, int address, byte* buffer, int size = 4) =>
            ReadProcessMemory(handle, address, buffer, size, out _);

        public static void WriteMemory(IntPtr handle, int address, byte[] buffer) =>
            WriteProcessMemory(handle, address, buffer, buffer.Length, out _);

        public static void WriteMemory(IntPtr handle, int address, byte[] buffer, int size) =>
            WriteProcessMemory(handle, address, buffer, size, out _);

        public static int AllocateMemory(IntPtr handle, int size, MemoryProtection protection = MemoryProtection.ExecuteRead,
            int specificAddress = 0) =>
            VirtualAllocEx(handle, specificAddress, size, AllocationType.Commit, protection);

        /// <summary>
        /// Allocates memory and put jmp to <paramref name="returnAddress"/> at the end if not 0
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="size"></param>
        /// <param name="returnAddress"></param>
        public static int CreateCave(IntPtr handle, int size, int returnAddress = 0)
        {
            var caveAddress = AllocateMemory(handle, size + 5);

            return caveAddress;
        }


        public static byte[] GetBuffer(IntPtr handle, int baseAddress, int[] offsets, byte size = 4) =>
            GetBuffer(handle, GetAddress(handle, baseAddress, offsets), size);

        public static byte[] GetBuffer(IntPtr handle, int address, int size = 4)
        {
            var bytes = new byte[size];
            ReadMemory(handle, address, bytes);
            return bytes;
            //TODO: make it nullable, bc yes
            //return ReadMemory(handle, address, bytes) ? bytes : null;
        }

        // public static int GetAddress(int baseAddress, params int[] offsets) => GetAddress(_handle, baseAddress, offsets);

        public static unsafe int GetAddress(IntPtr handle, int baseAddress, params int[] offsets)
        {
            var bytes = stackalloc byte[sizeof(int)];
            var num = (int*) bytes;

            foreach (var offset in offsets)
            {
                if (!ReadMemory(handle, baseAddress, bytes))
                    return 0;
                baseAddress = *num + offset;
            }

            return baseAddress;
        }

        public static byte[] AsmJump(ulong destination, ulong origin)
        {
            var jumpStart = destination - origin - CaveOffset;
            var bytes = new byte[sizeof(ulong) + 1];
            bytes[0] = (byte) InstructionsEnum.JmpRM32;
            var address = BitConverter.GetBytes(jumpStart);
            // idk why size - 1
            Array.Copy(address, 0, bytes, 1, sizeof(ulong) - 1);
            int newLength;
            for (newLength = bytes.Length - 1; newLength > 0; newLength--)
            {
                if (bytes[newLength] != 0)
                    break;
            }

            Array.Resize(ref bytes, newLength + 1);

            return bytes;
        }

        public static byte[] AsmJumpOld(ulong destination, ulong origin, byte[] cave = null)
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

        public enum Instructions
        {
            Push,
            Pop,
            Mov,
            Add,
            Sub,
            Xor,
            And,
            Or,
            Inc,
            Dec,
            Cmp,
            Jmp,
            Call,
            Shl,
            Shr,
            Mul
        }

        public enum InstructionsEnum : byte
        {
            MovRM8R8 = 0x88,
            MovRM16R16 = 0x89,
            MovRM32R32 = 0x89,
            MovR8RM8 = 0x8A,
            MovR16RM16 = 0x8B,
            MovR32RM32 = 0x8B,
            MovEaxI16 = 0xB8,
            MovEaxI32 = 0xB8,
            MovEcxI16 = 0xB9,
            MovEcxI32 = 0xB9,
            MovEdxI16 = 0xBA,
            MovEdxI32 = 0xBA,
            MovEbxI16 = 0xBB,
            MovEbxI32 = 0xBB,
            MovEspI16 = 0xBC,
            MovEspI32 = 0xBC,
            MovEbpI16 = 0xBD,
            MovEbpI32 = 0xBD,
            MovEsiI16 = 0xBE,
            MovEsiI32 = 0xBE,
            MovEdiI16 = 0xBF,
            MovEdiI32 = 0xBF,

            ShlRM8 = 0xD0,
            ShlRM16 = 0xD1,
            ShlRM32 = 0xD1,
            // ShrRM8 = 0xD0,

            IncRM8 = 0xFE,
            IncRM16 = 0xFF,
            IncRM32 = 0xFF,


            AddRM8R8 = 0x00,
            AddRM16R16 = 0x01,
            AddRM32R32 = 0x01,
            AddR8RM8 = 0x02,
            AddR16RM16 = 0x03,
            AddR32RM32 = 0x03,

            SubRM8R8 = 0x28,
            SubRM16R16 = 0x29,
            SubRM32R32 = 0x29,
            SubR8RM8 = 0x2A,
            SubR16RM16 = 0x2B,
            SubR32RM32 = 0x2B,

            MulRM8 = 0xF6,
            MulRM16 = 0xF7,
            MulRM32 = 0xF7,

            AndRM8R8 = 0x20,
            AndRM16R16 = 0x21,
            AndRM32R32 = 0x21,
            AndR8RM8 = 0x22,
            AndR16RM16 = 0x23,
            AndR32RM32 = 0x23,

            CmpRM8R8 = 0x38,
            CmpRM16R16 = 0x39,
            CmpRM32R32 = 0x39,
            CmpR8RM8 = 0x3A,
            CmpR16RM16 = 0x3B,
            CmpR32RM32 = 0x3B,

            JmpRM16 = 0xE9,
            JmpRM32 = 0xE9,

            CallRM16 = 0xFF,
            CallRM32 = 0xFF,

            PushRM16 = 0xFF,
            PushRM32 = 0xFF,
            PushEax = 0x50,
            PushEcx = 0x51,
            PushEdx = 0x52,
            PushEbx = 0x53,
            PushEsp = 0x54,
            PushEbp = 0x55,
            PushEsi = 0x56,
            PushEdi = 0x57,

            PopRM16 = 0x8F,
            PopRM32 = 0x8F,
            PopEax = 0x58,
            PopEcx = 0x59,
            PopEdx = 0x5A,
            PopEbx = 0x5B,
            PopEsp = 0x5C,
            PopEbp = 0x5D,
            PopEsi = 0x5E,
            PopEdi = 0x5F,

            XorRM8R8 = 0x30,
            XorRM16R16 = 0x31,
            XorRM32R32 = 0x31,
            XorR8RM8 = 0x32,
            XorR16RM16 = 0x33,
            XorR32RM32 = 0x33,



            Or = 0x08,
            Xor = 0x30,
            Inc = 0x40,
            Dec = 0x48,
            Test = 0x84,
            Nop = 0x90,
            RetN = 0xC3, //0xC2
            RetF = 0xCA, //0xCB
            Call = 0x9A, // 0xE8, 0xFF/2, 0xFF/3
            Mul = 0xF7,
        }

        /*
         * 31 C0 = xor eax,eax
         * 33 00 = xor eax,[eax] | xor [eax],eax
         * 
         */

        public enum RegisterOpcode : byte
        {
            EaxEax = 0xC0,
            EaxEcx = 0xC8,
            EaxEdx = 0xD0,
            EaxEbx = 0xD8,
            EaxEsp = 0xE0,
            EaxEbp = 0xE8,
            EaxEsi = 0xF0,
            EaxEdi = 0xF8,

            EcxEax = 0xC1,
            EcxEcx = 0xC9,
            EcxEdx = 0xD1,
            EcxEbx = 0xD9,
            EcxEsp = 0xE1,
            EcxEbp = 0xE9,
            EcxEsi = 0xF1,
            EcxEdi = 0xF9,

            EdxEax = 0xC2,
            EdxEcx = 0xCA,
            EdxEdx = 0xD2,
            EdxEbx = 0xDA,
            EdxEsp = 0xE2,
            EdxEbp = 0xEA,
            EdxEsi = 0xF2,
            EdxEdi = 0xFA,

            EbxEax = 0xC3,
            EbxEcx = 0xCB,
            EbxEdx = 0xD3,
            EbxEbx = 0xDB,
            EbxEsp = 0xE3,
            EbxEbp = 0xEB,
            EbxEsi = 0xF3,
            EbxEdi = 0xFB,

            EspEax = 0xC4,
            EspEcx = 0xCC,
            EspEdx = 0xD4,
            EspEbx = 0xDC,
            EspEsp = 0xE4,
            EspEbp = 0xEC,
            EspEsi = 0xF4,
            EspEdi = 0xFC,

            EbpEax = 0xC5,
            EbpEcx = 0xCD,
            EbpEdx = 0xD5,
            EbpEbx = 0xDD,
            EbpEsp = 0xE5,
            EbpEbp = 0xED,
            EbpEsi = 0xF5,
            EbpEdi = 0xFD,

            EsiEax = 0xC6,
            EsiEcx = 0xCE,
            EsiEdx = 0xD6,
            EsiEbx = 0xDE,
            EsiEsp = 0xE6,
            EsiEbp = 0xEE,
            EsiEsi = 0xF6,
            EsiEdi = 0xFE,

            EdiEax = 0xC7,
            EdiEcx = 0xCF,
            EdiEdx = 0xD7,
            EdiEbx = 0xDF,
            EdiEsp = 0xE7,
            EdiEbp = 0xEF,
            EdiEsi = 0xF7,
            EdiEdi = 0xFF,
        }

        #region Native

        #region Kernel

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
            AsyncWindowPos = 0x4000,
            DeferErase = 0x2000,
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
        public static extern bool VirtualFreeEx(
            IntPtr handle,
            int address,
            int size,
            AllocationType allocationType);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(
            IntPtr handle,
            int address,
            int size,
            MemoryProtection newProtection,
            out MemoryProtection oldProtection);

        [DllImport("kernel32.dll")]
        public static extern int VirtualAllocEx(
            IntPtr handle,
            int address,
            int size,
            AllocationType allocationType,
            MemoryProtection protection);

        [DllImport("kernel32.dll")]
        public static extern unsafe bool ReadProcessMemory(IntPtr handle, int address, byte* buffer, int size, out IntPtr numberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, int address, byte[] buffer, int size, out IntPtr numberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, int address, int[] buffer, int size, out IntPtr numberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, int address, float[] buffer, int size, out IntPtr numberOfBytesRead);
        
        [DllImport("kernel32.dll")]
        public static extern unsafe bool WriteProcessMemory(IntPtr handle, int address, byte* buffer, int size, out IntPtr numberOfBytesWritten);
        
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr handle, int address, byte[] buffer, int size, out IntPtr numberOfBytesWritten);
        
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr handle, int address, int[] buffer, int size, out IntPtr numberOfBytesWritten);
        
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr handle, int address, float[] buffer, int size, out IntPtr numberOfBytesWritten);
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr handle,
            IntPtr threadAttributes, uint stackSize, uint startAddress,
            IntPtr parameter, uint creationFlags, out uint threadId);
        
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);
        
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);
        
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId
        );
        
        #endregion

        #region User32
        
        [StructLayout(LayoutKind.Explicit)]
        public struct Input
        {
            [FieldOffset(0)]
            public InputType type;
            //internal InputUnion U;
            [FieldOffset(4)]
            public MouseInput MouseInput;
            [FieldOffset(4)]
            public KeyboardInput KeyboardInput;
            [FieldOffset(4)]
            public HardwareInput HardwareInput;
            public static int Size => Marshal.SizeOf(typeof(Input));
        }

        public enum InputType : uint
        {
            Mouse,
            Keyboard,
            Hardware
        }
        
        [Flags]
        internal enum MouseEventFlags : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal MouseEventFlags dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }
        
        [Flags]
        internal enum KeyEventFlags : uint
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            SCANCODE = 0x0008,
            UNICODE = 0x0004
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            internal short wVk;
            internal short wScan;
            internal KeyEventFlags dwFlags;
            internal int time;
            internal UIntPtr dwExtraInfo;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }
        
        // [StructLayout(LayoutKind.Explicit)]
        // internal struct InputUnion
        // {
        //     [FieldOffset(0)]
        //     public MouseInput MouseInput;
        //     [FieldOffset(0)]
        //     public KeyboardInput KeyboardInput;
        //     [FieldOffset(0)]
        //     public HardwareInput HardwareInput;
        // }

        [Flags]
        public enum MouseButton
        {
            Control = 0x0008,
            LeftButton = 0x0001,
            MiddleButton = 0x0010,
            RightButton = 0x0002,
            Shift = 0x0004,
            XButton = 0x0020,
            XButton2 = 0x0040
        }

        public enum SystemMessage : uint
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

        public enum ClipboardFormat : uint
        {
            Text = 1, // ANSI
            BitMap = 2,
            OemText = 7,
            UnicodeText = 13,
            DspText = 0x81
        }

        public enum KeyMappingType
        {
            VirtualKeyToScanCode = 0,
            ScanCodeToVirtualKey = 1,
            VirtualKeyToChar = 2,
            ScanCodeToVirtualKeyEx = 3
        }

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(
            IntPtr handle,
            out int processId);

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(
            int virtualKey,
            int mapType);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(
            IntPtr windowHandle,
            SystemMessage message,
            int wParam,
            int lParam);
        
        [DllImport("user32.dll")]
        public static extern int SendMessage(
            IntPtr hWnd, 
            SystemMessage msg, 
            int wParam, 
            int lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessage(
            out MSG message,
            IntPtr windowHandle,
            uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG message);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG message);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(ClipboardFormat format);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr SetTimer(IntPtr windowHandle, IntPtr nIDEvent, uint uElapse,
            TimerProc lpTimerFunc);
        
        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, Input [] pInputs, int cbSize);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr windowHandle);

        public delegate void TimerProc(IntPtr windowHandle, uint uMsg, IntPtr nIDEvent, uint dwTime);
        
        #endregion

        #endregion
    }
}