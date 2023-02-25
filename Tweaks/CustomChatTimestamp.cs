using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Tweaks;

public unsafe partial class CustomChatTimestamp : Tweak
{
    public override string Name => "Custom Chat Timestamp";
    public override string Description => "As it says, configurable chat timestamp format.";
    public static Configuration Config => Plugin.Config.Tweaks.CustomChatTimestamp;

    public class Configuration
    {
        [ConfigField(Description = "This gets passed to C#'s DateTime.ToString() function.", DefaultValue = "[HH:mm] ")]
        public string Format = "[HH:mm] ";
    }

    [SigHook("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87")]
    private byte* FormatAddon(nint a1, ulong addonRowId, ulong value)
    {
        if (addonRowId != 7840) return FormatAddonHook.Original(a1, addonRowId, value);

        var str = (Utf8String*)(a1 + 0x9C0);
        var time = DateTime.UnixEpoch.AddSeconds(value).ToLocalTime();
        var formatted = time.ToString(Config.Format);

        MemoryHelper.WriteString((nint)str->StringPtr, formatted + "\0");
        str->BufUsed = formatted.Length + 1;
        str->StringLength = formatted.Length;

        return str->StringPtr;
    }
}
