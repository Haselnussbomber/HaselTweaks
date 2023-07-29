using HaselTweaks.Structs;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter
{
    public Character* Character { get; }
    public HaselCharacter* HaselCharacter => (HaselCharacter*)Character;
    public ulong ContentId { get; }
    public ushort TerritoryType { get; }
    public byte ClassJobId { get; }

    public CharaSelectCharacter(Character* character, AgentLobby.CharaSelectEntry* entry)
    {
        Character = character;
        ContentId = entry->ContentId;
        TerritoryType = entry->CharacterInfo.TerritoryType;
        ClassJobId = entry->CharacterInfo.CurrentClassJobId;
    }
}
