using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using Achievement = Lumina.Excel.GeneratedSheets.Achievement;

namespace HaselTweaks.Tweaks;

public class AchievementLinkTooltipConfiguration
{
    [BoolConfig]
    public bool ShowCompletionStatus = true;

    [BoolConfig]
    public bool PreventSpoiler = true;
}

[Tweak]
public unsafe class AchievementLinkTooltip : Tweak<AchievementLinkTooltipConfiguration>
{
    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], OnChatLogPanelPostReceiveEvent);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], OnChatLogPanelPostReceiveEvent);
    }

    private void OnChatLogPanelPostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        var unitBase = (AtkUnitBase*)args.Addon;

        if (!unitBase->IsReady || *(byte*)(args.Addon + 0x389) != 0 || *(byte*)(args.Addon + 0x3C6) != 0)
            return;

        if (args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.AtkEventType != (byte)AtkEventType.LinkMouseOver)
            return;

        var linkData = *(LinkData**)receiveEventArgs.Data;
        var linkType = (LinkMacroPayloadType)linkData->Type;
        if (linkType is not LinkMacroPayloadType.Achievement)
            return;

        var achievement = GetRow<Achievement>(linkData->Id);
        if (achievement == null)
            return;

        using var tooltipText = new Utf8String();

        ref var achievements = ref UIState.Instance()->Achievement;
        var isComplete = achievements.IsComplete((int)achievement.RowId);

        var canShowName = !Config.PreventSpoiler;
        var canShowDescription = !Config.PreventSpoiler;

        if (Config.PreventSpoiler)
        {
            var isHiddenCategory = achievement.AchievementCategory.Value?.HideCategory == true;
            var isHiddenName = achievement.AchievementHideCondition.Value?.HideName == true;
            var isHiddenAchievement = achievement.AchievementHideCondition.Value?.HideAchievement == true;

            canShowName |= !isHiddenName || isComplete;
            canShowDescription |= !(isHiddenCategory || isHiddenAchievement) || isComplete;
        }

        var sb = new SeStringBuilder();

        sb.BeginMacro(MacroCode.Color)
          .AppendIntExpression(RaptureTextModule.Instance()->TextModule.MacroDecoder.GlobalParameters[61].IntValue)
          .EndMacro();

        if (canShowName)
            sb.Append(new ReadOnlySeStringSpan(achievement.Name.RawData));
        else
            sb.Append(GetAddonText(3384)); // "???"

        sb.PopColor();
        sb.BeginMacro(MacroCode.NewLine).EndMacro();

        if (canShowDescription)
            sb.Append(new ReadOnlySeStringSpan(achievement.Description.RawData));
        else
            sb.Append(GetAddonText(3385)); // "???"

        if (Config.ShowCompletionStatus)
        {
            sb.BeginMacro(MacroCode.NewLine).EndMacro();

            if (achievements.IsLoaded())
            {
                sb.PushColorType(isComplete ? 43u : 518);

                sb.Append(isComplete
                    ? t("AchievementLinkTooltip.AchievementComplete")
                    : t("AchievementLinkTooltip.AchievementUnfinished"));

                sb.PopColorType();
            }
            else
            {
                sb.PushColorType(3);
                sb.Append(t("AchievementLinkTooltip.AchievementsNotLoaded"));
                sb.PopColorType();
            }
        }

        tooltipText.SetString(sb.ToArray());

        // ShowTooltip call @ AddonChatLog_OnRefresh, case 0x12
        AtkStage.Instance()->TooltipManager.ShowTooltip(
            unitBase->Id,
            *(AtkResNode**)(args.Addon + 0x230),
            tooltipText.StringPtr);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LinkData
    {
        [FieldOffset(0x10)] public byte* Payload;
        [FieldOffset(0x1B)] public byte Type;
        [FieldOffset(0x24)] public uint Id;
    }

    private static uint SwapRedBlue(uint value)
        => 0xFF000000 | ((value & 0x000000FF) << 16) | (value & 0x0000FF00) | ((value & 0x00FF0000) >> 16);
}
