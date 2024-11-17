using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Structs.Agents;

[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public unsafe struct AgentMiragePrismPrismSetConvert {
    [FieldOffset(0x00)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public AgentMiragePrismPrismSetConvertData* Data;
}

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x590)] // not sure
public partial struct AgentMiragePrismPrismSetConvertData {
    //[FieldOffset(0x0C)] public uint NeededPrisms?;
    [FieldOffset(0x14)] public int ContextMenuItemIndex;
    [FieldOffset(0x18)] public uint SomethingItemSelect;
    [FieldOffset(0x20)] public uint PrismHave;
    // [FieldOffset(0x24)] public uint NumSets?;
    // [FieldOffset(0x28)] public uint NumSets?;
    [FieldOffset(0x30), FixedSizeArray] internal FixedSizeArray5<ItemSet> _itemSets;
    [FieldOffset(0x288)] public uint NumItemsInSet;
    [FieldOffset(0x28C), FixedSizeArray] internal FixedSizeArray5<ItemSetItem> _items;
    [FieldOffset(0x380)] public Utf8String N0005AE1A;
    [FieldOffset(0x3F8)] public Utf8String N0005AE57;
    // ...
}

[StructLayout(LayoutKind.Explicit, Size = 0x1C)] // not sure
public struct ItemSetItem
{
    [FieldOffset(0x00)] public uint ItemId;
    [FieldOffset(0x04)] public uint IconId;
    [FieldOffset(0x08)] public InventoryType InventoryType;
    [FieldOffset(0x10)] public uint Slot;
}

[StructLayout(LayoutKind.Explicit, Size = 0x78)] // not sure
public struct ItemSet
{
    [FieldOffset(0x00)] public uint ItemId;
    [FieldOffset(0x04)] public uint IconId;
    [FieldOffset(0x08)] public Utf8String Name;
}
