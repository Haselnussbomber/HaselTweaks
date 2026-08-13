using Dalamud.Game.Text.Evaluator;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TerritoryIntendedUse = FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedExpBar : ConfigurableTweak<EnhancedExpBarConfiguration>
{
    private readonly TextService _textService;
    private readonly IClientState _clientState;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;

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

    private void OnTerritoryChanged(uint territoryType)
        => TriggerReset();

    private void UpdateExpDetour(AgentHUD* thisPtr, NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray)
    {
        _updateExpHook!.Original(thisPtr, expNumberArray, expStringArray, characterStringArray);

        if (!PlayerState.Instance()->IsLoaded || !_excelService.TryGetRow<ClassJob>(PlayerState.Instance()->CurrentClassJobId, out var classJob))
            return;

        SetColor(); // reset unless overwritten

        // for safety, don't show when the game uses different texts than Addon#1019
        if (thisPtr->ExpFlags.HasFlag(AgentHudExpFlag.InEureka) || thisPtr->ExpFlags.HasFlag(AgentHudExpFlag.InOccultCrescent))
            return;

        var gameMain = GameMain.Instance();
        // don't show custom bars when in synced content
        if (thisPtr->ExpFlags.HasFlag(AgentHudExpFlag.Synced) && gameMain->CurrentContentFinderConditionId != 0)
            return;

        if (_config.ForceCompanionBar && OverwriteWithCompanionBar(classJob))
            return;

        if (_config.ForcePvPSeriesBar && _excelService.TryGetRow<TerritoryType>(gameMain->CurrentTerritoryTypeId, out var territoryType) && territoryType.IsPvpZone && OverwriteWithPvPBar(classJob))
            return;

        if (_config.ForceSanctuaryBar && OverwriteWithSanctuaryBar(classJob))
            return;

        if (!thisPtr->ExpFlags.HasFlag(AgentHudExpFlag.MaxLevel))
            return;

        switch (_config.MaxLevelOverride)
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
        var addon = args.GetAddon<AtkUnitBase>();

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
        ref var buddy = ref UIState.Instance()->Buddy.CompanionInfo;

        if (buddy.Companion == null || buddy.Companion->EntityId == 0xE0000000)
            return false;

        if (!_excelService.TryGetRow<BuddyRank>(buddy.Rank, out var buddyRank))
            return false;

        var levelText = _textService.GetAddonText(4968, _clientState.ClientLanguage).Trim().Replace(":", ""); // "Rank:"
        var currentRank = buddy.Rank;
        var maxRank = _excelService.GetRowCount<BuddyRank>() - 1;
        var rank = currentRank > maxRank ? maxRank : currentRank;
        var currentExp = (int)buddy.CurrentXP;
        var neededExp = (int)buddyRank.ExpRequired;

        SetText("EnhancedExpBar.CompanionBar.Format",
            classJob.RowId,
            levelText,
            rank.ToSeIconCharNumbers(),
            currentExp,
            neededExp);

        SetExperience(currentExp, neededExp);

        return true;
    }

    private bool OverwriteWithPvPBar(ClassJob classJob)
    {
        var pvpProfile = PvPProfile.Instance();
        if (!pvpProfile->IsLoaded)
            return false;

        var currentRank = pvpProfile->GetSeriesCurrentRank();

        if (!_excelService.TryGetRow<PvPSeriesLevel>(currentRank, out var pvpSeriesLevel))
            return false;

        var claimedRank = pvpProfile->GetSeriesClaimedRank();

        if (_config.HidePvPSeriesBarAfterClaimingAllRewards && currentRank == claimedRank && _excelService.TryGetRow<PvPSeries>(pvpProfile->Series, out var pvpSeries))
        {
            var maxRewardRank = pvpSeries.LevelRewards
                .Index()
                .Where(t => t.Item.LevelRewardItem[0].RowId != 0 && t.Item.LevelRewardItem[1].RowId != 36656)
                .Max(t => t.Index);

            if (maxRewardRank != 0 && maxRewardRank == claimedRank)
                return false;
        }

        var levelText = _textService.GetAddonText(14860, _clientState.ClientLanguage).Trim().Replace(":", ""); // "Series Level: "
        var maxRank = _excelService.GetRowCount<PvPSeriesLevel>() - 1;
        var rank = currentRank > maxRank ? maxRank : currentRank;
        var canClaimReward = currentRank > claimedRank;
        var currentExp = (int)pvpProfile->SeriesExperience;
        var neededExp = (int)pvpSeriesLevel.ExpToNext;

        SetText("EnhancedExpBar.PvPBar.Format",
            classJob.RowId,
            levelText,
            rank.ToSeIconCharNumbers(),
            canClaimReward ? 1 : 0,
            currentExp,
            neededExp);

        SetExperience(currentExp, neededExp);

        if (!_config.DisableColorChanges)
            SetColor(65, 35); // trying to make it look like the xp bar in the PvP Profile window and failing miserably. eh, good enough

        return true;
    }

    private bool OverwriteWithSanctuaryBar(ClassJob classJob)
    {
        if (GameMain.Instance()->CurrentTerritoryIntendedUseId != TerritoryIntendedUse.IslandSanctuary)
            return false;

        var mjiManager = MJIManager.Instance();
        if (mjiManager == null || !_excelService.TryGetRow<MJIRank>(mjiManager->IslandState.CurrentRank, out var mjiRank))
            return false;

        var rankText = _textService.GetAddonText(14252, _clientState.ClientLanguage).Trim().Replace(":", ""); // "Sanctuary Rank:"
        var rank = (int)mjiManager->IslandState.CurrentRank;
        var currentExp = (int)mjiManager->IslandState.CurrentXP;
        var neededExp = (int)mjiRank.ExpToNext;

        SetText("EnhancedExpBar.SanctuaryBar.Format",
            _config.SanctuaryBarHideJob ? 0 : 1,
            classJob.RowId,
            rankText,
            rank.ToSeIconCharNumbers(),
            currentExp,
            neededExp);

        SetExperience(currentExp, neededExp);

        if (!_config.DisableColorChanges)
            SetColor(25, 60, 255); // blue seems nice.. just like the sky ^_^

        return true;
    }

    private void SetText(string key, params Span<SeStringParameter> parameters)
    {
        var evaluated = _textService.EvaluateTranslatedSeString(key, _clientState.ClientLanguage, parameters);

        using var rssb = new RentedSeStringBuilder();

        AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud)->SetValue(69,
            rssb.Builder
                .Append(evaluated)
                .GetViewAsSpan());
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
