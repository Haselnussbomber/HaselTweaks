using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A8)]
public unsafe partial struct CharaViewStatus : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public uint MainhandItemID;
    [FieldOffset(0x2A4)] public bool DrawWeapon;

    public static CharaViewStatus* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewStatus>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ?? 48 89 03 48 8B C3 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 83 C4 20 5B C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);

    [VirtualFunction(10)]
    public partial void Vf10(bool a2, CharaViewGameObject* gameObject);
}
