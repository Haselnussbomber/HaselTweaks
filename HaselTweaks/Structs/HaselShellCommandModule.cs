using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Utils;

namespace HaselTweaks.Structs;

public unsafe partial struct HaselShellCommandModule
{
    public static HaselShellCommandModule* Instance() => (HaselShellCommandModule*)UIModule.Instance()->GetRaptureShellModule();

    [MemberFunction("E8 ?? ?? ?? ?? FE 86 ?? ?? ?? ?? C7 86")]
    public readonly partial byte* ExecuteCommandInner(Utf8String* command, UIModule* uiModule);

    public static void ExecuteCommand(string command)
    {
        using var cmd = new DisposableUtf8String(command);
        Instance()->ExecuteCommandInner(cmd, UIModule.Instance());
    }
}
