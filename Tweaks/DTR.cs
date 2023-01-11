using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public class DTR : Tweak
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
    public DtrBarEntry? DtrFPS;
    public DtrBarEntry? DtrBusy;
    private string BusyStatusText = string.Empty;

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

    private unsafe void UpdateInstance()
    {
        if (DtrInstance == null) return;

        var instanceAreaData = InstanceAreaData.Instance();
        if (instanceAreaData == null)
        {
            if (DtrInstance.Shown) DtrInstance.Shown = false;
            return;
        }

        var instanceId = instanceAreaData->GetInstanceId();

        if (instanceId <= 0 || instanceId >= 10)
        {
            if (DtrInstance.Shown) DtrInstance.Shown = false;
            return;
        }

        var instanceIcon = SeIconChar.Instance1 + (byte)(instanceId - 1);
        DtrInstance.Text = instanceIcon.ToIconString();
        if (!DtrInstance.Shown) DtrInstance.Shown = true;
    }

    private unsafe void UpdateBusy()
    {
        if (DtrBusy == null) return;

        var addr = Service.ClientState.LocalPlayer?.Address;
        if (addr == null || addr == IntPtr.Zero)
        {
            if (DtrBusy.Shown) DtrBusy.Shown = false;
            return;
        }

        var character = (Character*)addr;
        if (character->OnlineStatus != 12) // 12 = Busy
        {
            if (DtrBusy.Shown) DtrBusy.Shown = false;
            return;
        }

        if (string.IsNullOrEmpty(BusyStatusText))
        {
            var nameBytes = Service.Data.Excel.GetSheet<OnlineStatus>()?.GetRow(12)?.Name.RawData.ToArray();
            if (nameBytes == null)
            {
                if (DtrBusy.Shown) DtrBusy.Shown = false;
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

        if (!DtrBusy.Shown) DtrBusy.Shown = true;
    }

    private unsafe void UpdateFPS()
    {
        if (DtrFPS == null) return;

        var fw = GameFramework.Instance();
        DtrFPS.Shown = fw != null;
        if (fw == null) return;

        var fps = MemoryHelper.Read<float>((IntPtr)fw + 0x17C4);
        DtrFPS.Text = $"{fps:0} fps";
    }
}
