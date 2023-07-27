using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselRaptureLogModule
{
    public static HaselRaptureLogModule* Instance()
        => (HaselRaptureLogModule*)UIModule.Instance()->GetRaptureLogModule();

    [MemberFunction("E8 ?? ?? ?? ?? 32 C0 EB 17")]
    public partial void ShowLogMessageUInt(uint logMessageID, uint value);
}
