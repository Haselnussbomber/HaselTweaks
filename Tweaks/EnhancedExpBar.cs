using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using Lumina.Excel.GeneratedSheets;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace HaselTweaks.Tweaks;

public unsafe class EnhancedExpBar : Tweak
{
    public override string Name => "Enhanced Experience Bar";
    public override string Description => @"Enhances the Experience Bar with the following modes:

- The PvP Series Bar shows series rank and experience. A little * after the rank indicates a claimable rank-up reward.

- The Sanctuary Bar shows sanctuary level and island experience.";
    public override bool HasIncompatibilityWarning => Service.PluginInterface.PluginInternalNames.Contains("SimpleTweaksPlugin");
    public override string IncompatibilityWarning => "In order for this tweak to work properly, please make sure \"Show Experience Percentage\" is disabled in Simple Tweaks.";

    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.EnhancedExpBar;

    public enum MaxLevelOverrideType
    {
        [EnumOption("Default")]
        Default,

        [EnumOption("PvP Series Bar")]
        PvPSeriesBar,

        // Disabled because data is only available once loaded into the island. Sadge.
        //[EnumOption("Sanctuary Bar")]
        //SanctuaryBar
    }

    public class Configuration
    {
        [ConfigField(
            Label = "Always show PvP Series Bar in PvP Areas",
            OnChange = nameof(RequestUpdate)
        )]
        public bool ForcePvPSeriesBar = true;

        [ConfigField(
            Label = "Always show Sanctuary Bar on the Island",
            OnChange = nameof(RequestUpdate)
        )]
        public bool ForceSanctuaryBar = true;

        [ConfigField(
            Label = "Hide Job on Sanctuary Bar",
            OnChange = nameof(RequestUpdate)
        )]
        public bool SanctuaryBarHideJob = false;

        [ConfigField(
            Label = "Max Level Override",
            Description = "Will switch to the selected bar if your current job is on max level and none of the settings above apply.",
            Type = ConfigFieldTypes.SingleSelect,
            Options = nameof(MaxLevelOverrideType),
            OnChange = nameof(RequestUpdate)
        )]
        public MaxLevelOverrideType MaxLevelOverride = MaxLevelOverrideType.Default;

        [ConfigField(
            Label = "Disable color change",
            OnChange = nameof(RequestUpdate)
        )]
        public bool DisableColorChanges = false;
    }

