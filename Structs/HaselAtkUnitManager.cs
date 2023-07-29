using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselAtkUnitManager
{
    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6")]
    public readonly partial void AddonFinalize(AtkUnitBase** unitBasePtr);
}
