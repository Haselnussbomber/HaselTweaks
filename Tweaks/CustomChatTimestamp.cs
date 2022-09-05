using System;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Tweaks;

public unsafe class CustomChatTimestamp : Tweak
{
    public override string Name => "Custom Chat Timestamp";
    public override string Description => "As it says, configurable chat timestamp format.";
    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.CustomChatTimestamp;

    public class Configuration
    {
        [ConfigField(Description = "This gets passed to C#'s DateTime.ToString() function.", DefaultValue = "[HH:mm] ")]
        public string Format = "[HH:mm] ";
    }

    [AutoHook, Signature("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87", DetourName = nameof(Detour))]
    private Hook<DetourDelegate> Hook { get; init; } = null!;
    private delegate byte* DetourDelegate(IntPtr a1, ulong addonRowId, ulong value);

    private byte* Detour(IntPtr a1, ulong addonRowId, ulong value)
    {
        if (addonRowId != 7840) return Hook.Original(a1, addonRowId, value);

        var str = (Utf8String*)(a1 + 0x9C0);
        var time = DateTime.UnixEpoch.AddSeconds(value).ToLocalTime();
        var formatted = time.ToString(Config.Format);

        MemoryHelper.WriteString((IntPtr)str->StringPtr, formatted + "\0");
        str->BufUsed = formatted.Length + 1;
        str->StringLength = formatted.Length;

        return str->StringPtr;
    }
}
