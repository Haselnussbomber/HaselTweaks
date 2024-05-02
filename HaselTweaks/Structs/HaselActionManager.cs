using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Structs;

public unsafe partial struct HaselActionManager
{
    [MemberFunction("E8 ?? ?? ?? ?? 83 FE 04 74 58")]
    public partial void OpenCastBar(BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7);
}
