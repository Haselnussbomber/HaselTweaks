using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public unsafe partial class DTR(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ExcelService ExcelService,
    IDtrBar DtrBar,
    IFramework Framework,
    IClientState ClientState,
    IDalamudPluginInterface DalamudPluginInterface)
    : IConfigurableTweak
{
    public string InternalName => nameof(DTR);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private IDtrBarEntry? DtrInstance;
    private IDtrBarEntry? DtrFPS;
    private IDtrBarEntry? DtrBusy;
    private int LastFrameRate;
    private uint LastInstanceId;

    public void OnInitialize() { }

    public void OnEnable()
    {
        DtrInstance = DtrBar.Get("[HaselTweaks] Instance");
        DtrInstance.Tooltip = "HaselTweaks";

        DtrFPS = DtrBar.Get("[HaselTweaks] FPS");
        DtrFPS.Tooltip = "HaselTweaks";

        DtrBusy = DtrBar.Get("[HaselTweaks] Busy");
        DtrBusy.Tooltip = "HaselTweaks";
        UpdateBusyText();

        DtrInstance.Shown = false;
        DtrFPS.Shown = false;
        DtrBusy.Shown = false;

        Framework.Update += OnFrameworkUpdate;
        ClientState.Logout += OnLogout;
        TextService.LanguageChanged += OnLanguageChanged;
    }

    public void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.Logout -= OnLogout;
        TextService.LanguageChanged -= OnLanguageChanged;

        DtrInstance?.Remove();
        DtrInstance = null;
        DtrFPS?.Remove();
        DtrFPS = null;
        DtrBusy?.Remove();
        DtrBusy = null;

        ResetCache();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
            return;

        UpdateInstance();
        UpdateFPS();
        UpdateBusy();
    }

    private void OnLogout(int type, int code)
    {
        ResetCache();
    }

    private void ResetCache()
    {
        LastFrameRate = 0;
        LastInstanceId = 0;
    }

    private void OnLanguageChanged(string langCode)
    {
        UpdateBusyText();
    }

    private void UpdateBusyText()
    {
        if (DtrBusy == null)
            return;

        DtrBusy.Text = new SeStringBuilder()
            .PushColorType(1)
            .PushEdgeColorType(16)
            .Append(ExcelService.TryGetRow<OnlineStatus>(12, out var busyStatus) ? busyStatus.Name : ReadOnlySeString.FromText("Busy"))
            .PopEdgeColorType()
            .PopColorType()
            .ToSeString()
            .ToDalamudString();
    }

    private void UpdateInstance()
    {
        if (DtrInstance == null)
            return;

        var instanceId = UIState.Instance()->PublicInstance.InstanceId;
        if (instanceId == 0 || instanceId >= 10)
        {
            if (DtrInstance.Shown)
                DtrInstance.Shown = false;

            if (LastInstanceId != 0)
                LastInstanceId = 0;
            return;
        }

        if (LastInstanceId == instanceId)
            return;

        DtrInstance.Text = ((char)(SeIconChar.Instance1 + (byte)(instanceId - 1))).ToString();

        if (!DtrInstance.Shown)
            DtrInstance.Shown = true;

        LastInstanceId = instanceId;
    }

    private void UpdateBusy()
    {
        if (DtrBusy == null)
            return;

        DtrBusy.Shown = ClientState.IsLoggedIn && ClientState.LocalPlayer?.OnlineStatus.RowId == 12;
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var frameRate = (int)(GameFramework.Instance()->FrameRate + 0.5f);
        if (LastFrameRate == frameRate)
            return;

        try
        {
            DtrFPS.Text = string.Format(Config.FpsFormat, frameRate);
        }
        catch (FormatException)
        {
            DtrFPS.Text = TextService.Translate("DTR.FpsFormat.Invalid");
        }

        if (!DtrFPS.Shown)
            DtrFPS.Shown = true;

        LastFrameRate = frameRate;
    }
}
