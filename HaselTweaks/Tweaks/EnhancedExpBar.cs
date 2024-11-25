using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Tweaks;

public unsafe partial class EnhancedExpBar(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    IClientState ClientState,
    IAddonLifecycle AddonLifecycle,
    IGameInteropProvider GameInteropProvider,
    ExcelService ExcelService)
    : IConfigurableTweak
{
    public string InternalName => nameof(EnhancedExpBar);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<HaselAgentHUD.Delegates.UpdateExp>? UpdateExpHook;
    private byte ColorMultiplyRed = 100;
    private byte ColorMultiplyGreen = 100;
    private byte ColorMultiplyBlue = 100;

    public void OnInitialize()
    {
        UpdateExpHook = GameInteropProvider.HookFromAddress<HaselAgentHUD.Delegates.UpdateExp>(
            HaselAgentHUD.MemberFunctionPointers.UpdateExp,
            UpdateExpDetour);
    }

    public void OnEnable()
    {
        ClientState.LeavePvP += OnLeavePvP;
        ClientState.TerritoryChanged += OnTerritoryChanged;

        AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        UpdateExpHook?.Enable();

        TriggerReset();
    }

    public void OnDisable()
    {
        ClientState.LeavePvP -= OnLeavePvP;
        ClientState.TerritoryChanged -= OnTerritoryChanged;

        AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_Exp", OnAddonExpPostRequestedUpdate);

        UpdateExpHook?.Disable();

        if (Status is TweakStatus.Enabled)
            TriggerReset();
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        UpdateExpHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnLeavePvP()
        => TriggerReset();

    private void OnTerritoryChanged(ushort territoryType)
        => TriggerReset();

    private void UpdateExpDetour(HaselAgentHUD* thisPtr, NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray)
    {
        UpdateExpHook!.Original(thisPtr, expNumberArray, expStringArray, characterStringArray);

        if (!ClientState.IsLoggedIn || ClientState.LocalPlayer == null || !ClientState.LocalPlayer.ClassJob.IsValid)
            return;

        SetColor(); // reset unless overwritten

        if (Config.ForceCompanionBar && UIState.Instance()->Buddy.CompanionInfo.Companion != null && UIState.Instance()->Buddy.CompanionInfo.Companion->EntityId != 0xE0000000)
        {
            OverwriteWithCompanionBar();
            return;
        }

        if (Config.ForcePvPSeriesBar && GameMain.IsInPvPArea())
        {
            OverwriteWithPvPBar();
            return;
        }

        if (Config.ForceSanctuaryBar && GameMain.Instance()->CurrentTerritoryIntendedUseId == 49)
        {
            OverwriteWithSanctuaryBar();
            return;
        }

        if (!((AgentHUD*)thisPtr)->ExpIsMaxLevel)
            return;

        switch (Config.MaxLevelOverride)
        {
            case MaxLevelOverrideType.PvPSeriesBar:
                OverwriteWithPvPBar();
                return;

            case MaxLevelOverrideType.CompanionBar:
                OverwriteWithCompanionBar();
                return;
        }
    }

    private void OnAddonExpPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon;

        var gaugeBarNode = GetNode<AtkComponentNode>(addon, 6);
        if (gaugeBarNode == null)
            return;

        var nineGridNode = GetNode<AtkNineGridNode>(gaugeBarNode->Component, 4);
        if (nineGridNode == null)
            return;

        if (nineGridNode->AtkResNode.MultiplyRed != ColorMultiplyRed)
            nineGridNode->AtkResNode.MultiplyRed = ColorMultiplyRed;

        if (nineGridNode->AtkResNode.MultiplyGreen != ColorMultiplyGreen)
            nineGridNode->AtkResNode.MultiplyGreen = ColorMultiplyGreen;

        if (nineGridNode->AtkResNode.MultiplyBlue != ColorMultiplyBlue)
            nineGridNode->AtkResNode.MultiplyBlue = ColorMultiplyBlue;
    }

    private void OverwriteWithCompanionBar()
    {
        var buddy = UIState.Instance()->Buddy.CompanionInfo;

        if (!ExcelService.TryGetRow<BuddyRank>(buddy.Rank, out var buddyRank))
            return;

        var job = ClientState.LocalPlayer!.ClassJob.Value.Abbreviation;
        var levelLabel = (TextService.GetAddonText(4968) ?? "Rank").Trim().Replace(":", "");
        var rank = buddy.Rank > 20 ? 20 : buddy.Rank;
        var level = rank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var requiredExperience = buddyRank.ExpRequired;
        var xpText = requiredExperience == 0 ? "" : $"   {buddy.CurrentXP}/{requiredExperience}";

        SetText($"{job}  {levelLabel} {level}{xpText}");
        SetExperience((int)buddy.CurrentXP, (int)requiredExperience);
    }

    private void OverwriteWithPvPBar()
    {
        var pvpProfile = PvPProfile.Instance();

        if (pvpProfile == null || pvpProfile->IsLoaded != 0x01 || !ExcelService.TryGetRow<PvPSeriesLevel>(pvpProfile->GetSeriesCurrentRank(), out var pvpSeriesLevel))
            return;

        var claimedRank = pvpProfile->GetSeriesClaimedRank();
        var currentRank = pvpProfile->GetSeriesCurrentRank();

        var job = ClientState.LocalPlayer!.ClassJob.Value.Abbreviation;
        var levelLabel = (TextService.GetAddonText(14860) ?? "Series Level").Trim().Replace(":", "");
        var rank = currentRank > 30 ? 30 : currentRank; // 30 = Series Max Rank, hopefully in the future too
        var level = rank.ToString().Aggregate("", (str, chr) => str + (char)(SeIconChar.Number0 + byte.Parse(chr.ToString())));
        var star = currentRank > claimedRank ? '*' : ' ';
        var requiredExperience = pvpSeriesLevel.Unknown0;

        SetText($"{job}  {levelLabel} {level}{star}   {pvpProfile->SeriesExperience}/{requiredExperience}");
        SetExperience(pvpProfile->SeriesExperience, requiredExperience);

        if (!Config.DisableColorChanges)
            SetColor(65, 35); // trying to make it look like the xp bar in the PvP Profile window and failing miserably. eh, good enough
    }

    private void OverwriteWithSanctuaryBar()
    {
        var mjiManager = MJIManager.Instance();

        if (mjiManager == null || !ExcelService.TryGetRow<MJIRank>(mjiManager->IslandState.CurrentRank, out var mjiRank))
            return;

        var job = Config.SanctuaryBarHideJob ? "" : ClientState.LocalPlayer!.ClassJob.Value.Abbreviation + "  ";
        var levelLabel = (TextService.GetAddonText(14252) ?? "Sanctuary Rank").Trim().Replace(":", "");
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

        agentHUD->ExpIsLevelSynced = false;
        agentHUD->ExpUnkBool2 = false;
        agentHUD->ExpIsMaxLevel = false;
        agentHUD->ExpIsInEureka = false;
    }

    private void SetColor(byte red = 100, byte green = 100, byte blue = 100)
    {
        if (ColorMultiplyRed != red)
            ColorMultiplyRed = red;

        if (ColorMultiplyGreen != green)
            ColorMultiplyGreen = green;

        if (ColorMultiplyBlue != blue)
            ColorMultiplyBlue = blue;
    }
}
