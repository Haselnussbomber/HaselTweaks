using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = RaptureLogModule.StructSize)]
public unsafe partial struct HaselRaptureLogModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 41 83 EC 01")]
    public unsafe partial uint FormatLogMessage(uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex);
}
