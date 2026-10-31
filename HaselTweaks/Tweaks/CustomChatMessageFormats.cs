using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CustomChatMessageFormats : ConfigurableTweak<CustomChatMessageFormatsConfiguration>
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly ExcelService _excelService;
    private readonly GfdService _gfdService;
    private readonly IFramework _framework;
    private readonly ISigScanner _sigScanner;

    private Hook<RaptureLogModule.Delegates.FormatLogMessage>? _formatLogMessageHook;
    private CalculateLogLines _calculateLogLine;

    public delegate uint CalculateLogLines(AtkFontCodeModule* thisPtr, Utf8String* a2, Utf8String* a3, nint a4, bool a5);

    public override ValueTask OnEnable()
    {
        var calculateLogLinesAddress = _sigScanner.ScanText("E8 ?? ?? ?? ?? 85 C0 45 0F B6 C7");
        _calculateLogLine = Marshal.GetDelegateForFunctionPointer<CalculateLogLines>(calculateLogLinesAddress);

        return new ValueTask(_framework.Run(() =>
        {
            _disposables = DisposableBag.Create(
                _formatLogMessageHook = _gameInteropProvider.EnabledHookFromAddress<RaptureLogModule.Delegates.FormatLogMessage>(
                    RaptureLogModule.MemberFunctionPointers.FormatLogMessage,
                    FormatLogMessageDetour),

                _languageProvider.OnLanguageChange(OnLanguageChanged));

            ReloadChat();
        }));
    }

    public override ValueTask OnDisable()
    {
        return new ValueTask(_framework.Run(() =>
        {
            DisposeAndNull(ref _disposables);

            if (Status is TweakStatus.Enabled)
                ReloadChat();
        }));
    }

    private void OnLanguageChanged()
    {
        if (_isConfigWindowOpen)
        {
            _cachedLogKindRows = GenerateLogKindCache();
            _cachedTextColor = GenerateTextColor();
        }
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    private uint FormatLogMessageDetour(RaptureLogModule* thisPtr, uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, void* a6, Utf8String* a7, int chatTabIndex)
    {
        if (thisPtr->LogKindSheet == null || thisPtr->AtkFontCodeModule == null)
            return 0;

        if (!_config.FormatOverrides.TryGetValue(logKindId, out var logKindOverride) || !logKindOverride.Enabled || !logKindOverride.IsValid())
            return _formatLogMessageHook!.OriginalDisposeSafe(thisPtr, logKindId, sender, message, timestamp, a6, a7, chatTabIndex);

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

        return _calculateLogLine.Invoke(thisPtr->AtkFontCodeModule, a7, tempParseMessage0, (nint)a6, false);
    }
}
