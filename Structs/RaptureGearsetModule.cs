using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 45 33 F6 48 89 51 10 48 8D 05 ?? ?? ?? ?? 4C 89 71 08 49 8B D8"
[StructLayout(LayoutKind.Explicit, Size = 0xB670)]
public unsafe struct RaptureGearsetModule
{
    public static RaptureGearsetModule* Instance() => (RaptureGearsetModule*)Framework.Instance()->GetUiModule()->GetRaptureGearsetModule();

    [FieldOffset(0x00)] public void* vtbl;
    [FieldOffset(0x30)] public fixed byte ModuleName[16];
    [FieldOffset(0x48)] public GearsetArray Gearsets;

    [Flags]
    public enum GearsetFlag : byte
    {
        /// <remarks>
        /// Set when this gearset entry has been created.
        /// </remarks>
        Exists = 1 << 0,

        Unknown1 = 1 << 1,

        /// <remarks>
        /// Shows a red exclamation mark with message "The specified main arm was missing from your Armoury Chest."
        /// </remarks>
        MainHandMissing = 1 << 2,

        /// <remarks>
        /// Set when "Display Headgear" is ticked.
        /// </remarks>
        DisplayHeadgear = 1 << 3,

        /// <remarks>
        /// Set when "Display Sheathed Arms" is ticked.
        /// </remarks>
        DisplaySheathedArms = 1 << 4,

        /// <remarks>
        /// Set when "Manually adjust visor (select gear only)." is ticked.
        /// </remarks>
        ManuallyAdjustVisor = 1 << 5,

        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
    }

    [Flags]
    public enum GearsetItemFlag : byte
    {
        /// <remarks>
        /// Shows a yellow exclamation mark with message "One or more items were missing from your Armoury Chest."
        /// </remarks>
        MissingItem = 1 << 0,

        Unknown1 = 1 << 1,

        /// <remarks>
        /// Shows a gray exclamation mark with message "One or more items were not the specified color."
        /// </remarks>
        DifferentColor = 1 << 2,

        /// <remarks>
        /// Shows a gray exclamation mark with message "One or more items were not melded with the specified materia."
        /// </remarks>
        DifferentMateria = 1 << 3,

        /// <remarks>
        /// Shows a gray exclamation mark with message "One or more items did not have the specified appearance."
        /// </remarks>
        DifferentAppearance = 1 << 4,

        Unknown5 = 1 << 5,
        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x1C0)]
    public struct Gearset
    {
        internal const int StructSize = 0x1C0;

        [FieldOffset(0x00)] public byte ID;
        [FieldOffset(0x01)] public fixed byte RawName[0x2F];

        /// <remarks>Row ID of ClassJob sheet</remarks>
        [FieldOffset(0x31)] public byte ClassJob;

        [FieldOffset(0x32)] public byte GlamourPlate;

        [FieldOffset(0x34)] public ushort ItemLevel;

        /// <remarks>Internal Portrait ID (not the sorting order)</remarks>
        [FieldOffset(0x36)] public byte InstantPortrait;

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

    [StructLayout(LayoutKind.Explicit, Size = 0x1C)]
    public struct GearsetItem
    {
        internal const int StructSize = 0x1C;

        /// <remarks>Row ID of Item sheet</remarks>
        [FieldOffset(0x00)] public uint ItemID;

        /// <remarks>Row ID of Item sheet</remarks>
        [FieldOffset(0x04)] public uint GlamourItemID;

        /// <remarks>Row ID of Stain sheet</remarks>
        [FieldOffset(0x08)] public byte Stain;

        /// <remarks>Row ID of Materia sheet</remarks>
        [FieldOffset(0x0A)] public fixed ushort Materia[5];

        /// <remarks>Index for Item column of Materia sheet</remarks>
        [FieldOffset(0x14)] public fixed byte MateriaItem[5];

        [FieldOffset(0x19)] public GearsetItemFlag Flags;
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
