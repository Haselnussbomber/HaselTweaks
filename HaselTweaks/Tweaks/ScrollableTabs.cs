
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class ScrollableTabs : Tweak
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly IChatGui _chatGui;
    private readonly IClientState _clientState;
    private readonly TextService _textService;
    private DalamudLinkPayload? _linkHandler;

    [AutoPostConstruct]
    private void Initialize()
    {
        IsObsolete = true;
        _linkHandler ??= _chatGui.AddChatLinkHandler(2, OnLinkClick);
    }

    public override ValueTask DisposeAsync()
    {
        if (_linkHandler != null)
        {
            _chatGui.RemoveChatLinkHandler(_linkHandler.CommandId);
            _linkHandler = null;
        }

        return base.DisposeAsync();
    }

    public override ValueTask OnEnable()
    {
        _clientState.Login += OnLogin;

        if (_clientState.IsLoggedIn)
            _framework.RunOnTick(OnLogin, delayTicks: 1);

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        _clientState.Login -= OnLogin;
        return ValueTask.CompletedTask;
    }

    private void OnLogin()
    {
        _clientState.Login -= OnLogin;

        if (_linkHandler == null)
            return;

        if (!_pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "ScrollableTabs"))
        {
            var rssb = new RentedSeStringBuilder();
            Chat.Print(rssb.Builder
                .AppendHaselTweaksPrefix()
                .Append(_textService.Translate("ScrollableTabs.ObsoleteMessage"))
                .Append(" ")
                .Append(new ReadOnlySeStringSpan(_linkHandler.Encode()))
                .Append(_textService.Translate("ClickToOpenPluginInstaller"))
                .PopLink()
                .ToReadOnlySeString());
        }

        OnDisable();
        Status = TweakStatus.Disabled;
        if (_pluginConfig.EnabledTweaks.Remove(InternalName))
            _pluginConfig.Save();
    }

    private void OnLinkClick(uint commandId, Dalamud.Game.Text.SeStringHandling.SeString str)
    {
        _pluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.AllPlugins, "ScrollableTabs");
    }
}
