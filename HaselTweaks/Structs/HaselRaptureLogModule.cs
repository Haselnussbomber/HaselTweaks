using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x3488)]
public unsafe partial struct HaselRaptureLogModule
{
    [FieldOffset(0xF8)] public HaselRaptureTextModule* RaptureTextModule;

    [FieldOffset(0x100)] public HaselAtkFontCodeModule* AtkFontCodeModule;

    [FieldOffset(0x108), FixedSizeArray] internal FixedSizeArray10<Utf8String> _tempParseMessage;

    [FieldOffset(0x520)] public nint LogKindSheet;

    [MemberFunction("E8 ?? ?? ?? ?? 89 43 28 41 FF CD")]
    public unsafe partial uint FormatLogMessage(uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex);
}
