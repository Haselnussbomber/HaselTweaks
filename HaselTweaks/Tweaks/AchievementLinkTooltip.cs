using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Text;
using Lumina.Text.Payloads;
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

    public string InternalName => nameof(AchievementLinkTooltip);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

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

        Status = TweakStatus.Disposed;
    }

    private void OnChatLogPanelPostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var unitBase = (AtkUnitBase*)args.Addon;

        if (!unitBase->IsReady || *(byte*)(args.Addon + 0x3A1) != 0 || *(byte*)(args.Addon + 0x3DE) != 0)
            return;

        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.AtkEventType != (byte)AtkEventType.LinkMouseOver)
            return;

        var eventData = *(AtkEventData*)receiveEventArgs.Data;
        var linkData = eventData.LinkData;
        var linkType = (LinkMacroPayloadType)linkData->LinkType;
        if (linkType is not LinkMacroPayloadType.Achievement)
            return;

        if (!_excelService.TryGetRow<Achievement>(linkData->UIntValue1, out var achievement))
            return;

        using var tooltipText = new Utf8String();

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

        tooltipText.SetString(sb.ToArray());

        // ShowTooltip call @ AddonChatLog_OnRefresh, case 0x12
        AtkStage.Instance()->TooltipManager.ShowTooltip(
            unitBase->Id,
            *(AtkResNode**)(args.Addon + 0x248),
            tooltipText.StringPtr);
    }
}
