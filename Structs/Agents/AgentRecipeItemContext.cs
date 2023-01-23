using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "74 34 48 8D 0D ?? ?? ?? ?? 40 88 68 08"
[Agent(AgentId.RecipeItemContext)]
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct AgentRecipeItemContext
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x28)] public uint ResultItemId;
}
