using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AchievementLinkTooltip : IConfigurableTweak
{
    private static readonly string[] ChatPanels = ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"];

    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ExcelService _excelService;
    private Utf8String* _tooltipText;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _tooltipText = Utf8String.CreateEmpty();
    }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, ChatPanels, OnChatLogPanelPostReceiveEvent);
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, ChatPanels, OnChatLogPanelPostReceiveEvent);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        if (_tooltipText != null)
        {
            _tooltipText->Dtor(true);
            _tooltipText = null;
        }

        Status = TweakStatus.Disposed;
    }

    private void OnChatLogPanelPostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonChatLogPanel*)args.Addon;

        if (!addon->IsReady || addon->LogViewer.IsSelectingText || addon->IsResizing)
            return;

        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.AtkEventType != (byte)AtkEventType.LinkMouseOver)
            return;

        var eventData = (AtkEventData*)receiveEventArgs.Data;
        var linkData = eventData->LinkData;
        var linkType = (LinkMacroPayloadType)linkData->LinkType;
        if (linkType is not LinkMacroPayloadType.Achievement)
            return;

        if (!_excelService.TryGetRow<Achievement>(linkData->UIntValue1, out var achievement))
            return;

        ref var achievements = ref UIState.Instance()->Achievement;
        var isComplete = achievements.IsComplete((int)achievement.RowId);

        var canShowName = !Config.PreventSpoiler;
        var canShowDescription = !Config.PreventSpoiler;

        if (Config.PreventSpoiler)
        {
            var isHiddenCategory = achievement.AchievementCategory.ValueNullable?.HideCategory == true;
            var isHiddenName = achievement.AchievementHideCondition.ValueNullable?.HideName == true;
            var isHiddenAchievement = achievement.AchievementHideCondition.ValueNullable?.HideAchievement == true;

            canShowName |= !isHiddenName || isComplete;
            canShowDescription |= !(isHiddenCategory || isHiddenAchievement) || isComplete;
        }

        var sb = new SeStringBuilder();

        sb.BeginMacro(MacroCode.Color)
          .AppendIntExpression(RaptureTextModule.Instance()->TextModule.MacroDecoder.GlobalParameters[61].IntValue)
          .EndMacro();

        if (canShowName)
            sb.Append(achievement.Name);
        else
            sb.Append(_textService.GetAddonText(3384)); // "???"

        sb.PopColor();
        sb.BeginMacro(MacroCode.NewLine).EndMacro();

        if (canShowDescription)
            sb.Append(achievement.Description);
        else
            sb.Append(_textService.GetAddonText(3385)); // "???"

        if (Config.ShowCompletionStatus)
        {
            sb.BeginMacro(MacroCode.NewLine).EndMacro();

            if (achievements.IsLoaded())
            {
                sb.PushColorType(isComplete ? 43u : 518);

                sb.Append(_textService.Translate(isComplete
                    ? "AchievementLinkTooltip.AchievementComplete"
                    : "AchievementLinkTooltip.AchievementUnfinished"));

                sb.PopColorType();
            }
            else
            {
                sb.PushColorType(3);
                sb.Append(_textService.Translate("AchievementLinkTooltip.AchievementsNotLoaded"));
                sb.PopColorType();
            }
        }

        _tooltipText->SetString(sb.ToArray());

        // ShowTooltip call @ AddonChatLog_OnRefresh, case 0x12
        AtkStage.Instance()->TooltipManager.ShowTooltip(
            addon->Id,
            (AtkResNode*)addon->PanelCollisionNode,
            _tooltipText->StringPtr.Value);
    }
}
