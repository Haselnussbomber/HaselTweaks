using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedExpBar : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IClientState _clientState;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;
    private readonly SeStringEvaluator _seStringEvaluator;

    private Hook<AgentHUD.Delegates.UpdateExp>? _updateExpHook;
    private byte _colorMultiplyRed = 100;
    private byte _colorMultiplyGreen = 100;
    private byte _colorMultiplyBlue = 100;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _updateExpHook = _gameInteropProvider.HookFromAddress<AgentHUD.Delegates.UpdateExp>(
            AgentHUD.MemberFunctionPointers.UpdateExp,
            UpdateExpDetour);
    }

    public void OnEnable()
    {
        _clientState.LeavePvP += OnLeavePvP;
        _clientState.TerritoryChanged += OnTerritoryChanged;

        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        _updateExpHook?.Enable();

        TriggerReset();
    }

    public void OnDisable()
    {
        _clientState.LeavePvP -= OnLeavePvP;
        _clientState.TerritoryChanged -= OnTerritoryChanged;

        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        _updateExpHook?.Disable();

        if (Status is TweakStatus.Enabled)
            TriggerReset();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _updateExpHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnLeavePvP()
        => TriggerReset();

    private void OnTerritoryChanged(ushort territoryType)
        => TriggerReset();

    private void UpdateExpDetour(AgentHUD* thisPtr, NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray)
    {
        _updateExpHook!.Original(thisPtr, expNumberArray, expStringArray, characterStringArray);

        if (PlayerState.Instance()->IsLoaded == 0 || !_excelService.TryGetRow<ClassJob>(PlayerState.Instance()->CurrentClassJobId, out var classJob))
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
        var addon = (AtkUnitBase*)args.Addon;

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
        if (pvpProfile == null || pvpProfile->IsLoaded != 0x01 || !_excelService.TryGetRow<PvPSeriesLevel>(pvpProfile->GetSeriesCurrentRank(), out var pvpSeriesLevel))
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
        if (wksManager == null || wksManager->Research == null || !wksManager->Research->IsLoaded)
            return false;

        if (!(classJob.IsCrafter() || classJob.IsGatherer()))
            return false;

        var job = classJob.Abbreviation;
        var toolClassId = (byte)(classJob.RowId - 7);
        var stage = wksManager->Research->CurrentStages[toolClassId - 1];
        var nextStage = wksManager->Research->UnlockedStages[toolClassId - 1];

        if (stage == 9)
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

        if (!_excelService.TryGetRow<RawRow>("WKSCosmoToolClass", toolClassId, out var toolClassRow))
            return false;

        if (!_excelService.TryGetRow<RawRow>("WKSCosmoToolDataAmount", toolClassRow.ReadUInt8(0x8E), out var dataAmountRow)) // Unknown40
            return false;

        byte selectedType = 0;
        var lowestPercentage = float.MaxValue;

        for (byte type = 1; type <= 4; type++)
        {
            if (!wksManager->Research->IsTypeAvailable(toolClassId, type))
                break;

            var neededXP = wksManager->Research->GetNeededAnalysis(toolClassId, type);
            if (neededXP == 0)
                continue;

            var currentXP = wksManager->Research->GetCurrentAnalysis(toolClassId, type);
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
            for (byte type = 1; type <= 4; type++)
            {
                if (!wksManager->Research->IsTypeAvailable(toolClassId, type))
                    break;

                selectedType = type;
            }
        }

        if (selectedType == 0)
            return false;

        var toolNameId = toolClassRow.ReadUInt16((nuint)(8 * (selectedType - 1) + 0x70));

        if (!_excelService.TryGetRow<WKSCosmoToolName>(toolNameId, out var toolNameRow))
            return false;

        var toolName = toolNameRow.Unknown0.ExtractText();
        var finalCurrentXP = wksManager->Research->GetCurrentAnalysis(toolClassId, selectedType);
        var finalNeededXP = wksManager->Research->GetNeededAnalysis(toolClassId, selectedType);
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
