using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x3488)]
public unsafe partial struct HaselRaptureLogModule
{
    public static HaselRaptureLogModule* Instance() => (HaselRaptureLogModule*)RaptureLogModule.Instance();

    [MemberFunction("E8 ?? ?? ?? ?? 32 C0 EB 17")]
    public readonly partial void ShowLogMessageUInt(uint logMessageID, uint value);
}
