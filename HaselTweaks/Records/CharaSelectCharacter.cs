using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter
{
    public Character* Character { get; }
    public ulong ContentId { get; }
    public ushort TerritoryType { get; }
    public byte ClassJobId { get; }

    public CharaSelectCharacter(Character* character, CharaSelectCharacterEntry* entry)
    {
        Character = character;
        ContentId = entry->ContentId;
        TerritoryType = entry->ClientSelectData.TerritoryType;
        ClassJobId = entry->ClientSelectData.CurrentClass;
    }
}
