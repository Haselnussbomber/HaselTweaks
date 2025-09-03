using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedExpBar : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IClientState _clientState;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private Hook<AgentHUD.Delegates.UpdateExp>? _updateExpHook;
    private byte _colorMultiplyRed = 100;
    private byte _colorMultiplyGreen = 100;
    private byte _colorMultiplyBlue = 100;

    public override void OnEnable()
    {
        _updateExpHook = _gameInteropProvider.HookFromAddress<AgentHUD.Delegates.UpdateExp>(
            AgentHUD.MemberFunctionPointers.UpdateExp,
            UpdateExpDetour);
        _updateExpHook?.Enable();

        _clientState.LeavePvP += OnLeavePvP;
        _clientState.TerritoryChanged += OnTerritoryChanged;

        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        TriggerReset();
    }

    public override void OnDisable()
    {
        _clientState.LeavePvP -= OnLeavePvP;
        _clientState.TerritoryChanged -= OnTerritoryChanged;

        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        _updateExpHook?.Dispose();
        _updateExpHook = null;

        if (Status is TweakStatus.Enabled)
            TriggerReset();
    }

    private void OnLeavePvP()
        => TriggerReset();

    private void OnTerritoryChanged(ushort territoryType)
        => TriggerReset();

    private void UpdateExpDetour(AgentHUD* thisPtr, NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray)
    {
        _updateExpHook!.Original(thisPtr, expNumberArray, expStringArray, characterStringArray);

        if (!PlayerState.Instance()->IsLoaded || !_excelService.TryGetRow<ClassJob>(PlayerState.Instance()->CurrentClassJobId, out var classJob))
            return;

        SetColor(); // reset unless overwritten

        if (Config.ForceCompanionBar && OverwriteWithCompanionBar(classJob))
            return;

        if (Config.ForcePvPSeriesBar && _excelService.TryGetRow<TerritoryType>(GameMain.Instance()->CurrentTerritoryTypeId, out var territoryType) && territoryType.IsPvpZone && OverwriteWithPvPBar(classJob))
            return;

        if (Config.ForceSanctuaryBar && OverwriteWithSanctuaryBar(classJob))
            return;

        if (Config.ForceCosmicResearchBar && OverwriteWithCosmicResearchBar(classJob))
            return;

        if (!thisPtr->ExpFlags.HasFlag(AgentHudExpFlag.MaxLevel))
            return;

        switch (Config.MaxLevelOverride)
        {
            case MaxLevelOverrideType.PvPSeriesBar:
                OverwriteWithPvPBar(classJob);
                return;

            case MaxLevelOverrideType.CompanionBar:
                OverwriteWithCompanionBar(classJob);
                return;
        }
    }

    private void OnAddonExpPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;

        var gaugeBarNode = addon->GetComponentByNodeId(6);
        if (gaugeBarNode == null)
            return;

        var nineGridNode = gaugeBarNode->GetNineGridNodeById(4);
        if (nineGridNode == null)
            return;

        if (nineGridNode->MultiplyRed != _colorMultiplyRed)
            nineGridNode->MultiplyRed = _colorMultiplyRed;

        if (nineGridNode->MultiplyGreen != _colorMultiplyGreen)
            nineGridNode->MultiplyGreen = _colorMultiplyGreen;

        if (nineGridNode->MultiplyBlue != _colorMultiplyBlue)
            nineGridNode->MultiplyBlue = _colorMultiplyBlue;
    }

    private bool OverwriteWithCompanionBar(ClassJob classJob)
    {
        var buddy = UIState.Instance()->Buddy.CompanionInfo;

        if (buddy.Companion == null || buddy.Companion->EntityId == 0xE0000000)
            return false;

        if (!_excelService.TryGetRow<BuddyRank>(buddy.Rank, out var buddyRank))
            return false;

        var levelLabel = _textService.GetAddonText(4968).Trim().Replace(":", ""); // "Rank:"
        var rank = buddy.Rank > 20 ? 20 : buddy.Rank;
        var level = rank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var requiredExperience = buddyRank.ExpRequired;
        var xpText = requiredExperience == 0 ? "" : $"   {buddy.CurrentXP}/{requiredExperience}";

        SetText($"{classJob.Abbreviation}  {levelLabel} {level}{xpText}");
        SetExperience((int)buddy.CurrentXP, (int)requiredExperience);

        return true;
    }

    private bool OverwriteWithPvPBar(ClassJob classJob)
    {
        var pvpProfile = PvPProfile.Instance();
        if (pvpProfile == null || !pvpProfile->IsLoaded || !_excelService.TryGetRow<PvPSeriesLevel>(pvpProfile->GetSeriesCurrentRank(), out var pvpSeriesLevel))
            return false;

        var claimedRank = pvpProfile->GetSeriesClaimedRank();
        var currentRank = pvpProfile->GetSeriesCurrentRank();

        var levelLabel = _textService.GetAddonText(14860).Trim().Replace(":", ""); // "Series Level: "
        var rank = currentRank > 30 ? 30 : currentRank; // 30 = Series Max Rank, hopefully in the future too
        var level = rank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var star = currentRank > claimedRank ? '*' : ' ';
        var requiredExperience = pvpSeriesLevel.ExpToNext;

        SetText($"{classJob.Abbreviation}  {levelLabel} {level}{star}   {pvpProfile->SeriesExperience}/{requiredExperience}");
        SetExperience(pvpProfile->SeriesExperience, requiredExperience);

        if (!Config.DisableColorChanges)
            SetColor(65, 35); // trying to make it look like the xp bar in the PvP Profile window and failing miserably. eh, good enough

        return true;
    }

    private bool OverwriteWithSanctuaryBar(ClassJob classJob)
    {
        if (GameMain.Instance()->CurrentTerritoryIntendedUseId != 49)
            return false;

        var mjiManager = MJIManager.Instance();
        if (mjiManager == null || !_excelService.TryGetRow<MJIRank>(mjiManager->IslandState.CurrentRank, out var mjiRank))
            return false;

        var job = Config.SanctuaryBarHideJob ? "" : classJob.Abbreviation + "  ";
        var levelLabel = _textService.GetAddonText(14252).Trim().Replace(":", ""); // "Sanctuary Rank:"
        var level = mjiManager->IslandState.CurrentRank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var requiredExperience = mjiRank.ExpToNext;

        var expStr = mjiManager->IslandState.CurrentXP.ToString();
        var reqExpStr = requiredExperience.ToString();
        if (requiredExperience == 0)
            expStr = reqExpStr = "--";

        SetText($"{job}{levelLabel} {level}   {expStr}/{reqExpStr}");
        SetExperience((int)mjiManager->IslandState.CurrentXP, (int)requiredExperience);

        if (!Config.DisableColorChanges)
            SetColor(25, 60, 255); // blue seems nice.. just like the sky ^_^

        return true;
    }

    private bool OverwriteWithCosmicResearchBar(ClassJob classJob)
    {
        if (GameMain.Instance()->CurrentTerritoryIntendedUseId != 60)
            return false;

        var wksManager = WKSManager.Instance();
        if (wksManager == null)
            return false;

        var researchModule = wksManager->ResearchModule;
        if (researchModule == null || !researchModule->IsLoaded)
            return false;

        if (!(classJob.IsCrafter() || classJob.IsGatherer()))
            return false;

        var job = classJob.Abbreviation;
        var toolClassId = (byte)(classJob.RowId - 7);
        var stage = researchModule->CurrentStages[toolClassId - 1];
        var nextStage = researchModule->UnlockedStages[toolClassId - 1];
        var maxStage = _excelService.GetSheet<WKSCosmoToolPassiveBuff>().Max(row => row.Unknown0);

        if (stage == maxStage)
        {
            if (Config.ShowCosmicToolScore)
            {
                var score = wksManager->Scores[toolClassId - 1];
                if (score < 500000)
                {
                    var max = score switch
                    {
                        >= 150000 => 500000,
                        >= 50000 => 150000,
                        _ => 50000,
                    };

                    using var rssb = new RentedSeStringBuilder();
                    SetText(rssb.Builder
                        .Append(job)
                        .Append("  ")
                        .Append(_seStringEvaluator.EvaluateFromAddon(16852, [score]))
                        .Append(" / ")
                        .Append(_seStringEvaluator.EvaluateFromAddon(16852, [max]))
                        .GetViewAsSpan());

                    SetExperience(score, max);

                    if (!Config.DisableColorChanges)
                        SetColor(30, 60, 170);

                    return true;
                }
            }

            SetText($"{job} {_textService.GetAddonText(6167)}"); // Complete
            SetExperience(0, 0);

            return true;
        }

        if (!_excelService.TryGetRow<WKSCosmoToolClass>(toolClassId, out var toolClassRow))
            return false;

        byte selectedType = 0;
        var lowestPercentage = float.MaxValue;

        for (byte type = 1; type <= 5; type++)
        {
            if (!researchModule->IsTypeAvailable(toolClassId, type))
                break;

            var neededXP = researchModule->GetNeededAnalysis(toolClassId, type);
            if (neededXP == 0)
                continue;

            var currentXP = researchModule->GetCurrentAnalysis(toolClassId, type);
            if (currentXP >= neededXP)
                continue;

            var percentage = (float)currentXP / neededXP;
            if (percentage < lowestPercentage)
            {
                lowestPercentage = percentage;
                selectedType = type;
            }
        }

        if (selectedType == 0)
        {
            for (byte type = 1; type <= 5; type++)
            {
                if (!researchModule->IsTypeAvailable(toolClassId, type))
                    break;

                selectedType = type;
            }
        }

        if (selectedType == 0)
            return false;

        if (!_excelService.TryGetRow<WKSCosmoToolName>(toolClassRow.Types[selectedType - 1].Name.RowId, out var toolNameRow))
            return false;

        var toolName = toolNameRow.Name.ToString();
        var finalCurrentXP = researchModule->GetCurrentAnalysis(toolClassId, selectedType);
        var finalNeededXP = researchModule->GetNeededAnalysis(toolClassId, selectedType);
        var star = stage < nextStage ? '*' : ' ';

        SetText($"{job} {toolName}{star}   {finalCurrentXP}/{finalNeededXP}");
        SetExperience(finalCurrentXP, finalNeededXP);

        if (!Config.DisableColorChanges)
            SetColor(30, 60, 170);
        return true;
    }

    private void SetText(ReadOnlySpan<byte> span)
    {
        AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud)->SetValue(69, span);
    }

    private void SetText(string text)
    {
        AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud)->SetValue(69, text);
    }

    private void SetExperience(int experience, int maxExperience, int restedExperience = 0)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud);

        numberArray->SetValue(16, experience);
        numberArray->SetValue(17, maxExperience == 0 ? 0 : (int)MathF.Round(experience * 10000f / maxExperience));
        numberArray->SetValue(18, maxExperience);
        numberArray->SetValue(19, restedExperience);
        numberArray->SetValue(20, restedExperience == 0 ? 0 : (int)MathF.Round(restedExperience * 10000f / maxExperience));
    }

    private void TriggerReset()
    {
        // trigger update with wrong data
        var agentHUD = AgentHUD.Instance();

        agentHUD->ExpCurrentExperience = 0;
        agentHUD->ExpNeededExperience = 0;
        agentHUD->ExpRestedExperience = 0;
        agentHUD->CharacterClassJobId = 0;

        agentHUD->ExpClassJobId = 0;
        agentHUD->ExpLevel = 0;
        agentHUD->ExpContentLevel = 0;

        agentHUD->ExpFlags = AgentHudExpFlag.None;

        SetColor();
    }

    private void SetColor(byte red = 100, byte green = 100, byte blue = 100)
    {
        if (_colorMultiplyRed != red)
            _colorMultiplyRed = red;

        if (_colorMultiplyGreen != green)
            _colorMultiplyGreen = green;

        if (_colorMultiplyBlue != blue)
            _colorMultiplyBlue = blue;
    }
}
