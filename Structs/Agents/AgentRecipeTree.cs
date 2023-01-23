using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 33 C0 48 89 43 28 89 43 30 88 43 34"
[Agent(AgentId.RecipeTree)]
[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct AgentRecipeTree
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    public AddonRecipeTree* GetAddon() =>
        AgentInterface.IsAgentActive() && IsAddonReady(AgentInterface.AddonId)
            ? GetAddon<AddonRecipeTree>(AgentInterface.AddonId)
            : null;
}
