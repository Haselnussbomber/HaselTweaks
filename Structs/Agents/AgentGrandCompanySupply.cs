using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 07 48 8D 05 ?? ?? ?? ?? 48 89 47 28 33 C0 48 89 47 40 48 89 47 48 66 89 47 50 89 47 54 66 89 47 58 48 89 5F 38 48 8B 5C 24 ?? C6 47 30 00 48 89 47 60 48 89 47 68"
[Agent(AgentId.GrandCompanySupply)]
[StructLayout(LayoutKind.Explicit, Size = 0x98)]
public unsafe struct AgentGrandCompanySupply
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x84)] public uint NextReset; // unix timestamp

    [FieldOffset(0x90)] public ushort TabIndex;
    [FieldOffset(0x92)] public byte ItemOrder;
    [FieldOffset(0x93)] public byte ItemFilter;

    public AddonGrandCompanySupplyList* GetAddon() =>
        AgentInterface.IsAgentActive() && IsAddonReady(AgentInterface.AddonId)
            ? GetAddon<AddonGrandCompanySupplyList>(AgentInterface.AddonId)
            : null;
}
