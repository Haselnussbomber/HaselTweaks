using System.Text;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselCommon.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace HaselTweaks.Tweaks;

public class DTRConfiguration
{
    public string FormatUnitText = " fps";
}

[Tweak]
public unsafe class DTR : Tweak<DTRConfiguration>
{
    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("DTR.Config.Explanation.Pre"));
        ImGuiUtils.TextUnformattedColored(HaselColor.From(ImGuiColors.DalamudRed), t("DTR.Config.Explanation.DalamudSettings"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        if (ImGui.IsItemClicked())
        {
            static void OpenSettings()
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Service.Framework.RunOnTick(OpenSettings, delayTicks: 2);
                    return;
                }

                Service.CommandManager.ProcessCommand("/xlsettings");
            }
            Service.Framework.RunOnTick(OpenSettings, delayTicks: 2);
        }
        ImGuiUtils.SameLineSpace();
        ImGui.TextUnformatted(t("DTR.Config.Explanation.Post"));

        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("DTR.Config.FormatUnitText.Label"));
        if (ImGui.InputText("##FormatUnitTextInput", ref Config.FormatUnitText, 20))
        {
            Service.GetService<Configuration>().Save();
            _lastFrameRate = 0; // trigger update
        }
        ImGui.SameLine();
        if (ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", " fps")))
        {
            Config.FormatUnitText = " fps";
            Service.GetService<Configuration>().Save();
        }
        if (Service.TranslationManager.TryGetTranslation("DTR.Config.FormatUnitText.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }

    public DtrBarEntry? DtrInstance;
    public DtrBarEntry? DtrFPS;
    public DtrBarEntry? DtrBusy;
    private int _lastFrameRate;
    private uint _lastInstanceId;

    public override void Enable()
    {
        DtrInstance = Service.DtrBar.Get("[HaselTweaks] Instance");

        DtrFPS = Service.DtrBar.Get("[HaselTweaks] FPS");

        DtrBusy = Service.DtrBar.Get("[HaselTweaks] Busy",
            new SeStringBuilder()
                .PushColorType(1)
                .PushEdgeColorType(16)
                .Append(GetRow<OnlineStatus>(12)?.Name.RawData.ToArray() ?? Encoding.UTF8.GetBytes("Busy"))
                .PopEdgeColorType()
                .PopColorType()
                .ToSeString()
                .ToDalamudString());

        DtrInstance.Shown = false;
        DtrFPS.Shown = false;
        DtrBusy.Shown = false;
    }

    public override void Disable()
    {
        DtrInstance?.Dispose();
        DtrInstance = null;
        DtrFPS?.Dispose();
        DtrFPS = null;
        DtrBusy?.Dispose();
        DtrBusy = null;
    }

    public override void OnFrameworkUpdate()
    {
        if (!Service.ClientState.IsLoggedIn)
            return;

        UpdateInstance();
        UpdateFPS();
        UpdateBusy();
    }

    private void UpdateInstance()
    {
        if (DtrInstance == null)
            return;

        var instanceId = UIState.Instance()->PublicInstance.InstanceId;
        if (_lastInstanceId == instanceId || instanceId == 0 || instanceId >= 10)
        {
            DtrInstance.Shown = false;
            return;
        }

        DtrInstance.Text = ((char)(SeIconChar.Instance1 + (byte)(instanceId - 1))).ToString();
        DtrInstance.Shown = true;

        _lastInstanceId = instanceId;
    }

    private void UpdateBusy()
    {
        if (DtrBusy == null)
            return;

        DtrBusy.Shown = Service.ClientState.LocalPlayer?.OnlineStatus.Id == 12;
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var frameRate = (int)(Framework.Instance()->FrameRate + 0.5f);
        if (_lastFrameRate == frameRate)
            return;

        DtrFPS.Text = t("DTR.FPS.Format", frameRate, Config.FormatUnitText);
        DtrFPS.Shown = true;

        _lastFrameRate = frameRate;
    }
}
