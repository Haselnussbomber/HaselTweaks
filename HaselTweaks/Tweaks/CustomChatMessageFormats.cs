using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CustomChatMessageFormats : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ILogger<CustomChatMessageFormats> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;
    private readonly TextureService _textureService;
    private readonly SeStringEvaluator _seStringEvaluator;

    private Hook<RaptureLogModule.Delegates.FormatLogMessage>? _formatLogMessageHook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _formatLogMessageHook = _gameInteropProvider.HookFromAddress<RaptureLogModule.Delegates.FormatLogMessage>(
            RaptureLogModule.MemberFunctionPointers.FormatLogMessage,
            FormatLogMessageDetour);
    }

    public void OnEnable()
    {
        ReloadChat();
        _languageProvider.LanguageChanged += OnLanguageChange;
        _formatLogMessageHook?.Enable();
    }

    public void OnDisable()
    {
        _languageProvider.LanguageChanged -= OnLanguageChange;
        _formatLogMessageHook?.Disable();

        if (Status is TweakStatus.Enabled)
            ReloadChat();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _formatLogMessageHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnLanguageChange(string langCode)
    {
        if (_isConfigWindowOpen)
        {
            _cachedLogKindRows = GenerateLogKindCache();
            _cachedTextColor = GenerateTextColor();
        }
    }

    private static unsafe void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    private unsafe uint FormatLogMessageDetour(RaptureLogModule* thisPtr, uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, void* a6, Utf8String* a7, int chatTabIndex)
    {
        if (thisPtr->LogKindSheet == null || thisPtr->AtkFontCodeModule == null)
            return 0;

        if (!Config.FormatOverrides.TryGetValue(logKindId, out var logKindOverride) || !logKindOverride.Enabled || !logKindOverride.IsValid())
            return _formatLogMessageHook!.Original(thisPtr, logKindId, sender, message, timestamp, a6, a7, chatTabIndex);

        var tempParseMessage1 = thisPtr->TempParseMessage.GetPointer(1);
        tempParseMessage1->Clear();

        if (!thisPtr->RaptureTextModule->TextModule.FormatString(message->StringPtr.Value, null, tempParseMessage1))
            return 0;

        var senderStr = new ReadOnlySeStringSpan(sender->AsSpan());
        var messageStr = new ReadOnlySeStringSpan(tempParseMessage1->AsSpan());

        var tempParseMessage0 = thisPtr->TempParseMessage.GetPointer(0);
        tempParseMessage0->Clear();
        tempParseMessage0->SetString(_seStringEvaluator.Evaluate(logKindOverride.Format, [senderStr, messageStr]));

        if (thisPtr->ChatTabShouldDisplayTime[chatTabIndex] && timestamp != null)
        {
            if (thisPtr->UseServerTime)
                *timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var tempParseMessage3 = thisPtr->TempParseMessage.GetPointer(3);
            tempParseMessage3->SetString(thisPtr->RaptureTextModule->FormatAddonText2Int(thisPtr->Use12HourClock ? 7841u : 7840u, *timestamp));
            using var buffer = new Utf8String();
            tempParseMessage0->Copy(Utf8String.Concat(tempParseMessage3, &buffer, tempParseMessage0));
        }

        return ((HaselAtkFontCodeModule*)thisPtr->AtkFontCodeModule)->CalculateLogLines(a7, tempParseMessage0, (nint)a6, false);
    }
}
