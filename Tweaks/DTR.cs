using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
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
        ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey, "Shows Instance, FPS and Busy status in DTR bar.");

        ImGuiUtils.DrawSection("Configuration");
        ImGui.Text("To enable/disable elements or to change the order go into");
        ImGui.TextColored(ImGuiColors.DalamudRed, "Dalamud Settings");
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
        ImGui.Text("> Server Info Bar.");
    }

    public DtrBarEntry? DtrInstance;
    private int LastInstance;
    public DtrBarEntry? DtrFPS;
    public DtrBarEntry? DtrBusy;
    private string? BusyStatusText = null;
    private int LastOnlineStatus;

    public override void Enable()
    {
        DtrInstance = Service.DtrBar.Get("[HaselTweaks] Instance");
        DtrFPS = Service.DtrBar.Get("[HaselTweaks] FPS");
        DtrBusy = Service.DtrBar.Get("[HaselTweaks] Busy");
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

        var instanceId = UIState.Instance()->AreaInstance.Instance;
        if (LastInstance == instanceId)
            return;

        if (instanceId <= 0 || instanceId >= 10)
        {
            if (DtrInstance.Shown)
                DtrInstance.Shown = false;
            return;
        }

        LastInstance = instanceId;

        var instanceIcon = SeIconChar.Instance1 + (byte)(instanceId - 1);
        DtrInstance.Text = instanceIcon.ToIconString();

        DtrInstance.Shown = true;
    }

    private void UpdateBusy()
    {
        if (DtrBusy == null)
            return;

        var addr = Service.ClientState.LocalPlayer?.Address;
        if (addr == null || addr == 0)
        {
            if (DtrBusy.Shown)
                DtrBusy.Shown = false;
            return;
        }

        var character = (Character*)addr;
        if (LastOnlineStatus == character->OnlineStatus)
        {
            if (LastOnlineStatus != 12 && DtrBusy.Shown)
                DtrBusy.Shown = false;
            return;
        }

        LastOnlineStatus = character->OnlineStatus;

        if (character->OnlineStatus != 12) // 12 = Busy
        {
            if (DtrBusy.Shown)
                DtrBusy.Shown = false;
            return;
        }

        if (BusyStatusText == null)
        {
            var nameBytes = Service.Data.Excel.GetSheet<OnlineStatus>()?.GetRow(12)?.Name.RawData.ToArray();
            if (nameBytes == null)
            {
                if (DtrBusy.Shown)
                    DtrBusy.Shown = false;
                return;
            }

            BusyStatusText = SeString.Parse(nameBytes).ToString();
        }

        DtrBusy.Text = new SeString(
            new UIForegroundPayload(1),
            new UIGlowPayload(16),
            new TextPayload(BusyStatusText),
            UIGlowPayload.UIGlowOff,
            UIForegroundPayload.UIForegroundOff
        );

        DtrBusy.Shown = true;
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null) return;

        var fw = GameFramework.Instance();
        if (fw == null)
        {
            if (DtrFPS.Shown)
                DtrFPS.Shown = false;
            return;
        }

        DtrFPS.Text = $"{fw->FrameRate:0} fps";

        if (!DtrFPS.Shown)
            DtrFPS.Shown = true;
    }
}
