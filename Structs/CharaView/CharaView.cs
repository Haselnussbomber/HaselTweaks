using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using Lumina.Data.Parsing;

namespace HaselTweaks.Structs;

/*
 * clientObjectId:
 *  0 used in Character, PvPCharacter
 *  1 used in Inspect, CharaCard
 *  2 used in TryOn, GearSetPreview
 *  3 used in Colorant
 *  4 used in BannerList, BannerEdit
 */

public enum CharaViewState : uint
{
    Ready = 6
}

[StructLayout(LayoutKind.Explicit, Size = 0x2C8)]
public unsafe partial struct CharaView : ICreatable
{
    [FieldOffset(0x8)] public CharaViewState State; // initialization state of KernelTexture, Camera etc. that happens in Render()
    [FieldOffset(0xC)] public uint ClientObjectId; // ClientObjectManager = non-networked objects, ClientObjectIndex + 40
    [FieldOffset(0x10)] public uint ClientObjectIndex;
    [FieldOffset(0x14)] public uint CameraType; // turns portrait ambient/directional lighting on/off
    [FieldOffset(0x18)] public IntPtr CameraManager;
    [FieldOffset(0x20)] public IntPtr Camera;
    [FieldOffset(0x28)] public IntPtr Unk28;
    [FieldOffset(0x30)] public IntPtr Agent; // for example: AgentTryOn
    [FieldOffset(0x38)] public IntPtr AgentCallbackReady; // if set, called when State changes to Ready
    [FieldOffset(0x40)] public IntPtr AgentCallback; // not investigated, used inside vf7 and vf11
    [FieldOffset(0x48)] public CharaViewCharacterData CharacterData;

    [FieldOffset(0xB8)] public uint UnkB8;
    [FieldOffset(0xBC)] public uint UnkBC;
    [FieldOffset(0xC0)] public float UnkC0;
    [FieldOffset(0xC4)] public float ZoomRatio;

    [FieldOffset(0xD0)] public fixed byte Items[0x20 * 14];

    [FieldOffset(0x2B8)] public bool CharacterDataCopied;
    [FieldOffset(0x2B9)] public bool CharacterLoaded;

    public Span<CharaViewItem> ItemSpan
    {
        get
        {
            fixed (byte* ptr = Items)
            {
                return new Span<CharaViewItem>(ptr, sizeof(CharaViewItem));
            }
        }
    }

    public static CharaView* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaView>();
    }

    [MemberFunction("E8 ?? ?? ?? ?? 41 80 A6 ?? ?? ?? ?? ?? 48 8D 05")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(IntPtr agent, int clientObjectId, IntPtr agentCallbackReady);

    [VirtualFunction(2)]
    public partial void Release();

    [VirtualFunction(3)]
    public partial void ResetPositions();

    [VirtualFunction(4)]
    public partial void Vf4(float a2);

    [VirtualFunction(5)]
    public partial void Vf5(float a2);

    [VirtualFunction(6)]
    public partial void Vf6(float a2, float a3);

    [VirtualFunction(7)]
    public partial byte Vf7(IntPtr a2); // called by Render()

    [VirtualFunction(8)]
    public partial void Vf8(); // noop

    [VirtualFunction(9)]
    public partial void Vf9(); // noop

    [VirtualFunction(10)]
    public partial void Vf10(); // noop

    [VirtualFunction(11)]
    public partial bool IsGameObjectReady(CharaViewGameObject* obj);

    [VirtualFunction(12)]
    public partial float Vf12(int a2, int a3);

    [MemberFunction("0F 10 02 0F 11 41 48")]
    public partial void SetCustomizeData(CharaViewCharacterData* data);

    [MemberFunction("E8 ?? ?? ?? ?? EB 27 8B D6")]
    public partial void Render(uint frameIndex);

    [MemberFunction("E8 ?? ?? ?? ?? 48 85 C0 75 05 0F 57 C9")]
    public partial CharaViewGameObject* GetGameObject();

    [MemberFunction("E8 ?? ?? ?? ?? 49 8D 4F 10 88 85")]
    public partial bool IsAnimationPaused();

    [MemberFunction("E8 ?? ?? ?? ?? B2 01 48 8B CE E8 ?? ?? ?? ?? 32 C0")]
    public partial void ToggleAnimationPaused(bool paused);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 47 28 BA")]
    public partial void UnequipGear(bool hasCharacterData = false, bool characterLoaded = true);

    [MemberFunction("E8 ?? ?? ?? ?? 45 33 DB FF C3")]
    public partial void SetItemSlotData(byte slotId, uint itemId, byte stain, int a5 = 0, byte a6 = 1); // maybe a5 is glamour id and a6 is a boolean that it should use glamour?

    [MemberFunction("E8 ?? ?? ?? ?? B1 01 0F B6 86")]
    public partial void ToggleDrawWeapon(bool drawn);
}

