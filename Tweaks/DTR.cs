using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public unsafe class DTR : Tweak
{
    public override string Name => "DTR";
    public override bool HasDescription => true;
    public override void DrawDescription()
    {
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "Shows Instance number (only if the current zone is instanced), FPS and Busy status in DTR bar.");

        ImGuiUtils.DrawSection("Configuration");
        ImGui.TextUnformatted("To enable/disable elements or to change the order go into");
        ImGuiUtils.TextUnformattedColored(ImGuiColors.DalamudRed, "Dalamud Settings");
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
                    Service.Framework.RunOnTick(OpenSettings, default, 2);
                    return;
                }

                Chat.SendMessage("/xlsettings");
            }
            Service.Framework.RunOnTick(OpenSettings, default, 2);
        }
        ImGuiUtils.SameLineSpace();
        ImGui.TextUnformatted("> Server Info Bar.");
    }

    public DtrBarEntry? DtrInstance;
    public DtrBarEntry? DtrFPS;
    public DtrBarEntry? DtrBusy;

    public override void Enable()
    {
        DtrInstance = Service.DtrBar.Get("[HaselTweaks] Instance");
        DtrFPS = Service.DtrBar.Get("[HaselTweaks] FPS");
        DtrBusy = Service.DtrBar.Get("[HaselTweaks] Busy");

        DtrBusy.Text = new SeString(
            new UIForegroundPayload(1),
            new UIGlowPayload(16),
            new TextPayload(Service.Data.Excel.GetSheet<OnlineStatus>()?.GetRow(12)?.Name.ToDalamudString().ToString()),
            UIGlowPayload.UIGlowOff,
            UIForegroundPayload.UIForegroundOff
        );
    }

    public override void Disable()
    {
        DtrInstance?.Remove();
        DtrInstance = null;

        DtrFPS?.Remove();
        DtrFPS = null;

        DtrBusy?.Remove();
        DtrBusy = null;
    }

    public override void Dispose()
    {
        DtrInstance?.Dispose();
        DtrFPS?.Dispose();
        DtrBusy?.Dispose();
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
            if (DtrInstance.Shown)
                DtrInstance.Shown = false;
            return;
        }

        var instanceId = uiState->AreaInstance.Instance;
        if (instanceId <= 0 || instanceId >= 10)
        {
            if (DtrInstance.Shown)
                DtrInstance.Shown = false;
            return;
        }

        var text = ((char)(SeIconChar.Instance1 + (byte)(instanceId - 1))).ToString();
        if (DtrInstance.Text?.TextValue != text)
            DtrInstance.Text = text;

        if (!DtrInstance.Shown)
            DtrInstance.Shown = true;
    }

    private void UpdateBusy()
    {
        if (DtrBusy == null)
            return;

        var character = (Character*)(Service.ClientState.LocalPlayer?.Address ?? 0);
        if (character == null || character->OnlineStatus != 12) // 12 = Busy
        {
            if (DtrBusy.Shown)
                DtrBusy.Shown = false;
        }
        else
        {
            if (!DtrBusy.Shown)
                DtrBusy.Shown = true;
        }
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var gameFramework = GameFramework.Instance();
        if (gameFramework == null)
        {
            if (DtrFPS.Shown)
                DtrFPS.Shown = false;
            return;
        }

        var text = $"{gameFramework->FrameRate:0} fps";
        if (DtrFPS.Text?.TextValue != text)
            DtrFPS.Text = text;

        if (!DtrFPS.Shown)
            DtrFPS.Shown = true;
    }
}
