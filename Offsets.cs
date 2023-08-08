using System;
using System.ComponentModel;

namespace TroveSkip
{
    public static class Offsets
    {
        public static class LocalPlayer
        {
            public static readonly int[] Self = {0x0, 0x28};
            
            public static readonly int[] CharacterSelf = Self.Join(0xC4);
            public static readonly int[] Name = Self.Join(0x1B4, 0x0); 
            public static readonly int[] MinimalLootRarity = Self.Join(0x678);
            
            public static class Character
            {
                public static readonly int[] ControllerSelf = CharacterSelf.Join(0x4); 
                public static readonly int[] InfoSelf = CharacterSelf.Join(0x10C);
                public static readonly int[] StatsSelf = CharacterSelf.Join(0x2D4);
                
                public static readonly int[] CharacterId = CharacterSelf.Join(0xFC, 0x334);
                public static readonly int[] CurrentHealth = CharacterSelf.Join(0xC, 0x38, 0x10, 0x138);
                
                public static class Controller
                {
                    public static readonly int[] PositionX = ControllerSelf.Join(0x60);
                    public static readonly int[] PositionY = ControllerSelf.Join(0x64);
                    public static readonly int[] PositionZ = ControllerSelf.Join(0x68);
                    public static readonly int[] VelocityX = ControllerSelf.Join(0x90);
                    public static readonly int[] VelocityY = ControllerSelf.Join(0x94);
                    public static readonly int[] VelocityZ = ControllerSelf.Join(0x98);
                    public static readonly int[] Gravity = ControllerSelf.Join(0xB8);
                    public static readonly int[] RotationX = ControllerSelf.Join(0x1C4); //Works if State = 0
                    public static readonly int[] RotationY = ControllerSelf.Join(0x1C4); //Broken
                    public static readonly int[] RotationState = ControllerSelf.Join(0x1D0); //Default = -1, Block = 0
                }

                public static class Stats
                {
                    public static readonly int[] PhysiscalDamage = StatsSelf.Join(0x19C);
                    public static readonly int[] MagicDamage = StatsSelf.Join(0x1A0);
                    public static readonly int[] MaximumHealth = StatsSelf.Join(0x1A4);
                    public static readonly int[] MaximumEnergy = StatsSelf.Join(0x1A8);
                    public static readonly int[] HealthRegeneration = StatsSelf.Join(0x1AC);
                    public static readonly int[] EnergyRegeneration = StatsSelf.Join(0x1B0);
                    //1b4
                    public static readonly int[] CriticalHit = StatsSelf.Join(0x1B8);
                    public static readonly int[] MovementSpeed = StatsSelf.Join(0x1BC);
                    public static readonly int[] Jumps = StatsSelf.Join(0x1C0);
                    //1c4
                    //1c8
                    //1cc
                    public static readonly int[] MagicFind = StatsSelf.Join(0x1D0);
                    public static readonly int[] Lasermancy = StatsSelf.Join(0x1D4);
                    public static readonly int[] AttackSpeed = StatsSelf.Join(0x1D8);
                    public static readonly int[] Flasks = StatsSelf.Join(0x1DC);
                    public static readonly int[] ExperienceGain = StatsSelf.Join(0x1F0);
                    public static readonly int[] CriticalDamage = StatsSelf.Join(0x1F4);
                    public static readonly int[] BattleFactor = StatsSelf.Join(0x1F8);
                    public static readonly int[] PowerRank = StatsSelf.Join(0x200);
                    public static readonly int[] EncryptionKey = StatsSelf.Join(0x21C);
                    public static readonly int[] Light = StatsSelf.Join(0x238);
                }
                
                public static class Info
                {
                    public static readonly int[] MasteryRank = InfoSelf.Join(0xAC);
                    public static readonly int[] MasteryPoints = InfoSelf.Join(0xE4);
                    public static readonly int[] IsMasteryRankIncreased = InfoSelf.Join(0xE8);
                }
            }
        }
        
        public static class Camera
        {
            public static readonly int Self = 0x4;

            public static readonly int[] LocalCamera = Self.Join(0x24);
            public static readonly int[] HorizontalMove = Self.Join(0x2C);
            public static readonly int[] VerticalMove = Self.Join(0x28);
            public static readonly int[] MinimumDistance = Self.Join(0x38);
            public static readonly int[] MaximumDistance = Self.Join(0x3C);
            
            public static class Rotation
            {
                public static readonly int[] Self = LocalCamera.Join(0x84, 0x0);

                public static readonly int[] RotationX = Self.Join(0x100);
                public static readonly int[] RotationY = Self.Join(0x104);
                public static readonly int[] RotationZ = Self.Join(0x108);
            }
        }
        
        public static class World
        {
            public static readonly int Id = 0x8;
            public static readonly int Players = 0xFC;
            
            public static class PlayersList
            {
                public static readonly int[] Self = Players.Join(0x0);
                public static readonly int[] Count = Players.Join(0x2C);
            }
        }

        public static class Settings
        {
            public static readonly int Grama = 0x4; // = min(96, DrawDistance / 2)
            public static readonly int ObjectsDrawDistance = 0x8;
            public static readonly int AntiAliasing = 0xC; //boolean
            public static readonly int DetailDistance = 0x14;
            public static readonly int ShaderDetail = 0x1C;
            [Description("Not changable direct")] public static readonly int SuperSampling = 0x20;
            
            [Obsolete("Ex version can't be affected by changing graphics")] 
            public static readonly int VFXQuantity = 0x24; //50000 / vfxquantity
            
            public static readonly int DrawDistance = 0x28;
            public static readonly int VFXQuantityEx = 0x2C; //better to use than "VFXQuantity"
            [Description("Not changable direct")] public static readonly int Gamma = 0x30;
            [Description("Not changable direct")] public static readonly int Brightness = 0x34;
        }
    }
}