[StructLayout(LayoutKind.Explicit, Size = 0x68)]
public unsafe partial struct CharaViewCharacterData : ICreatable
{
    [FieldOffset(0)] public CustomizeData CustomizeData; // see Glamourer.Customization.CharacterCustomization
    [FieldOffset(0x1A)] public byte Unk1A;
    [FieldOffset(0x1B)] public byte Unk1B;
    [FieldOffset(0x1C)] public fixed uint ItemIds[14];
    [FieldOffset(0x54)] public fixed byte ItemStains[14];
    [FieldOffset(0x62)] public byte ClassJobId;
    [FieldOffset(0x63)] public bool VisorHidden;
    [FieldOffset(0x64)] public bool WeaponHidden;
    [FieldOffset(0x65)] public bool VisorClosed;
    [FieldOffset(0x66)] public byte Unk66;
    [FieldOffset(0x67)] public byte Unk67;

    public static CharaViewCharacterData* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewCharacterData>();
    }

    public static CharaViewCharacterData* CreateFromLocalPlayer()
    {
        var obj = Create();
        obj->ImportLocalPlayerEquipment();
        return obj;
    }

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8D 45 10 48 8B CF")]
    public partial void Ctor();

    [MemberFunction("E9 ?? ?? ?? ?? 41 0F B6 40 ?? 88 42 62")]
    public partial void ImportLocalPlayerEquipment();
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct CharaViewItem
{
    [FieldOffset(0x0)] public byte SlotId;
    [FieldOffset(0x1)] public byte EquipSlotCategory;
    [FieldOffset(0x2)] public byte EquipSlotCategory2;
    [FieldOffset(0x3)] public byte Stain;
    [FieldOffset(0x4)] public byte Stain2;
    [FieldOffset(0x5)] public byte Unk5;
    [FieldOffset(0x6)] public byte Unk6;
    [FieldOffset(0x7)] public byte Unk7;
    [FieldOffset(0x8)] public uint ItemId;
    [FieldOffset(0xC)] public uint ItemId2;
    [FieldOffset(0x10)] public Quad ModelMain;
    [FieldOffset(0x18)] public Quad ModelSub;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewGameObject // BattleChara?!
{
    // FYI: i have no idea what these should be called. haven't looked into them,
    // just came up with a name based on a function or 3 that i found
    [FieldOffset(0x1A8)] public CharaViewGameObjectClassJobSettings ClassJobSettings;
    [FieldOffset(0x6D0)] public CharaViewGameObjectDrawDataContainer DrawDataContainer;
    [FieldOffset(0x8D0)] public CharaViewGameObjectUnk8D0 Unk8D0;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewGameObjectClassJobSettings
{
    [MemberFunction("44 0F B6 49 ?? 88 51 38")]
    public partial void SetPoseClassJob(short classJobId);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewGameObjectDrawDataContainer
{
    [MemberFunction("E8 ?? ?? ?? ?? EB 14 40 80 FE 08")]
    public partial void ToggleVisorVisibility(uint a2, bool isHidden); // a2 = 0

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 2D")]
    public partial void ToggleVisorClosed(bool isClosed);

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8B 6C 24 ?? 0F BA E5 0A")]
    public partial void ToggleWeaponVisibility(bool isHidden);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharaViewGameObjectUnk8D0
{
    [MemberFunction("48 8B 41 08 F6 80 ?? ?? ?? ?? ?? 74 10 84 D2")]
    public partial void SetExpression(byte id);
}
