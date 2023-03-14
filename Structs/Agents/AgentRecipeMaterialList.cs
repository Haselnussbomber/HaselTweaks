using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 C7 43 ?? ?? ?? ?? ?? 48 89 03 33 C0 89 43 28"
[Agent(AgentId.RecipeMaterialList)]
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe partial struct AgentRecipeMaterialList
{
    [FieldOffset(0)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public ushort RecipeId;

    [FieldOffset(0x2C)] public uint Amount;

    [FieldOffset(0x34)] public bool WindowLocked;
    [FieldOffset(0x38)] public RecipeData* Recipe;

    [StructLayout(LayoutKind.Explicit)]
    public struct RecipeData
    {
        [FieldOffset(0x90)] public uint RecipeId;
        [FieldOffset(0x94)] public uint ResultItemId;
        [FieldOffset(0x98)] public uint ResultAmount;
        [FieldOffset(0x9C)] public uint ResultItemIconId;
        [FieldOffset(0xA0)] public Utf8String ItemName;
    }

    [MemberFunction("E8 ?? ?? ?? ?? EB B1 48 8B 4B 28")]
    public partial void OpenByRecipeId(uint recipeId, uint amount = 1);
}
