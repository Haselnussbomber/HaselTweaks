using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 80 8B ?? ?? ?? ?? ?? B9
[StructLayout(LayoutKind.Explicit, Size = 0x2D8)]
public unsafe partial struct AddonMYCWarResultNotebook
{
    [StructLayout(LayoutKind.Explicit)]
    public struct VTable
    {
        [FieldOffset(0x8 * 2)]
        public unsafe delegate* unmanaged[Stdcall]<AddonMYCWarResultNotebook*, AtkEventType, int, AtkEvent*, IntPtr> ReceiveEvent;
    }

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0)] public VTable* vtbl;

    [FieldOffset(0x240)] public AtkCollisionNode* DescriptionCollisionNode;

    [FieldOffset(0x254)] public int MaxNoteIndex;
    [FieldOffset(0x258)] public int CurrentNoteIndex;
    [FieldOffset(0x25C)] public int CurrentPageIndex;
}
