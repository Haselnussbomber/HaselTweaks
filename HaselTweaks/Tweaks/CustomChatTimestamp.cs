using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CustomChatTimestamp : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;

    private Hook<RaptureTextModule.Delegates.FormatAddonText2Int>? _formatAddonText2IntHook;

    public override void OnEnable()
    {
        _formatAddonText2IntHook = _gameInteropProvider.HookFromAddress<RaptureTextModule.Delegates.FormatAddonText2Int>(
            RaptureTextModule.MemberFunctionPointers.FormatAddonText2Int,
            FormatAddonText2IntDetour);
        _formatAddonText2IntHook.Enable();
        ReloadChat();
    }

    public override void OnDisable()
    {
        _formatAddonText2IntHook?.Dispose();
        _formatAddonText2IntHook = null;

        if (Status is TweakStatus.Enabled)
            ReloadChat();
    }

    private CStringPointer FormatAddonText2IntDetour(RaptureTextModule* thisPtr, uint addonRowId, int value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(Config.Format))
        {
            try
            {
                var str = thisPtr->UnkStrings1.GetPointer(1);
                str->SetString(DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().ToString(Config.Format));
                return str->StringPtr;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error formatting Chat Timestamp");
            }
        }

        return _formatAddonText2IntHook!.Original(thisPtr, addonRowId, value);
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }
}
