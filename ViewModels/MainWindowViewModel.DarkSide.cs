using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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

        // private static int[] _autoLootSignature =
        // {
        //     0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF0, 0xD9, 0xEE, 0x83, 0xEC, 0x38, 0x56, 0x57, 0x8B, 0xF9, 0xDD, 0x87, 0x78, 0x03, 00, 00, 0xDF, 0xF1, 0xDD, 0xD8, 0x76, 0x45
        // };
        //
        // private static int[] _autoLootEnabledSignature =
        // {
        //     0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF0, 0xD9, 0xEE, 0x83, 0xEC, 0x38, 0x56, 0x57, 0x8B, 0xF9, 0xDD, 0x87, 0x78, 0x03, 00, 00, 0xDF, 0xF1, 0xDD, 0xD8, 0x76, 0x27
        // };

        private const int MinimalDrawDistance = 31;
        private const int MaximalDrawDistance = 211;
        private const int MaxGrama = 96;
        private const int DefaultObjectsDistance = 150;
        private const int NoGraphicsValue = 0;

        private const int MinimalModuleOffset = 16_200_000;
        private const int MaximalModuleOffset = 19_000_000;

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

        private static readonly int[] CameraVerticalRotationOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.VerticalMove
        };
        
        private static readonly int[] CameraHorizontalRotationOffsets =
        {
            (int) GameOffset.Camera,
            (int) CameraOffset.HorizontalMove
        };

        private static readonly int[] CharacterIdOffsets = LocalCharactersOffsets.Join(0xfc, 0x334);
        private static readonly int[] CurrentHealthOffsets = LocalCharactersOffsets.Join(0xC, 0x38, 0x10, 0x138);

        private static readonly int[] LocalMoveOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Controller);
        private static readonly int[] LocalXPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionX); // float
        private static readonly int[] LocalYPosition = LocalMoveOffsets.Join((int) ControllerOffset.PositionY);
        private static readonly int[] LocalXVelocity = LocalMoveOffsets.Join((int) ControllerOffset.VelocityX); // float
        private static readonly int[] GravityOffsets = LocalMoveOffsets.Join((int) ControllerOffset.Gravity); // float

        private static readonly int[] StatsOffsets = LocalCharactersOffsets.Join((int) CharacterOffset.Stats);
        private static readonly int[] SpeedOffsets = StatsOffsets.Join((int) StatOffset.MovementSpeed); // encoded float
        private static readonly int[] PowerRankOffsets = StatsOffsets.Join((int) StatOffset.PowerRank); // encoded float
        private static readonly int[] MaxHealthStatOffsets = StatsOffsets.Join((int) StatOffset.MaximumHealth); // encoded float

        private static readonly int[]
            StatsEncryptionKeyOffsets = StatsOffsets.Join((int) StatOffset.EncryptionKey); // encoder (uint)

        private static readonly int[] XView = ViewOffsets.Join(0x100);

        private static readonly int[] PlayersInWorld = {(int) Memory.World.WorldOffset.Players};

        private static readonly int[]
            PlayersArray = PlayersInWorld.Join((int) PlayersOffset.PlayersArray, 0x0); // local player

        private static readonly int[] PlayersCountInWorldOffsets = PlayersInWorld.Join((int) PlayersOffset.Count);

        private static readonly int[] FirstPlayerXPosition = PlayersArray.Join(CharacterPositionX);

        private static readonly int[] WorldIdOffsets = {(int) Memory.World.WorldOffset.Id};

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

        private static readonly int[] HalfDrawDistanceOffsets = {(int) SettingOffset.Grama};
        private static readonly int[] IdkObject = {(int) SettingOffset.ObjectsDrawDistance};
        private static readonly int[] DrawDistanceOffsets = {(int) SettingOffset.DrawDistance};

        //private readonly int[] MaxCamDist = { 0x4, 0x3C };
        //private readonly int[] MinCamDist = { 0x4, 0x38 };
    }
}