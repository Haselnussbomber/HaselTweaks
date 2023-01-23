using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 C7 43 ?? ?? ?? ?? ?? 48 89 03 33 C0 89 43 28"
[Agent(AgentId.RecipeMaterialList)]
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe struct AgentRecipeMaterialList
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    public AddonRecipeMaterialList* GetAddon() =>
        AgentInterface.IsAgentActive() && IsAddonReady(AgentInterface.AddonId)
            ? GetAddon<AddonRecipeMaterialList>(AgentInterface.AddonId)
            : null;
}
