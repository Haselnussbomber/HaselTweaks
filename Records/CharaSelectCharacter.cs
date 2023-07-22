using HaselTweaks.Structs;
using CSCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using HaselCharacter = HaselTweaks.Structs.Character;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter
{
    public CSCharacter* Character { get; }
    public HaselCharacter* HaselCharacter => (HaselCharacter*)Character;
    public ulong ContentId { get; }
    public ushort TerritoryType { get; }
    public byte ClassJobId { get; }

    public CharaSelectCharacter(CSCharacter* character, AgentLobby.CharaSelectEntry* entry)
    {
        Character = character;
        ContentId = entry->ContentId;
        TerritoryType = entry->CharacterInfo.TerritoryType;
        ClassJobId = entry->CharacterInfo.CurrentClassJobId;
    }
}
