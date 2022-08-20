using System;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class SeriesExpBar : Tweak
{
    public override string Name => "Series Exp Bar";
    public override bool HasDescription => true;
    public override void DrawDescription()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFBBBBBB);

        ImGui.TextWrapped("The experience bar shows series rank and experience instead. A little * after the rank indicates a claimable reward.");

        if (Service.PluginInterface.PluginInternalNames.Contains("SimpleTweaksPlugin"))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiUtils.DrawIcon(60073, 24, 24);
            ImGui.SameLine();
            ImGui.TextWrapped("In order for this tweak to work properly, please make sure to disable \"Show Experience Percentage\" in Simple Tweaks first.");
        }

        ImGui.PopStyleColor();
    }

    public static Configuration Config => HaselTweaks.Configuration.Instance.Tweaks.SeriesExpBar;

    public class Configuration
    {
        [ConfigField(Label = "Only show in PvP Areas")]
        public bool OnlyInPvPAreas = true;
    }

    [AutoHook, Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 30 48 8B 72 18", DetourName = nameof(OnRequestedUpdateDetour))]
    private Hook<OnRequestedUpdateDelegate> OnRequestedUpdateHook { get; init; } = null!;
    private delegate IntPtr OnRequestedUpdateDelegate(AddonExp* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    [Signature("48 8D 81 ?? ?? ?? ?? 89 54 24 10")]
    private GaugeBarSetRestedBarValueDelegate GaugeBarSetRestedBarValue { get; init; } = null!;
    private delegate uint GaugeBarSetRestedBarValueDelegate(AtkComponentGaugeBar* bar, uint value);

    [Signature("E8 ?? ?? ?? ?? 41 3B 1E")]
    private GaugeBarSetBarValueDelegate GaugeBarSetBarValue { get; init; } = null!;
    private delegate uint GaugeBarSetBarValueDelegate(AtkComponentGaugeBar* bar, uint value, uint a3, bool skipAnimation);

    public override void Enable()
    {
        Service.ClientState.LeavePvP += ClientState_LeavePvP;
        RequestUpdate();
    }

    public override void Disable()
    {
        Service.ClientState.LeavePvP -= ClientState_LeavePvP;
    }

    private void ClientState_LeavePvP()
    {
        // request update immediately upon leaving the pvp area, because
        // otherwise it might get updated too late, like a second after the black screen ends
        RequestUpdate();
    }

    private void RequestUpdate()
    {
        var addon = (AddonExp*)AtkUtils.GetUnitBase("_Exp");
        if (addon == null) return;

        // to trigger GaugeBar update without animation. it'll fetch correct values from the number array
        addon->ClassJob--;
        addon->RequiredExp--;

        var framework = Framework.Instance();
        if (framework == null) return;

        var uiModule = framework->GetUiModule();
        if (uiModule == null) return;

        var atkModule = uiModule->GetRaptureAtkModule();
        if (atkModule == null) return;

        addon->AtkUnitBase.OnUpdate(
            atkModule->AtkModule.AtkArrayDataHolder.NumberArrays,
            atkModule->AtkModule.AtkArrayDataHolder.StringArrays
        );
    }

    private IntPtr OnRequestedUpdateDetour(AddonExp* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        if (addon->GaugeBarNode == null)
            goto OriginalOnRequestedUpdate;

        var nineGridNode = (AtkNineGridNode*)addon->GaugeBarNode->AtkComponentBase.UldManager.SearchNodeById(4);
        if (nineGridNode == null)
            goto OriginalOnRequestedUpdate;

        if (Service.ClientState.LocalPlayer?.ClassJob.GameData == null || !Service.Data.IsDataReady || (Config.OnlyInPvPAreas && !Service.ClientState.IsPvP))
            goto OriginalOnRequestedUpdateWithColorReset;

        var pvpUiState = UIState_PvPState.Instance();
        if (pvpUiState == null || pvpUiState->IsLoaded != 0x01)
            goto OriginalOnRequestedUpdateWithColorReset;

        var PvPSeriesLevelSheet = Service.Data.GetExcelSheet<PvPSeriesLevel>();
        if (PvPSeriesLevelSheet == null || pvpUiState->SeriesRank > PvPSeriesLevelSheet.Count() - 1)
            goto OriginalOnRequestedUpdateWithColorReset;

        var leftText = (AtkTextNode*)addon->AtkUnitBase.UldManager.SearchNodeById(4);
        if (leftText == null)
            goto OriginalOnRequestedUpdateWithColorReset;

        var ret = OnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);

        var job = Service.ClientState.LocalPlayer.ClassJob.GameData.Abbreviation;
        var seriesLevelText = Service.Data.GetExcelSheet<Addon>()!.GetRow(14860)!.Text; // "Series Level: "
        var levelStr = pvpUiState->SeriesRank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var star = pvpUiState->SeriesRankWithOverflow > pvpUiState->SeriesRank ? '*' : ' ';
        var rankRequiredExperience = PvPSeriesLevelSheet.GetRow(pvpUiState->SeriesRank)!.Unknown0;

        leftText->SetText($"{job}  {seriesLevelText} {levelStr}{star}   {pvpUiState->SeriesExperience}/{rankRequiredExperience}");

        GaugeBarSetRestedBarValue(addon->GaugeBarNode, 0);

        // max value is set to 10000 in AddonExp_OnSetup and we won't change that, so adjust
        GaugeBarSetBarValue(addon->GaugeBarNode, (uint)(pvpUiState->SeriesExperience / (float)rankRequiredExperience * 10000), 0, false);

        // trying to make it look like the xp bar in the PvP Profile window and failing miserably. eh, good enough
        nineGridNode->AtkResNode.MultiplyRed = 65;
        nineGridNode->AtkResNode.MultiplyGreen = 35;

        return ret;

        OriginalOnRequestedUpdateWithColorReset:

        // reset colors if not in PvP mode
        if (nineGridNode->AtkResNode.MultiplyRed != 100)
            nineGridNode->AtkResNode.MultiplyRed = 100;

        if (nineGridNode->AtkResNode.MultiplyGreen != 100)
            nineGridNode->AtkResNode.MultiplyGreen = 100;

        OriginalOnRequestedUpdate:
        return OnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);
    }
}
