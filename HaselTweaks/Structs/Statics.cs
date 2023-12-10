using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselTweaks.Structs;

public unsafe partial struct Statics
{
    [MemberFunction("E8 ?? ?? ?? ?? 8B 44 24 78 89 44 24 44")]
    public static partial void GetTodoArgs(EventHandler* questEventHandler, BattleChara* localPlayer, int i, uint* numHave, uint* numNeeded, uint* itemId);

    [MemberFunction("66 83 F9 1E 0F 83")]
    public static partial nint UpdateQuestWork(ushort index, nint questData, bool a3, bool a4, bool a5);

    [MemberFunction("80 F9 07 77 10")]
    public static partial byte IsGatheringPointRare(byte gatheringPointType);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ?? 48 8D 8C 24 ?? ?? ?? ?? 48 8B D0 E8 ?? ?? ?? ?? 8B 4E 08")]
    public static partial byte* GetGatheringPointName(RaptureTextModule** module, byte gatheringTypeId, byte gatheringPointType);
}
