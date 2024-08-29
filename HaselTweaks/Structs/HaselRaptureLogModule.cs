using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x3488)]
public unsafe partial struct HaselRaptureLogModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 41 83 EC 01")]
    public unsafe partial uint FormatLogMessage(uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex);
}
