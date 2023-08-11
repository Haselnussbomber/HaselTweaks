using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// located at RaptureAtkUnitManager + 0x9CB0
[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct WindowContextMenuHandler
{
    [MemberFunction("48 89 6C 24 ?? 48 89 54 24 ?? 56 41 54")]
    public readonly partial AtkValue* Callback(AtkValue* result, nint a3, long a4, long eventParam);
}
