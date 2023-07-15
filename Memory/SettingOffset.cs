using System;
using System.ComponentModel;

namespace TroveSkip.Memory
{
    public enum SettingOffset
    {
        Grama = 0x4, // = min(96, DrawDistance / 2)
        ObjectsDrawDistance = 0x8,
	AntiAliasing = 0xC, //boolean
	DetailDistance = 0x14,
        ShaderDetail = 0x1C,
        [Description("Not changable direct")]
        SuperSampling = 0x20,
        [Obsolete("Ex version can't be affected by changing graphics")]
        VFXQuantity = 0x24, //50000 / vfxquantity
        DrawDistance = 0x28,
        VFXQuantityEx = 0x2C, //better to use than "VFXQuantity"
        [Description("Not changable direct")]
        Gamma = 0x30,
        [Description("Not changable direct")]
        Brightness = 0x34
    }
}