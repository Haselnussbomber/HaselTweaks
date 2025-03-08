using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = ActionManager.StructSize)]
public unsafe partial struct HaselActionManager
{
    [MemberFunction("E8 ?? ?? ?? ?? 41 83 FF 04 0F 84")]
    public partial void OpenCastBar(BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7, float a8);
}
