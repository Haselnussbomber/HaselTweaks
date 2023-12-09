using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

// https://github.com/aers/FFXIVClientStructs/pull/722
// Client::System::Input::ClipBoard
//   Client::System::Input::ClipBoardInterface
// ctor "E8 ?? ?? ?? ?? 48 8B C3 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 83 C4 20"
[VTableAddress("48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 83 C1 08 E8 ?? ?? ?? ?? 48 8D 4B 70 E8 ?? ?? ?? ?? 48 8B C3", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0xD8)]
public unsafe partial struct HaselClipBoard
{
    [FieldOffset(0x8)] public Utf8String SystemClipboardText;
    [FieldOffset(0x70)] public Utf8String CopyStagingText;

    [VirtualFunction(1)]
    public partial void WriteToSystemClipboard(Utf8String* stringToCopy, Utf8String* copiedStringWithoutPayload);

    [VirtualFunction(2)]
    public partial Utf8String* GetSystemClipboardText();

    [VirtualFunction(3)]
    public partial void SetCopyStagingText(Utf8String* utf8String);

    [VirtualFunction(4)]
    public partial void ApplyCopyStagingText();
}
