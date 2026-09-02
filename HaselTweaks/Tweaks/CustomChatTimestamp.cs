using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CustomChatTimestamp : ConfigurableTweak<CustomChatTimestampConfiguration>
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;
    private readonly TextService _textService;

    private Hook<RaptureTextModule.Delegates.FormatAddonText2Int> _formatAddonText2IntHook;

    public override void OnEnable()
    {
        _disposables = _formatAddonText2IntHook = _gameInteropProvider.EnabledHookFromAddress<RaptureTextModule.Delegates.FormatAddonText2Int>(
            RaptureTextModule.MemberFunctionPointers.FormatAddonText2Int,
            FormatAddonText2IntDetour);

        ReloadChat();
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);

        if (Status is TweakStatus.Enabled)
            ReloadChat();
    }

    private CStringPointer FormatAddonText2IntDetour(RaptureTextModule* thisPtr, uint addonRowId, int value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(_config.Format))
        {
            try
            {
                var str = thisPtr->UnkStrings1.GetPointer(1);
                str->SetString(DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().ToString(_config.Format));
                return str->StringPtr;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error formatting Chat Timestamp");
            }
        }

        return _formatAddonText2IntHook!.OriginalDisposeSafe(thisPtr, addonRowId, value);
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }
}
