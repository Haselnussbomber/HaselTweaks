using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xB348)]
public unsafe struct RaptureGearsetModule
{
    public static RaptureGearsetModule* Instance() => (RaptureGearsetModule*)Framework.Instance()->GetUiModule()->GetRaptureGearsetModule();

    [FieldOffset(0x00)] public void* vtbl;
    [FieldOffset(0x30)] public fixed byte ModuleName[16];
    [FieldOffset(0x48)] public GearsetArray Gearsets;

    [Flags]
    public enum GearsetFlag : byte
    {
        /// <summary>
        /// Set when this gearset entry has been created.
        /// </summary>
        Exists = 1 << 0,

        Unknown1 = 1 << 1,

        /// <summary>
        /// Shows a red exclamation mark with message "The specified main arm was missing from your Armoury Chest."
        /// </summary>
        MainHandMissing = 1 << 2,

        /// <summary>
        /// Set when "Display Headgear" is ticked.
        /// </summary>
        DisplayHeadgear = 1 << 3,

        /// <summary>
        /// Set when "Display Sheathed Arms" is ticked.
        /// </summary>
        DisplaySheathedArms = 1 << 4,

        /// <summary>
        /// Set when "Manually adjust visor (select gear only)." is ticked.
        /// </summary>
        ManuallyAdjustVisor = 1 << 5,

        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x1C0)]
    public struct Gearset
    {
        internal const int StructSize = 0x1C0;

        [FieldOffset(0x00)] public byte ID;
        [FieldOffset(0x01)] public fixed byte RawName[0x2F];
        [FieldOffset(0x31)] public byte ClassJob;
        [FieldOffset(0x32)] public byte GlamourSetLink;
        [FieldOffset(0x33)] public byte Unknown1;
        [FieldOffset(0x34)] public ushort ItemLevel;
        [FieldOffset(0x36)] public byte InstantPortraitID;
        [FieldOffset(0x37)] public GearsetFlag Flags;

        private const int ItemDataOffset = 0x38;
        [FieldOffset(ItemDataOffset)] public GearsetItemArray Items;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 00)] public GearsetItem MainHand;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 01)] public GearsetItem OffHand;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 02)] public GearsetItem Head;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 03)] public GearsetItem Body;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 04)] public GearsetItem Hands;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 05)] public GearsetItem Belt;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 06)] public GearsetItem Legs;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 07)] public GearsetItem Feet;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 08)] public GearsetItem Ears;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 09)] public GearsetItem Neck;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 10)] public GearsetItem Wrists;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 11)] public GearsetItem RingRight;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 12)] public GearsetItem RightLeft;
        [FieldOffset(ItemDataOffset + GearsetItem.StructSize * 13)] public GearsetItem SoulStone;

        public string Name
        {
            get
            {
                fixed (byte* ptr = RawName)
                {
                    return Encoding.UTF8.GetString(ptr, 0x2F);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 0xAF2C)]
    public struct GearsetArray
    {
        public const int Length = 101;

        private fixed byte data[Length * Gearset.StructSize];

        public Gearset* this[int i]
        {
            get
            {
                if (i < 0 || i >= Length) return null;
                fixed (byte* p = data)
                {
                    return (Gearset*)(p + sizeof(Gearset) * i);
                }
            }
        }
    }

    [Flags]
    public enum GearsetItemFlag : byte
    {
        /// <summary>
        /// Shows a yellow exclamation mark with message "One or more items were missing from your Armoury Chest."
        /// </summary>
        MissingItem = 1 << 0,

        Unknown1 = 1 << 1,

        /// <summary>
        /// Shows a gray exclamation mark with message "One or more items were not the specified color."
        /// </summary>
        DifferentColor = 1 << 2,

        /// <summary>
        /// Shows a gray exclamation mark with message "One or more items were not melded with the specified materia."
        /// </summary>
        DifferentMateria = 1 << 3,

        /// <summary>
        /// Shows a gray exclamation mark with message "One or more items did not have the specified appearance."
        /// </summary>
        DifferentAppearance = 1 << 4,

        Unknown5 = 1 << 5,
        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x1C)]
    public struct GearsetItem
    {
        internal const int StructSize = 0x1C;

        [FieldOffset(0x00)] public uint ItemID;
        [FieldOffset(0x04)] public uint GlamourItemID;
        [FieldOffset(0x08)] public ushort Stain;

        [FieldOffset(0x17)] public GearsetItemFlag Flags;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x188)]
    public struct GearsetItemArray
    {
        public const int Length = 14;

        private fixed byte data[Length * GearsetItem.StructSize];

        public GearsetItem* this[int i]
        {
            get
            {
                if (i < 0 || i >= Length) return null;
                fixed (byte* p = data)
                {
                    return (GearsetItem*)(p + sizeof(GearsetItem) * i);
                }
            }
        }
    }
}