    [AutoHook, Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 30 48 8B 72 18", DetourName = nameof(OnRequestedUpdateDetour))]
    private Hook<OnRequestedUpdateDelegate> OnRequestedUpdateHook { get; init; } = null!;
    private delegate IntPtr OnRequestedUpdateDelegate(AddonExp* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    public override void Enable()
    {
        Service.ClientState.LeavePvP += ClientState_LeavePvP;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        RequestUpdate();
    }

    public override void Disable()
    {
        Service.ClientState.LeavePvP -= ClientState_LeavePvP;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        RequestUpdate();
    }

    // probably the laziest way to detect if xp has changed
    private ushort LastSeriesXp = 0;
    private uint LastIslandExperience = 0;
    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        var shouldUpdate = false;

        var pvpState = PvPState.Instance();
        if (pvpState != null && pvpState->IsLoaded == 0x01 && LastSeriesXp != pvpState->SeriesExperience)
        {
            shouldUpdate = true;
            LastSeriesXp = pvpState->SeriesExperience;
        }

        var islandState = MJIManager.Instance();
        if (islandState != null && LastIslandExperience != islandState->CurrentXP)
        {
            shouldUpdate = true;
            LastIslandExperience = islandState->CurrentXP;
        }

        if (shouldUpdate)
        {
            RunUpdate(true);
            shouldUpdate = false;
        }
    }

    private void ClientState_LeavePvP()
    {
        // request update immediately upon leaving the pvp area, because
        // otherwise it might get updated too late, like a second after the black screen ends
        RequestUpdate();
    }

    private void ClientState_TerritoryChanged(object? sender, ushort territoryType)
    {
        RequestUpdate();
    }

    private void RequestUpdate()
    {
        RunUpdate(false);
    }

    private void RunUpdate(bool useDetour = false)
    {
        var addon = (AddonExp*)AtkUtils.GetUnitBase("_Exp");
        if (addon == null) return;

        var framework = Framework.Instance();
        if (framework == null) return;

        var uiModule = framework->GetUiModule();
        if (uiModule == null) return;

        var raptureAtkModule = uiModule->GetRaptureAtkModule();
        if (raptureAtkModule == null) return;

        if (useDetour)
        {
            OnRequestedUpdateDetour(
                addon,
                raptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays,
                raptureAtkModule->AtkModule.AtkArrayDataHolder.StringArrays
            );
        }
        else
        {
            // to trigger a GaugeBar update. it'll fetch correct values from the number array
            addon->ClassJob--;
            addon->RequiredExp--;

            addon->OnUpdate(
                raptureAtkModule->AtkModule.AtkArrayDataHolder.NumberArrays,
                raptureAtkModule->AtkModule.AtkArrayDataHolder.StringArrays
            );
        }
    }

    private IntPtr OnRequestedUpdateDetour(AddonExp* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        if (addon->GaugeBarNode == null)
            goto OriginalOnRequestedUpdate;

        var nineGridNode = (AtkNineGridNode*)addon->GaugeBarNode->AtkComponentBase.UldManager.SearchNodeById(4);
        if (nineGridNode == null)
            goto OriginalOnRequestedUpdate;

        if (Service.ClientState.LocalPlayer?.ClassJob.GameData == null || !Service.Data.IsDataReady)
            goto OriginalOnRequestedUpdateWithColorReset;

        var leftText = (AtkTextNode*)addon->AtkUnitBase.UldManager.SearchNodeById(4);
        if (leftText == null)
            goto OriginalOnRequestedUpdateWithColorReset;

        var ret = OnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);

        string job, levelLabel, level;
        uint requiredExperience;

        // --- forced bars in certain locations

        if (Config.ForcePvPSeriesBar && Service.ClientState.IsPvP) // TODO: only when PvP Series is active
            goto PvPBar;

        if (Config.ForceSanctuaryBar && Service.ClientState.TerritoryType == 1055) // TODO: is there a better way to check if we are on the island?
            goto SanctuaryBar;

        // --- max level overrides

        if (Service.ClientState.LocalPlayer.Level == PlayerState.Instance()->MaxLevel)
        {
            if (Config.MaxLevelOverride == MaxLevelOverrideType.PvPSeriesBar)
                goto PvPBar;

            //if (Config.MaxLevelOverride == MaxLevelOverrideType.SanctuaryBar)
            //    goto SanctuaryBar;
        }

        goto OriginalOnRequestedUpdateWithColorReset;

        PvPBar:
        {
            var pvpState = PvPState.Instance();
            if (pvpState == null || pvpState->IsLoaded != 0x01)
                goto OriginalOnRequestedUpdateWithColorReset;

            var PvPSeriesLevelSheet = Service.Data.GetExcelSheet<PvPSeriesLevel>();
            if (PvPSeriesLevelSheet == null || pvpState->SeriesCurrentRank > PvPSeriesLevelSheet.Count() - 1)
                goto OriginalOnRequestedUpdateWithColorReset;

            job = Service.ClientState.LocalPlayer.ClassJob.GameData.Abbreviation;
            levelLabel = (Service.StringUtils.GetSheetText<Addon>(14860, "Text") ?? "Series Level").Trim().Replace(":", "");
            var rank = pvpState->SeriesClaimedRank > 30 ? pvpState->SeriesCurrentRank : pvpState->SeriesClaimedRank; // 30 = Series Max Rank
            level = rank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
            var star = pvpState->SeriesClaimedRank > pvpState->SeriesCurrentRank ? '*' : ' ';
            requiredExperience = PvPSeriesLevelSheet.GetRow(pvpState->SeriesCurrentRank)!.Unknown0;

            leftText->SetText($"{job}  {levelLabel} {level}{star}   {pvpState->SeriesExperience}/{requiredExperience}");

            addon->GaugeBarNode->SetSecondaryValue(0); // rested experience bar

            // max value is set to 10000 in AddonExp_OnSetup and we won't change that, so adjust
            addon->GaugeBarNode->SetValue((uint)(pvpState->SeriesExperience / (float)requiredExperience * 10000), 0, false);

            if (!Config.DisableColorChanges)
            {
                // trying to make it look like the xp bar in the PvP Profile window and failing miserably. eh, good enough
                nineGridNode->AtkResNode.MultiplyRed = 65;
                nineGridNode->AtkResNode.MultiplyGreen = 35;
            }
            else
            {
                ResetColor(nineGridNode);
            }

            return ret;
        }

        SanctuaryBar:
        {
            var islandState = MJIManager.Instance();
            if (islandState == null)
                goto OriginalOnRequestedUpdateWithColorReset;

            var MJIRankSheet = Service.Data.GetExcelSheet<MJIRank>();
            if (MJIRankSheet == null || islandState->CurrentRank > MJIRankSheet.Count() - 1)
                goto OriginalOnRequestedUpdateWithColorReset;

            job = Config.SanctuaryBarHideJob ? "" : Service.ClientState.LocalPlayer.ClassJob.GameData.Abbreviation + "  ";
            levelLabel = (Service.StringUtils.GetSheetText<Addon>(14252, "Text") ?? "Sanctuary Rank").Trim().Replace(":", "");
            level = islandState->CurrentRank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
            requiredExperience = MJIRankSheet.GetRow(islandState->CurrentRank)!.ExpToNext;

            var expStr = islandState->CurrentXP.ToString();
            var reqExpStr = requiredExperience.ToString();
            if (requiredExperience == 0)
            {
                expStr = reqExpStr = "--";
            }

            leftText->SetText($"{job}{levelLabel} {level}   {expStr}/{reqExpStr}");

            addon->GaugeBarNode->SetSecondaryValue(0); // rested experience bar

            // max value is set to 10000 in AddonExp_OnSetup and we won't change that, so adjust
            addon->GaugeBarNode->SetValue((uint)(islandState->CurrentXP / (float)requiredExperience * 10000), 0, false);

            if (!Config.DisableColorChanges)
            {
                // blue seems nice.. just like the sky ^_^
                nineGridNode->AtkResNode.MultiplyRed = 25;
                nineGridNode->AtkResNode.MultiplyGreen = 60;
                nineGridNode->AtkResNode.MultiplyBlue = 255;
            }
            else
            {
                ResetColor(nineGridNode);
            }

            return ret;
        }

        OriginalOnRequestedUpdateWithColorReset:
        ResetColor(nineGridNode);

        OriginalOnRequestedUpdate:
        return OnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);
    }

    private void ResetColor(AtkNineGridNode* nineGridNode)
    {
        if (nineGridNode->AtkResNode.MultiplyRed != 100)
            nineGridNode->AtkResNode.MultiplyRed = 100;

        if (nineGridNode->AtkResNode.MultiplyGreen != 100)
            nineGridNode->AtkResNode.MultiplyGreen = 100;

        if (nineGridNode->AtkResNode.MultiplyBlue != 100)
            nineGridNode->AtkResNode.MultiplyBlue = 100;
    }
}
