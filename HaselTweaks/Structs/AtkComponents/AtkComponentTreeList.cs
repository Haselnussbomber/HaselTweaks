using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x228)]
public unsafe partial struct AtkComponentTreeList
{
    [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentTreeList Base;

    [FieldOffset(0x1A8)] public StdVector<Pointer<AtkComponentTreeListItem>> Items;

    [MemberFunction("E8 ?? ?? ?? ?? 44 38 60 45")]
    public readonly partial AtkComponentTreeListItem* GetItem(uint index);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkComponentTreeListItem
{
    [FieldOffset(0)] public AtkComponentTreeListItemData* Data; // StdVector?

    [FieldOffset(0x18)] public byte** Title;

    [FieldOffset(0x30)] public AtkComponentListItemRenderer* Renderer;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkComponentTreeListItemData
{
    [FieldOffset(0)] public AtkComponentTreeListItemType Type;
    // [FieldOffset(0x4)] public uint ???;
    [FieldOffset(0x8)] public uint RowId; // perhaps?
}

public enum AtkComponentTreeListItemType : uint
{
    Leaf = 0,
    LastLeafInGroup = 1,
    Group = 2,
}
