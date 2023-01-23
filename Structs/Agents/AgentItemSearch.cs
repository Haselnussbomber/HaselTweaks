using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 33 ED C6 41 08 00 48 89 69 18"
[Agent(AgentId.ItemSearch)]
[StructLayout(LayoutKind.Explicit, Size = 0x37F0)]
public unsafe struct AgentItemSearch
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    public AddonItemSearch* GetAddon() =>
        AgentInterface.IsAgentActive() && IsAddonReady(AgentInterface.AddonId)
            ? GetAddon<AddonItemSearch>(AgentInterface.AddonId)
            : null;
}
