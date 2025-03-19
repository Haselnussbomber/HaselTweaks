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

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class DTR : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly IDtrBar _dtrBar;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IDalamudPluginInterface _dalamudPluginInterface;

    private IDtrBarEntry? _dtrInstance;
    private IDtrBarEntry? _dtrFPS;
    private IDtrBarEntry? _dtrBusy;
    private int _lastFrameRate;
    private uint _lastInstanceId;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _dtrInstance = _dtrBar.Get("[HaselTweaks] Instance");
        _dtrInstance.Tooltip = "HaselTweaks";

        _dtrFPS = _dtrBar.Get("[HaselTweaks] FPS");
        _dtrFPS.Tooltip = "HaselTweaks";

        _dtrBusy = _dtrBar.Get("[HaselTweaks] Busy");
        _dtrBusy.Tooltip = "HaselTweaks";
        UpdateBusyText();

        _dtrInstance.Shown = false;
        _dtrFPS.Shown = false;
        _dtrBusy.Shown = false;

        _framework.Update += OnFrameworkUpdate;
        _clientState.Logout += OnLogout;
        _languageProvider.LanguageChanged += OnLanguageChanged;
    }

    public void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;
        _clientState.Logout -= OnLogout;
        _languageProvider.LanguageChanged -= OnLanguageChanged;

        _dtrInstance?.Remove();
        _dtrInstance = null;
        _dtrFPS?.Remove();
        _dtrFPS = null;
        _dtrBusy?.Remove();
        _dtrBusy = null;

        ResetCache();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_clientState.IsLoggedIn)
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
        _lastFrameRate = 0;
        _lastInstanceId = 0;
    }

    private void OnLanguageChanged(string langCode)
    {
        UpdateBusyText();
    }

    private void UpdateBusyText()
    {
        if (_dtrBusy == null)
            return;

        _dtrBusy.Text = new SeStringBuilder()
            .PushColorType(1)
            .PushEdgeColorType(16)
            .Append(_excelService.TryGetRow<OnlineStatus>(12, out var busyStatus) ? busyStatus.Name : ReadOnlySeString.FromText("Busy"))
            .PopEdgeColorType()
            .PopColorType()
            .ToSeString()
            .ToDalamudString();
    }

    private void UpdateInstance()
    {
        if (_dtrInstance == null)
            return;

        var instanceId = UIState.Instance()->PublicInstance.InstanceId;
        if (instanceId == 0 || instanceId >= 10)
        {
            if (_dtrInstance.Shown)
                _dtrInstance.Shown = false;

            if (_lastInstanceId != 0)
                _lastInstanceId = 0;
            return;
        }

        if (_lastInstanceId == instanceId)
            return;

        _dtrInstance.Text = ((char)(SeIconChar.Instance1 + (byte)(instanceId - 1))).ToString();

        if (!_dtrInstance.Shown)
            _dtrInstance.Shown = true;

        _lastInstanceId = instanceId;
    }

    private void UpdateBusy()
    {
        if (_dtrBusy == null)
            return;

        _dtrBusy.Shown = _clientState.IsLoggedIn && _clientState.LocalPlayer?.OnlineStatus.RowId == 12;
    }

    private void UpdateFPS()
    {
        if (_dtrFPS == null)
            return;

        var frameRate = (int)(GameFramework.Instance()->FrameRate + 0.5f);
        if (_lastFrameRate == frameRate)
            return;

        try
        {
            _dtrFPS.Text = string.Format(Config.FpsFormat, frameRate);
        }
        catch (FormatException)
        {
            _dtrFPS.Text = _textService.Translate("DTR.FpsFormat.Invalid");
        }

        if (!_dtrFPS.Shown)
            _dtrFPS.Shown = true;

        _lastFrameRate = frameRate;
    }
}
