using HaselTweaks.Structs;
using CSCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using HaselCharacter = HaselTweaks.Structs.Character;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter
{
    public CSCharacter* Character { get; }
    public HaselCharacter* HaselCharacter => (HaselCharacter*)Character;
    public ulong ContentId { get; }
    public ushort TerritoryId { get; }
    public byte ClassJobId { get; }

    public CharaSelectCharacter(CSCharacter* character, CharaSelectCharacterEntry* entry)
    {
        Character = character;
        ContentId = entry->ContentId;
        TerritoryId = entry->ParsedData.TerritoryId;
        ClassJobId = entry->ParsedData.CurrentClassJobId;
    }
}
