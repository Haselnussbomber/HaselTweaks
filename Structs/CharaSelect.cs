using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Structs;

public unsafe partial struct CharaSelect
{
    [StaticAddress("48 89 2D ?? ?? ?? ?? 48 8B 6C 24", 3)]
    public static partial BattleChara* GetCurrentCharacter();

    [StaticAddress("4C 8D 3D ?? ?? ?? ?? 48 8B DA", 3)]
    public static partial CharaSelectCharacters* GetCharacterList();

    [StructLayout(LayoutKind.Explicit)]
    public partial struct CharaSelectCharacters
    {
        [FieldOffset(0), FixedSizeArray<CharaSelectCharacter>(40)]
        public fixed byte Characters[40 * 0x10];
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct CharaSelectCharacter
    {
        [FieldOffset(0)] public ulong ContentId;
        [FieldOffset(8)] public short ObjectIndex;
    }
}
