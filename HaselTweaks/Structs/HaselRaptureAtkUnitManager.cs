using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? 40 88 B7 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 87 ?? ?? ?? ?? 48 8D 9F"
[VTableAddress("48 8D 8B ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 05", 10)]
[StructLayout(LayoutKind.Explicit, Size = 0x9D10)]
public unsafe partial struct HaselRaptureAtkUnitManager
{
    public static HaselRaptureAtkUnitManager* Instance()
        => (HaselRaptureAtkUnitManager*)&RaptureAtkModule.Instance()->RaptureAtkUnitManager;

    [FieldOffset(0x9CF8)] public UIModule.UiFlags UiFlags;

    [VirtualFunction(6)]
    public readonly partial bool Vf6(nint a2);
}
