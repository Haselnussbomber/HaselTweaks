using System.Text;
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
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
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
    DalamudPluginInterface DalamudPluginInterface)
    : IConfigurableTweak
{
    public string InternalName => nameof(DTR);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private IDtrBarEntry? DtrInstance;
    private IDtrBarEntry? DtrFPS;
    private IDtrBarEntry? DtrBusy;
    private int _lastFrameRate;
    private uint _lastInstanceId;

    public void OnInitialize() { }

    public void OnEnable()
    {
        DtrInstance = DtrBar.Get("[HaselTweaks] Instance");

        DtrFPS = DtrBar.Get("[HaselTweaks] FPS");

        DtrBusy = DtrBar.Get("[HaselTweaks] Busy");

        DtrInstance.Shown = false;
        DtrFPS.Shown = false;
        DtrBusy.Shown = false;

        Framework.Update += OnFrameworkUpdate;
        TextService.LanguageChanged += OnLanguageChanged;
    }

    public void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
        TextService.LanguageChanged -= OnLanguageChanged;

        DtrInstance?.Remove();
        DtrInstance = null;
        DtrFPS?.Remove();
        DtrFPS = null;
        DtrBusy?.Remove();
        DtrBusy = null;
    }

    public void Dispose()
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

    private void OnLanguageChanged(string langCode)
    {
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

        DtrBusy.Text = new SeStringBuilder()
            .PushColorType(1)
            .PushEdgeColorType(16)
            .Append(ExcelService.GetRow<OnlineStatus>(12)?.Name.RawData.ToArray() ?? Encoding.UTF8.GetBytes("Busy"))
            .PopEdgeColorType()
            .PopColorType()
            .ToSeString()
            .ToDalamudString();

        DtrBusy.Shown = ClientState.IsLoggedIn && ClientState.LocalPlayer?.OnlineStatus.Id == 12;
    }

    private void UpdateFPS()
    {
        if (DtrFPS == null)
            return;

        var frameRate = (int)(GameFramework.Instance()->FrameRate + 0.5f);
        if (_lastFrameRate == frameRate)
            return;

        DtrFPS.Text = TextService.Translate("DTR.FPS.Format", frameRate, Config.FormatUnitText);
        DtrFPS.Shown = true;

        _lastFrameRate = frameRate;
    }
}
