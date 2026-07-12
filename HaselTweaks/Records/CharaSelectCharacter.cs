using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Records;

public unsafe class CharaSelectCharacter(Character* character, CharaSelectCharacterEntry* entry)
{
    public Character* Character { get; } = character;
    public ulong ContentId { get; } = entry->ContentId;
    public ushort TerritoryType { get; } = entry->ClientSelectData.TerritoryType;
    public byte ClassJobId { get; } = entry->ClientSelectData.CurrentClass;
    public bool IsEmotePlayed { get; set; }
}
