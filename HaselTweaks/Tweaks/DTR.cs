using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class DTR : ConfigurableTweak<DTRConfiguration>
{
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly ExcelService _excelService;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IDtrBar _dtrBar;
    private readonly IDalamudPluginInterface _dalamudPluginInterface;

    private DtrBarEntry? _dtrInstance;
    private DtrBarEntry? _dtrFPS;
    private DtrBarEntry? _dtrBusy;
    private int _lastFrameRate;
    private uint _lastInstanceId;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _dtrInstance = _dtrBar.GetDisposable("[HaselTweaks] Instance"),
            _dtrFPS = _dtrBar.GetDisposable("[HaselTweaks] FPS"),
            _dtrBusy = _dtrBar.GetDisposable("[HaselTweaks] Busy"),
            _framework.OnUpdate(OnFrameworkUpdate),
            _clientState.OnLogout(OnLogout),
            _languageProvider.OnLanguageChange(OnLanguageChange));

        UpdateBusyText();

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        _dtrInstance = null;
        _dtrFPS = null;
        _dtrBusy = null;

        ResetCache();

        return ValueTask.CompletedTask;
    }

    private void OnFrameworkUpdate()
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

    private void OnLanguageChange()
    {
        UpdateBusyText();
    }

    private void UpdateBusyText()
    {
        if (_dtrBusy == null)
            return;

        using var rssb = new RentedSeStringBuilder();
        _dtrBusy.Text = rssb.Builder
            .PushColorType(1)
            .PushEdgeColorType(16)
            .Append(_excelService.TryGetRow<OnlineStatus>(12, out var busyStatus) ? busyStatus.Name : ReadOnlySeString.FromText("Busy"))
            .PopEdgeColorType()
            .PopColorType()
            .ToReadOnlySeString()
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

        var localPlayer = Control.GetLocalPlayer();
        _dtrBusy.Shown = localPlayer != null && localPlayer->OnlineStatus == 12;
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
            _dtrFPS.Text = string.Format(_config.FpsFormat, frameRate);
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
