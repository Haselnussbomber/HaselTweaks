using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselTweaks.Enums;
using HaselTweaks.Extensions;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using ImColor = HaselTweaks.Structs.ImColor;

namespace HaselTweaks.Tweaks;

[Tweak(TweakFlags.HasCustomConfig)]
public unsafe class DTR : Tweak
{
    public override void DrawCustomConfig()
    {
        ImGui.TextUnformatted(t("DTR.Config.Explanation.Pre"));
        ImGuiUtils.TextUnformattedColored((ImColor)ImGuiColors.DalamudRed, t("DTR.Config.Explanation.DalamudSettings"));
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
    }

    public DtrBarEntry? DtrInstance;
    public DtrBarEntry? DtrFPS;
    public DtrBarEntry? DtrBusy;
    private int _lastFrameRate;

    public override void Enable()
    {
        DtrInstance = Service.DtrBar.Get("[HaselTweaks] Instance");
        DtrFPS = Service.DtrBar.Get("[HaselTweaks] FPS");
        DtrBusy = Service.DtrBar.Get("[HaselTweaks] Busy");

        DtrBusy.Text = new SeString(
            new UIForegroundPayload(1),
            new UIGlowPayload(16),
            new TextPayload(GetRow<OnlineStatus>(12)?.Name.ToDalamudString().ToString()),
            UIGlowPayload.UIGlowOff,
            UIForegroundPayload.UIForegroundOff
        );
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

    public override void OnFrameworkUpdate(Framework framework)
    {
        UpdateInstance();
        UpdateFPS();
        UpdateBusy();
    }

    private void UpdateInstance()
    {
        if (DtrInstance == null)
            return;

        var uiState = UIState.Instance();
        if (uiState == null)
        {
            DtrInstance.SetVisibility(false);
            return;
        }

        var instanceId = uiState->AreaInstance.Instance;
        if (instanceId <= 0 || instanceId >= 10)
        {
            DtrInstance.SetVisibility(false);
            return;
        }

        DtrInstance.SetText(((char)(SeIconChar.Instance1 + (byte)(instanceId - 1))).ToString());
        DtrInstance.SetVisibility(true);
    }

    private void UpdateBusy()
    {
        if (DtrBusy == null)
            return;

        DtrBusy.SetVisibility(Service.ClientState.LocalPlayer?.OnlineStatus.Id == 12);
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var gameFramework = GameFramework.Instance();
        if (gameFramework == null)
        {
            DtrFPS.SetVisibility(false);
            return;
        }

        var frameRate = (int)(gameFramework->FrameRate + 0.5f);
        if (_lastFrameRate != frameRate)
        {
            DtrFPS.SetText(t("DTR.FPS.Format", frameRate));
            DtrFPS.SetVisibility(true);
            _lastFrameRate = frameRate;
        }
    }
}
