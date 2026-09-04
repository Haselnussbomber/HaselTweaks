using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AchievementLinkTooltip : ConfigurableTweak<AchievementLinkTooltipConfiguration>
{
    private static readonly string[] ChatPanels = ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"];

    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly IAddonLifecycle _addonLifecycle;

    public override ValueTask OnEnable()
    {
        _disposables = _addonLifecycle.OnPostReceiveEvent(OnChatLogPanelPostReceiveEvent, ChatPanels);

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        return ValueTask.CompletedTask;
    }

    private void OnChatLogPanelPostReceiveEvent(AddonReceiveEventArgs args)
    {
        var addon = args.GetAddon<AddonChatLogPanel>();

        if (!addon->IsReady || addon->LogViewer.IsSelectingText || addon->IsResizing)
            return;

        if (args.EventType != AtkEventType.LinkMouseOver)
            return;

        var eventData = args.GetEventData<AtkEventData>();
        var linkData = eventData->LinkData;
        if (linkData == null)
            return;

        var linkType = (LinkMacroPayloadType)linkData->LinkType;
        if (linkType != LinkMacroPayloadType.Achievement)
            return;

        if (!_excelService.TryGetRow<Achievement>(linkData->UIntValue1, out var achievement))
            return;

        ref var achievements = ref UIState.Instance()->Achievement;
        var isComplete = achievements.IsComplete((int)achievement.RowId);

        var canShowName = !_config.PreventSpoiler;
        var canShowDescription = !_config.PreventSpoiler;

        if (_config.PreventSpoiler)
        {
            var isHiddenCategory = achievement.AchievementCategory.ValueNullable?.HideCategory == true;
            var isHiddenName = achievement.AchievementHideCondition.ValueNullable?.HideName == true;
            var isHiddenAchievement = achievement.AchievementHideCondition.ValueNullable?.HideAchievement == true;

            canShowName |= !isHiddenName || isComplete;
            canShowDescription |= !(isHiddenCategory || isHiddenAchievement) || isComplete;
        }

        using var rssb = new RentedSeStringBuilder();
        var sb = rssb.Builder;

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

        if (_config.ShowCompletionStatus)
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

        // ShowTooltip call @ AddonChatLog_OnRefresh, case 0x12
        AtkStage.Instance()->TooltipManager.ShowTooltip(
            addon->Id,
            (AtkResNode*)addon->PanelCollisionNode,
            sb.GetViewAsSpan());
    }
}
