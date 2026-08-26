using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class ShopItemIcons : Tweak
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
        _linkHandler ??= _chatGui.AddChatLinkHandler(3, OnLinkClick);
    }

    public override void Dispose()
    {
        if (_linkHandler != null)
        {
            _chatGui.RemoveChatLinkHandler(_linkHandler.CommandId);
            _linkHandler = null;
        }

        base.Dispose();
    }

    public override void OnEnable()
    {
        _clientState.Login += OnLogin;

        if (_clientState.IsLoggedIn)
            _framework.RunOnTick(OnLogin, delayTicks: 1);
    }

    public override void OnDisable()
    {
        _clientState.Login -= OnLogin;
    }

    private void OnLogin()
    {
        _clientState.Login -= OnLogin;

        if (_linkHandler == null)
            return;

        if (!_pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == "ShopItemIcons"))
        {
            var rssb = new RentedSeStringBuilder();
            Chat.Print(rssb.Builder
                .AppendHaselTweaksPrefix()
                .Append(_textService.Translate("ShopItemIcons.ObsoleteMessage"))
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
        _pluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.AllPlugins, "ShopItemIcons");
    }
}
