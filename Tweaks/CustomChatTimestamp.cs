using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public unsafe partial class CustomChatTimestamp : Tweak
{
    public override string Name => "Custom Chat Timestamp";
    public override string Description => "As it says, configurable chat timestamp format.";
    public static Configuration Config => Plugin.Config.Tweaks.CustomChatTimestamp;

    public class Configuration
    {
        [ConfigField(DefaultValue = "[HH:mm] ")]
        public string Format = "[HH:mm] ";
    }

    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        if (ImGui.InputText("Format##HaselTweaks_CustomChatTimestamp_Format", ref Config.Format, 50))
        {
            Plugin.Config.Save();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey);
        ImGui.Text("This gets passed to C#'s");
        ImGuiUtils.SameLineSpace();
        ImGuiUtils.DrawLink("DateTime.ToString()", "Custom date and time format strings documentation", "https://docs.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings");
        ImGuiUtils.SameLineSpace();
        ImGui.Text("function.");
        ImGui.PopStyleColor();
    }

    [SigHook("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87")]
    private byte* FormatAddon(nint a1, ulong addonRowId, ulong value)
    {
        if (addonRowId == 7840)
        {
            try
            {
                var str = (Utf8String*)(a1 + 0x9C0);
                var time = DateTime.UnixEpoch.AddSeconds(value).ToLocalTime();
                var formatted = time.ToString(Config.Format);

                MemoryHelper.WriteString((nint)str->StringPtr, formatted);
                str->BufUsed = formatted.Length + 1;
                str->StringLength = formatted.Length;

                return str->StringPtr;
            }
            catch (Exception e)
            {
                Error(e, "Error formatting Chat Timestamp");
            }
        }

        return FormatAddonHook.Original(a1, addonRowId, value);
    }
}
