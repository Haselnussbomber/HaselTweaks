using Dalamud.Interface.Colors;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public class DTRConfiguration
{
    public string FpsFormat = "{0} fps";
}

public partial class DTR
{
    private DTRConfiguration Config => _pluginConfig.Tweaks.DTR;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        ResetCache();
    }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();

        ImGui.TextUnformatted(_textService.Translate("DTR.Config.Explanation.Pre"));
        ImGuiUtils.TextUnformattedColored(Color.FromVector4(ImGuiColors.DalamudRed), _textService.Translate("DTR.Config.Explanation.DalamudSettings"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        if (ImGui.IsItemClicked())
        {
            void OpenSettings()
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _framework.RunOnTick(OpenSettings, delayTicks: 2);
                    return;
                }

                _dalamudPluginInterface.OpenDalamudSettingsTo(SettingsOpenKind.ServerInfoBar);
            }
            _framework.RunOnTick(OpenSettings, delayTicks: 2);
        }
        ImGuiUtils.SameLineSpace();
        ImGui.TextUnformatted(_textService.Translate("DTR.Config.Explanation.Post"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        _configGui.DrawString("FpsFormat", ref Config.FpsFormat, "{0} fps");

        ImGui.Spacing();
        ImGui.TextUnformatted(_textService.Translate("DTR.Config.Format.Example.Label"));

        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetTextLineHeight() + 2);
        using var child = ImRaii.Child("##FormatExample", size, true);
        if (!child) return;

        try
        {
            unsafe
            {
                ImGui.TextUnformatted(string.Format(Config.FpsFormat, (int)(GameFramework.Instance()->FrameRate + 0.5f)));
            }
        }
        catch (FormatException)
        {
            using (Color.Red.Push(ImGuiCol.Text))
                ImGui.TextUnformatted(_textService.Translate("DTR.Config.FpsFormat.Invalid"));
        }
    }
}
