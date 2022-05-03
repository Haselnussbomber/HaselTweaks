using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 D2 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 83 ?? ?? ?? ?? 48 89 90
[StructLayout(LayoutKind.Explicit, Size = 0x830)]
public unsafe struct AddonCharacterClass
{
    public const int NUM_CLASSES = 31;

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x220)] public BaseComponentNodesArray BaseComponentNodes;
    [FieldOffset(0x318)] public AtkComponentButton* TabsNode;
    [FieldOffset(0x320)] public AtkTextNode* CurrentXPTextNode;
    [FieldOffset(0x328)] public AtkTextNode* MaxXPTextNode;
    [FieldOffset(0x330)] public AtkTextNode* CurrentDesynthesisLevelTextNode;
    [FieldOffset(0x338)] public AtkTextNode* MaxDesynthesisLevelTextNode;
    [FieldOffset(0x340)] public ClassEntriesArray ClassEntries;

    [StructLayout(LayoutKind.Sequential, Size = NUM_CLASSES * 8)]
    public struct BaseComponentNodesArray
    {
        private fixed byte data[NUM_CLASSES * 8];
        public AtkComponentBase* this[int i]
        {
            get
            {
                if (i < 0 || i > NUM_CLASSES) return null;
                fixed (byte* p = data)
                {
                    var ptr = (IntPtr*)(p + i * 8);
                    if (ptr == null) return null;
                    return (AtkComponentBase*)*ptr;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public struct ClassEntry
    {
        [FieldOffset(0x00)] public uint Level;
        [FieldOffset(0x04)] public uint CurrentXP;
        [FieldOffset(0x08)] public uint LevelMaxXP;
        [FieldOffset(0x10)] public IntPtr DesynthesisLevel;
        [FieldOffset(0x18)] public IntPtr TooltipText;
        [FieldOffset(0x20)] public bool IsMaxLevel;
    }

    [StructLayout(LayoutKind.Sequential, Size = NUM_CLASSES * 0x28)]
    public struct ClassEntriesArray
    {
        private fixed byte data[NUM_CLASSES * 0x28];
        public ClassEntry* this[int i]
        {
            get
            {
                if (i < 0 || i > NUM_CLASSES) return null;
                fixed (byte* p = data)
                {
                    return (ClassEntry*)(p + sizeof(ClassEntry) * i);
                }
            }
        }
    }
}
