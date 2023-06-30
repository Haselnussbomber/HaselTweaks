using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05"
[Agent(AgentId.RecipeNote)]
[StructLayout(LayoutKind.Explicit, Size = 0x560)]
public struct AgentRecipeNote
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x398)] public uint ContextMenuResultItemId;
}
