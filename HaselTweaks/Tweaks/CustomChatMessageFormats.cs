using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselCommon.Text;
using HaselCommon.Text.Payloads;
using HaselCommon.Textures;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public unsafe partial class CustomChatMessageFormats(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ILogger<CustomChatMessageFormats> Logger,
    IGameInteropProvider GameInteropProvider,
    IDataManager DataManager,
    ExcelService ExcelService,
    IGameConfig GameConfig,
    TextureManager TextureManager)
    : IConfigurableTweak
{
    public string InternalName => nameof(CustomChatMessageFormats);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<HaselRaptureLogModule.Delegates.FormatLogMessage>? FormatLogMessageHook;

    public void OnInitialize()
    {
        FormatLogMessageHook = GameInteropProvider.HookFromAddress<HaselRaptureLogModule.Delegates.FormatLogMessage>(
            HaselRaptureLogModule.MemberFunctionPointers.FormatLogMessage,
            FormatLogMessageDetour);
    }

    public void OnEnable()
    {
        ReloadChat();
        TextService.LanguageChanged += OnLanguageChange;
        FormatLogMessageHook?.Enable();
    }

    public void OnDisable()
    {
        ReloadChat();
        TextService.LanguageChanged -= OnLanguageChange;
        FormatLogMessageHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status == TweakStatus.Disposed)
            return;

        OnDisable();
        FormatLogMessageHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChange(string langCode)
    {
        if (IsConfigWindowOpen)
        {
            CachedLogKindRows = GenerateLogKindCache();
            CachedTextColors = GenerateTextColors();
        }
    }

    private static unsafe void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    private unsafe uint FormatLogMessageDetour(HaselRaptureLogModule* haselRaptureLogModule, uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex)
    {
        if (haselRaptureLogModule->LogKindSheet == 0 || haselRaptureLogModule->AtkFontCodeModule == null)
            return 0;

        if (!Config.FormatOverrides.TryGetValue(logKindId, out var logKindOverride) || !logKindOverride.Enabled || !logKindOverride.IsValid())
            return FormatLogMessageHook!.Original(haselRaptureLogModule, logKindId, sender, message, timestamp, a6, a7, chatTabIndex);

        var tempParseMessage1 = haselRaptureLogModule->TempParseMessage.GetPointer(1);
        tempParseMessage1->Clear();

        if (!haselRaptureLogModule->RaptureTextModule->TextModule.FormatString(message->StringPtr, null, tempParseMessage1))
            return 0;

        var senderStr = new SeString([new RawPayload(sender->AsSpan().ToArray())]);
        var messageStr = new SeString([new RawPayload(tempParseMessage1->AsSpan().ToArray())]);

        var tempParseMessage0 = haselRaptureLogModule->TempParseMessage.GetPointer(0);
        tempParseMessage0->Clear();
        tempParseMessage0->SetString(logKindOverride.Format.Resolve([senderStr, messageStr]).Encode());

        var raptureLogModule = (RaptureLogModule*)haselRaptureLogModule;
        if (raptureLogModule->ChatTabShouldDisplayTime[chatTabIndex] && timestamp != null)
        {
            if (raptureLogModule->UseServerTime)
                *timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var tempParseMessage3 = haselRaptureLogModule->TempParseMessage.GetPointer(3);
            tempParseMessage3->SetString(haselRaptureLogModule->RaptureTextModule->FormatAddonText2Int(raptureLogModule->Use12HourClock ? 7841u : 7840u, *timestamp));
            using var buffer = new Utf8String();
            tempParseMessage0->Copy(Utf8String.Concat(tempParseMessage3, &buffer, tempParseMessage0));
        }

        return haselRaptureLogModule->AtkFontCodeModule->CalculateLogLines(a7, tempParseMessage0, a6, false);
    }
}
