using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

public unsafe partial struct HaselShellCommandModule
{
    public static HaselShellCommandModule* Instance() => (HaselShellCommandModule*)UIModule.Instance()->GetRaptureShellModule();

    [MemberFunction("E8 ?? ?? ?? ?? FE 86 ?? ?? ?? ?? C7 86")]
    public readonly partial byte* ExecuteCommandInner(Utf8String* command, UIModule* uiModule);

    public static void ExecuteCommand(string command)
    {
        var cmd = stackalloc Utf8String[1];
        cmd->Ctor();
        cmd->SetString(command);
        Instance()->ExecuteCommandInner(cmd, UIModule.Instance());
        cmd->Dtor();
    }
}
