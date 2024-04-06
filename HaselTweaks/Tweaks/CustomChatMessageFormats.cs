using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Text;
using HaselCommon.Text.Payloads;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak]
public partial class CustomChatMessageFormats : Tweak<CustomChatMessageFormatsConfiguration>
{
    public override unsafe void Enable()
    {
        ReloadChat();
    }

    public override void Disable()
    {
        ReloadChat();
    }

    [AddressHook<HaselRaptureLogModule>(nameof(HaselRaptureLogModule.FormatLogMessage))]
    public unsafe uint RaptureLogModule_FormatLogMessage(HaselRaptureLogModule* haselRaptureLogModule, uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex)
    {
        if (haselRaptureLogModule->LogKindSheet == 0 || haselRaptureLogModule->AtkFontCodeModule == null)
            return 0;

        if (!Config.FormatOverrides.TryGetValue(logKindId, out var logKindOverride) || !logKindOverride.Enabled || !logKindOverride.IsValid())
            return RaptureLogModule_FormatLogMessageHook.OriginalDisposeSafe(haselRaptureLogModule, logKindId, sender, message, timestamp, a6, a7, chatTabIndex);

        var tempParseMessage1 = haselRaptureLogModule->TempParseMessageSpan.GetPointer(1);
        tempParseMessage1->Clear();

        if (!haselRaptureLogModule->RaptureTextModule->TextModule.FormatString(message->StringPtr, null, tempParseMessage1))
            return 0;

        var senderStr = new SeString([new RawPayload(sender->AsSpan().ToArray())]);
        var messageStr = new SeString([new RawPayload(tempParseMessage1->AsSpan().ToArray())]);

        var tempParseMessage0 = haselRaptureLogModule->TempParseMessageSpan.GetPointer(0);
        tempParseMessage0->Clear();
        tempParseMessage0->SetString(logKindOverride.Format.Resolve([senderStr, messageStr]).Encode());

        var raptureLogModule = (RaptureLogModule*)haselRaptureLogModule;
        if (raptureLogModule->ChatTabShouldDisplayTime[chatTabIndex] && timestamp != null)
        {
            if (raptureLogModule->UseServerTime)
                *timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var tempParseMessage3 = haselRaptureLogModule->TempParseMessageSpan.GetPointer(3);
            tempParseMessage3->SetString(haselRaptureLogModule->RaptureTextModule->FormatAddonText2Int(raptureLogModule->Use12HourClock ? 7841u : 7840u, *timestamp));
            var buffer = stackalloc Utf8String[1];
            buffer->Ctor();
            tempParseMessage0->Copy(Utf8String.Concat(tempParseMessage3, buffer, tempParseMessage0));
            buffer->Dtor();
        }

        return haselRaptureLogModule->AtkFontCodeModule->CalculateLogLines(a7, tempParseMessage0, a6, false);
    }

    public static unsafe void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    public static void SaveAndReloadChat()
    {
        Service.GetService<Configuration>().Save();
        ReloadChat();
    }
}
