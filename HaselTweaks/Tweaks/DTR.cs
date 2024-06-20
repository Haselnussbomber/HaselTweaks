using System.Text;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public sealed class DTRConfiguration
{
    public string FormatUnitText = " fps";
}

public sealed unsafe class DTR(
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    IDtrBar DtrBar,
    IFramework Framework,
    IClientState ClientState,
    DalamudPluginInterface DalamudPluginInterface)
    : Tweak<DTRConfiguration>(PluginConfig, TranslationManager)
{
    private DtrBarEntry? DtrInstance;
    private DtrBarEntry? DtrFPS;
    private DtrBarEntry? DtrBusy;
    private int _lastFrameRate;
    private uint _lastInstanceId;

    public override void OnEnable()
    {
        DtrInstance = DtrBar.Get("[HaselTweaks] Instance");

        DtrFPS = DtrBar.Get("[HaselTweaks] FPS");

        DtrBusy = DtrBar.Get("[HaselTweaks] Busy",
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

        Framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;

        DtrInstance?.Dispose();
        DtrInstance = null;
        DtrFPS?.Dispose();
        DtrFPS = null;
        DtrBusy?.Dispose();
        DtrBusy = null;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
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

        DtrBusy.Shown = ClientState.LocalPlayer?.OnlineStatus.Id == 12;
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var frameRate = (int)(GameFramework.Instance()->FrameRate + 0.5f);
        if (_lastFrameRate == frameRate)
            return;

        DtrFPS.Text = t("DTR.FPS.Format", frameRate, Config.FormatUnitText);
        DtrFPS.Shown = true;

        _lastFrameRate = frameRate;
    }

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
            void OpenSettings()
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Framework.RunOnTick(OpenSettings, delayTicks: 2);
                    return;
                }

                DalamudPluginInterface.OpenDalamudSettingsTo(SettingsOpenKind.ServerInfoBar);
            }
            Framework.RunOnTick(OpenSettings, delayTicks: 2);
        }
        ImGuiUtils.SameLineSpace();
        ImGui.TextUnformatted(t("DTR.Config.Explanation.Post"));

        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("DTR.Config.FormatUnitText.Label"));
        if (ImGui.InputText("##FormatUnitTextInput", ref Config.FormatUnitText, 20))
        {
            PluginConfig.Save();
            _lastFrameRate = 0; // trigger update
        }
        ImGui.SameLine();
        if (ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", " fps")))
        {
            Config.FormatUnitText = " fps";
            PluginConfig.Save();
        }
        if (TranslationManager.TryGetTranslation("DTR.Config.FormatUnitText.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }
}
