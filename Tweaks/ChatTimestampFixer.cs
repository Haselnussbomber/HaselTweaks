using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Text;

namespace HaselTweaks.Tweaks;

public unsafe class ChatTimestampFixer : BaseTweak
{
    public override string Name => "Chat Timestamp Fixer";
    public override bool CanLoad => false; // TODO: fix it first

    [Signature("E8 ?? ?? ?? ?? 4C 63 6C 24 ??", DetourName = nameof(StringFormatDetour))]
    private Hook<FunctionDelegate>? Hook { get; init; }
    private delegate IntPtr FunctionDelegate(IntPtr a1, int addonRowId, int numberArg);

    public override void Enable()
    {
        base.Enable();
        PluginLog.Debug($"[ChatTimestampFixer] Hook.Address: {Hook?.Address:X}");
        Hook?.Enable();
    }

    public override void Disable()
    {
        base.Disable();
        Hook?.Disable();
    }

    public override void Dispose()
    {
        base.Dispose();
        Hook?.Dispose();
    }

    private IntPtr StringFormatDetour(IntPtr a1, int addonRowId, int numberArg)
    {
        // completely replace output for Addon#7840
        if (addonRowId == 7840)
        {
            var time = DateTime.UnixEpoch.AddSeconds(numberArg).ToLocalTime();
            var str = $"[{time:HH:mm}] ";
            //MemoryHelper.WriteString(result, str);

            var ptr = a1 + 0x9C0;
            fixed (byte* @string = Encoding.UTF8.GetBytes(str + "\0"))
                ((Utf8String*)ptr)->SetString(@string);
            return ptr;
        }

        return Hook!.Original(a1, addonRowId, numberArg);
    }
}
