using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? EB 03 48 8B C5 45 33 C9 48 89 47 20"
[Agent(AgentId.Lobby)]
[VTableAddress("48 8D 05 ?? ?? ?? ?? 48 89 71 18 48 89 01", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x1DD0)]
public unsafe partial struct AgentLobby
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x40)] public Unk40Struct Unk40;

    [FieldOffset(0x10E1)] public sbyte SelectedCharacterIndex;
    [FieldOffset(0x10E8)] public ulong SelectedCharacterContentId;

    [FieldOffset(0x10F2)] public short Unk10F2;

    [MemberFunction("E8 ?? ?? ?? ?? 66 44 89 B6")]
    public static partial void CleanupCharaSelectCharacters();

    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 07 C6 87 ?? ?? ?? ?? ?? 48 8B 4C 24")]
    public readonly partial void UpdateCharaSelectDisplay(sbyte index, bool a2);

    [MemberFunction("E8 ?? ?? ?? ?? EB 4A 84 C0")]
    public readonly partial void OpenLoginWaitDialog(int position);

    [StaticAddress("48 89 2D ?? ?? ?? ?? 48 8B 6C 24", 3, true)]
    public static partial Character* GetCurrentCharaSelectCharacter();

    [StaticAddress("4C 8D 3D ?? ?? ?? ?? 48 8B DA", 3)]
    public static partial CharaSelectCharacterList* GetCharaSelectCharacterList();

    public partial struct CharaSelectCharacterList
    {
        [FixedSizeArray<CharaSelectCharacter>(40)]
        public fixed byte Characters[40 * 0x10];
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct CharaSelectCharacter
    {
        [FieldOffset(0)] public ulong ContentId;
        [FieldOffset(8)] public short ObjectIndex;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x9C0)]
    public partial struct Unk40Struct
    {
        [FieldOffset(0x858)] public StdVector<Pointer<CharaSelectEntry>> CharaSelectEntries;

        [MemberFunction("E8 ?? ?? ?? ?? 48 8B 48 08 49 89 8C 24")]
        public partial CharaSelectEntry* GetCharacterEntryByIndex(int a2, int agentLobbyUnk10F2, int index);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x6F8)]
    public struct CharaSelectEntry
    {
        [FieldOffset(0x8)] public ulong ContentId;

        [FieldOffset(0x18)] public ulong CurrentWorldId;
        [FieldOffset(0x1A)] public ulong HomeWorldId;

        [FieldOffset(0x2C)] public fixed byte Name[32];
        [FieldOffset(0x4C)] public fixed byte CurrentWorldName[32];
        [FieldOffset(0x6C)] public fixed byte HomeWorldName[32];
        [FieldOffset(0x8C)] public fixed byte RawJson[1024];

        [FieldOffset(0x4A0)] public StdVector<Pointer<CharaSelectRetainerInfo>> RetainerInfo;

        [FieldOffset(0x4C0)] public CharaSelectCharacterInfo CharacterInfo; // x2?
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x1E2)]
    public struct CharaSelectCharacterInfo
    {
        [FieldOffset(0x8)] public fixed byte Name[32];
        [FieldOffset(0x28)] public byte CurrentClassJobId;

        [FieldOffset(0x2A)] public fixed ushort ClassJobLevelArray[30];
        [FieldOffset(0x66)] public byte Race;
        [FieldOffset(0x67)] public byte Tribe;
        [FieldOffset(0x68)] public byte Sex;
        [FieldOffset(0x69)] public byte BirthMonth;
        [FieldOffset(0x6A)] public byte BirthDay;
        [FieldOffset(0x6B)] public byte GuardianDeity;
        [FieldOffset(0x6C)] public byte FirstClass;

        [FieldOffset(0x6E)] public ushort ZoneId;
        [FieldOffset(0x70)] public ushort TerritoryType;
        [FieldOffset(0x72)] public ushort ContentFinderCondition;
        [FieldOffset(0x74)] public CustomizeData CustomizeData;

        [FieldOffset(0x90)] public WeaponModelId MainHandModel;
        [FieldOffset(0x98)] public WeaponModelId OffHandModel;
        [FieldOffset(0xA0)] public EquipmentModelId Head;
        [FieldOffset(0xA4)] public EquipmentModelId Body;
        [FieldOffset(0xA8)] public EquipmentModelId Hands;
        [FieldOffset(0xAC)] public EquipmentModelId Legs;
        [FieldOffset(0xB0)] public EquipmentModelId Feet;
        [FieldOffset(0xB4)] public EquipmentModelId Ears;
        [FieldOffset(0xB8)] public EquipmentModelId Neck;
        [FieldOffset(0xBC)] public EquipmentModelId Wrists;
        [FieldOffset(0xC0)] public EquipmentModelId RingRight;
        [FieldOffset(0xC4)] public EquipmentModelId RingLeft;
        [FieldOffset(0xC8)] public uint MainHandItemId;
        [FieldOffset(0xCC)] public uint OffHandItemId;
        [FieldOffset(0xD0)] public uint SoulstoneItemId;
        [FieldOffset(0xD4)] public uint RemakeFlag;
        [FieldOffset(0xD8)] public ConfigFlags ConfigFlags;
        [FieldOffset(0xDA)] public byte VoiceId;
        [FieldOffset(0xDB)] public fixed byte WorldName[32]; // always empty?

        [FieldOffset(0x100)] public ulong LoginStatus;
        [FieldOffset(0x108)] public byte IsOutTerritory;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x58)]
    public struct CharaSelectRetainerInfo
    {
        [FieldOffset(0)] public ulong RetainerId;
        [FieldOffset(0x8)] public ulong OwnerContentId;

        [FieldOffset(0x18)] public fixed byte Name[32];
    }

    [Flags]
    public enum ConfigFlags : ushort
    {
        None = 0,
        HideHead = 0x01,
        HideWeapon = 0x02,
        HideLegacyMark = 0x04,
        // ? = 0x08,
        StoreNewItemsInArmouryChest = 0x10,
        StoreCraftedItemsInInventory = 0x20,
        CloseVisor = 0x40,
        // ? = 0x80
    };
}
