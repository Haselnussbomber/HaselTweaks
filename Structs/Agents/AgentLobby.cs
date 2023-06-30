using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? EB 03 48 8B C5 45 33 C9 48 89 47 20"
[Agent(AgentId.Lobby)]
[VTableAddress("48 8D 05 ?? ?? ?? ?? 48 89 71 18 48 89 01", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x1DD0)]
public unsafe partial struct AgentLobby
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x10E1)] public byte CharaSelectSelectedCharacterIndex;
    [FieldOffset(0x10E8)] public ulong CharaSelectSelectedCharacterContentId;

    [FieldOffset(0x10F2)] public short Unk10F2;

    [MemberFunction("E8 ?? ?? ?? ?? 66 44 89 B6")]
    public static partial void CleanupCharaSelectCharacters();

    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 07 C6 87 ?? ?? ?? ?? ?? 48 8B 4C 24")]
    public readonly partial void UpdateCharaSelectDisplay(sbyte index, bool a2);

    [MemberFunction("E8 ?? ?? ?? ?? EB 4A 84 C0")]
    public readonly partial void OpenLoginWaitDialog(int position);
}
