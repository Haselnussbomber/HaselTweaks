using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Agents;

// ctor "E8 ?? ?? ?? ?? EB 03 48 8B C5 33 D2 48 89 87 ?? ?? ?? ?? 45 33 C9 4C 8B C6 8D 4A 30 E8 ?? ?? ?? ?? 48 85 C0 74 0D 48 8B D3 48 8B C8 E8 ?? ?? ?? ?? EB 03 48 8B C5 45 33 C9 48 89 87 ?? ?? ?? ?? 4C 8B C6 33 D2 B9 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 0D 48 8B D3 48 8B C8 E8 ?? ?? ?? ?? EB 03 48 8B C5 45 33 C9 48 89 87 ?? ?? ?? ?? 4C 8B C6 33 D2 B9 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 0D 48 8B D3 48 8B C8 E8 ?? ?? ?? ?? EB 03 48 8B C5 33 D2 48 89 87 ?? ?? ?? ?? 45 33 C9 4C 8B C6 8D 4A 30 E8 ?? ?? ?? ?? 48 85 C0 74 0D 48 8B D3 48 8B C8 E8 ?? ?? ?? ?? EB 03 48 8B C5 33 D2 48 89 87 ?? ?? ?? ?? 45 33 C9 4C 8B C6 8D 4A 60"
[Agent(AgentId.MJIGatheringNoteBook)]
[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public unsafe struct AgentMJIGatheringNoteBook
{
    [FieldOffset(0)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public AgentMJIGatheringNoteBook_Data* Data;

    public AddonAetherCurrent* GetAddon() =>
        AgentInterface.IsAgentActive() && IsAddonReady(AgentInterface.AddonId)
            ? GetAddon<AddonAetherCurrent>(AgentInterface.AddonId)
            : null;
}

[StructLayout(LayoutKind.Explicit, Size = 0x1A0)]
public unsafe struct AgentMJIGatheringNoteBook_Data
{
    [FieldOffset(0)] public uint Status;

    [FieldOffset(0x80)] public AgentMJIGatheringNoteBook_Data_GatherItem* GatherItems;

    [FieldOffset(0x98)] public AgentMJIGatheringNoteBook_Data_GatherItem** GatherItemPtrs; // sorted

    [FieldOffset(0x198)] public uint SelectedItemIndex;
    [FieldOffset(0x19C)] public byte Flags;
}

[StructLayout(LayoutKind.Explicit, Size = 0x88)]
public unsafe struct AgentMJIGatheringNoteBook_Data_GatherItem
{
    [FieldOffset(0x00)] public ushort Radius;
    [FieldOffset(0x02)] public short X;
    [FieldOffset(0x04)] public short Y;
    [FieldOffset(0x06)] public byte Unknown2; // from the sheet

    [FieldOffset(0x08)] public uint ItemId;
    [FieldOffset(0x0C)] public byte Sort;

    [FieldOffset(0x10)] public uint Icon;
    [FieldOffset(0x14)] public byte RowId; // offset by 1

    [FieldOffset(0x18)] public Utf8String Name;
}
