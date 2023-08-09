using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter
{
    public Character* Character { get; }
    public HaselCharacter* HaselCharacter => (HaselCharacter*)Character;
    public ulong ContentId { get; }
    public ushort TerritoryType { get; }
    public byte ClassJobId { get; }

    public CharaSelectCharacter(Character* character, CharaSelectCharacterEntry* entry)
    {
        Character = character;
        ContentId = entry->ContentId;
        TerritoryType = entry->CharacterInfo.TerritoryType;
        ClassJobId = entry->CharacterInfo.CurrentClassJobId;
    }
}
