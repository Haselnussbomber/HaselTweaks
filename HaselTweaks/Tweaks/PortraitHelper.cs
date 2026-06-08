using System.Threading;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows;

using AgentEvent = Dalamud.Game.Agent.AgentEvent;
using DAgentId = Dalamud.Game.Agent.AgentId;
using DSeString = Dalamud.Game.Text.SeStringHandling.SeString;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PortraitHelper : ConfigurableTweak<PortraitHelperConfiguration>
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(500);

    public static ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;
    public static PortraitPreset? ClipboardPreset { get; set; }

    private readonly IChatGui _chatGui;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ITextureProvider _textureProvider;
    private readonly IAgentLifecycle _agentLifecycle;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly AddonObserver _addonObserver;
    private readonly MenuBar _menuBar;

    private CancellationTokenSource? _mismatchCheckCTS;
    private DalamudLinkPayload? _openPortraitEditPayload;

    private Hook<UIClipboard.Delegates.OnClipboardDataChanged>? _onClipboardDataChangedHook;
    private Hook<RaptureGearsetModule.Delegates.UpdateGearset>? _updateGearsetHook;
    private Hook<UpdateGearVisibilityDelegate>? _updateGearVisibility;
    private bool _wasBoundByDuty;
    private bool _blockBannerPreview;
    private bool _classJobChanged;

    private delegate void UpdateGearVisibilityDelegate(uint entityId, nint packet);

    public override void OnEnable()
    {
        _onClipboardDataChangedHook = _gameInteropProvider.HookFromAddress<UIClipboard.Delegates.OnClipboardDataChanged>(
            UIClipboard.MemberFunctionPointers.OnClipboardDataChanged,
            OnClipboardDataChangedDetour);

        _updateGearsetHook = _gameInteropProvider.HookFromAddress<RaptureGearsetModule.Delegates.UpdateGearset>(
            RaptureGearsetModule.MemberFunctionPointers.UpdateGearset,
            UpdateGearsetDetour);

        _updateGearVisibility = _gameInteropProvider.HookFromSignature<UpdateGearVisibilityDelegate>(
            "48 89 74 24 ?? 57 48 83 EC ?? 48 8B FA 8B D1",
            UpdateGearVisibilityDetour);

        _agentLifecycle.RegisterListener(AgentEvent.PreShow, DAgentId.BannerPreview, OnBannerPreviewPreShow);

        _onClipboardDataChangedHook.Enable();
        _updateGearsetHook.Enable();
        _updateGearVisibility.Enable();

        _openPortraitEditPayload = _chatGui.AddChatLinkHandler(1, OpenPortraitEditChatHandler);

        if (IsAddonOpen(AgentId.BannerEditor))
            OnAddonOpen("BannerEditor");

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _clientState.TerritoryChanged += OnTerritoryChanged;
        _clientState.ClassJobChanged += OnClassJobChange;
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _clientState.TerritoryChanged -= OnTerritoryChanged;
        _clientState.ClassJobChanged -= OnClassJobChange;

        if (_openPortraitEditPayload != null)
            _chatGui.RemoveChatLinkHandler(_openPortraitEditPayload.CommandId);

        _menuBar.Close();

        _onClipboardDataChangedHook?.Dispose();
        _onClipboardDataChangedHook = null;

        _updateGearsetHook?.Dispose();
        _updateGearsetHook = null;

        _updateGearVisibility?.Dispose();
        _updateGearVisibility = null;

        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS?.Dispose();
        _mismatchCheckCTS = null;

        _agentLifecycle.UnregisterListener(AgentEvent.PreShow, DAgentId.BannerPreview, OnBannerPreviewPreShow);
    }

    private void OpenPortraitEditChatHandler(uint commandId, DSeString message)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();
        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;
        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        AgentGearSet.Instance()->OpenBannerEditorForGearset(gearsetId);
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName == "BannerEditor")
            _menuBar.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "BannerEditor")
            _menuBar.Close();
    }

    private void RestartCheck()
    {
        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS = new();

        _ = _framework.RunOnTick(
            CheckForGearChecksumMismatch,
            CheckDelay,
            cancellationToken: _mismatchCheckCTS.Token);
    }

    private void OnTerritoryChanged(uint territoryTypeId)
    {
        if (_wasBoundByDuty && !Conditions.Instance()->BoundByDuty56)
        {
            _wasBoundByDuty = false;
            RestartCheck();
        }
    }

    private void OnClipboardDataChangedDetour(UIClipboard* uiClipboard)
    {
        _onClipboardDataChangedHook!.Original(uiClipboard);

        try
        {
            ClipboardPreset = PortraitPreset.FromExportedString(uiClipboard->Data.SystemClipboardText.ToString());
            if (ClipboardPreset != null)
                _logger.LogDebug("Parsed ClipboardPreset: {ClipboardPreset}", ClipboardPreset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading preset");
        }
    }

    private int UpdateGearsetDetour(RaptureGearsetModule* raptureGearsetModule, int gearsetId)
    {
        if (_config.AutoUpdatePotraitOnGearUpdate)
            _blockBannerPreview = true;

        _logger.LogTrace("[UpdateGearsetDetour] Changed to {gearsetId}", gearsetId);

        var ret = _updateGearsetHook!.Original(raptureGearsetModule, gearsetId);

        RestartCheck();

        return ret;
    }

    private void UpdateGearVisibilityDetour(uint entityId, nint packet)
    {
        _updateGearVisibility!.Original(entityId, packet);

        if (!_classJobChanged || entityId != Control.Instance()->LocalPlayerEntityId)
            return;

        _logger.LogTrace("[UpdateGearVisibilityDetour] Updated LocalPlayer's gear visibility after Class/Job change");

        RestartCheck();
    }

    private void OnBannerPreviewPreShow(AgentEvent type, AgentArgs args)
    {
        var blockBannerPreview = _blockBannerPreview;
        _blockBannerPreview = false;

        if (_config.AutoUpdatePotraitOnGearUpdate && blockBannerPreview)
        {
            _logger.LogTrace("[OnBannerPreviewPreShow] Suppressed!");
            args.PreventOriginal();
        }
    }

    private void OnClassJobChange(uint classJobId)
    {
        _classJobChanged |= true;

        _logger.LogTrace("[OnClassJobChange] Changed to {classJobId}", classJobId);

        RestartCheck();
    }

    private void CheckForGearChecksumMismatch()
    {
        if (Conditions.Instance()->DutyRecorderPlayback)
            return;

        if (Conditions.Instance()->BoundByDuty56) // delay when bound by duty
        {
            _wasBoundByDuty = true;
            return;
        }

        if (Conditions.Instance()->BetweenAreas) // requeue when moving
        {
            RestartCheck();
            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;

        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
            return;

        if (_config.IgnoreDoHDoL && (!_excelService.TryGetRow<ClassJob>(gearset->ClassJob, out var classJobRow) || classJobRow.DohDolJobIndex != -1))
            return;

        var bannerIndex = gearset->BannerIndex;
        if (bannerIndex == 0) // no banner linked
            return;

        var bannerModule = BannerModule.Instance();
        var bannerId = bannerModule->GetBannerIdByBannerIndex(bannerIndex - 1);
        if (bannerId < 0) // banner not found
            return;

        var banner = bannerModule->GetBannerById(bannerId);
        if (banner == null) // banner not found
            return;

        var checksum = UIGlobals.GenerateEquippedItemsChecksum();
        var classJobChanged = Interlocked.Exchange(ref _classJobChanged, false);

        if (banner->Checksum == checksum)
        {
            _logger.LogInformation("Gear checksum matches! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);
            return;
        }

        _logger.LogInformation("Gear checksum mismatch detected! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X}, ClassJobChanged = {classJobChanged})", banner->Checksum, checksum, classJobChanged);

        if (!classJobChanged && _config.ReequipGearsetOnUpdate && gearset->GlamourSetLink > 0 && UIGlobals.CanApplyGlamourPlates())
        {
            _logger.LogInformation("Re-equipping Gearset #{gearsetId} to reapply glamour plate", gearset->Id + 1);
            raptureGearsetModule->EquipGearset(gearset->Id, gearset->GlamourSetLink);
            RecheckGearChecksum(banner);
        }
        else if (!classJobChanged && _config.AutoUpdatePotraitOnGearUpdate && gearset->GlamourSetLink == 0)
        {
            _logger.LogInformation("Trying to send portrait update...");

            if (SendPortraitUpdate(banner))
            {
                RecheckGearChecksum(banner);
            }
            else
            {
                AgentBannerPreview.Instance()->Show();
            }
        }
        else if (_config.NotifyGearChecksumMismatch)
        {
            NotifyMismatch();
        }
    }

    private void RecheckGearChecksum(BannerModuleEntry* banner)
    {
        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS = new();

        _ = _framework.RunOnTick(() =>
        {
            var checksum = UIGlobals.GenerateEquippedItemsChecksum();

            if (banner->Checksum != checksum)
            {
                _logger.LogInformation("Gear checksum still mismatching (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, checksum);
                NotifyMismatch();
            }
            else
            {
                _logger.LogInformation("Gear checksum matches now (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, checksum);
            }
        }, delay: CheckDelay, cancellationToken: _mismatchCheckCTS.Token);
    }

    private void NotifyMismatch()
    {
        var text = _textService.Translate("PortraitHelper.GearChecksumMismatch"); // based on LogMessage#5876

        using var rssb = new RentedSeStringBuilder();
        var sb = rssb.Builder
            .AppendHaselTweaksPrefix();

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            if (_openPortraitEditPayload != null)
            {
                sb.Append((ReadOnlySeStringSpan)_openPortraitEditPayload.Encode())
                  .Append(text)
                  .PopLink();
            }
            else
            {
                sb.Append(text);
            }
        }
        else
        {
            sb.Append(text);
        }

        UIModule.Instance()->ShowErrorText(text, false);

        Chat.PrintError(sb.GetViewAsSpan());
    }

    private bool SendPortraitUpdate(BannerModuleEntry* banner)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;
        if (!raptureGearsetModule->IsValidGearset(gearsetId))
        {
            _logger.LogWarning("No Portrait Update: Gearset invalid");
            return false;
        }

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
        {
            _logger.LogWarning("No Portrait Update: Gearset is null");
            return false;
        }

        var bannerIndex = gearset->BannerIndex;
        if (bannerIndex == 0) // no banner linked
        {
            _logger.LogInformation("No Portrait Update: Gearset not linked to Banner");
            return false;
        }

        if (banner->BannerIndex != bannerIndex - 1)
        {
            _logger.LogWarning("No Portrait Update: Banner index mismatch (Banner: {bannerIndex}, Gearset Banner Link: {gearsetBannerIndex})", banner->BannerIndex, bannerIndex - 1);
            return false;
        }

        var currentChecksum = UIGlobals.GenerateEquippedItemsChecksum();
        if (banner->Checksum == currentChecksum)
        {
            _logger.LogInformation("No Portrait Update: Checksum still matches");
            return false;
        }

        var localPlayer = (Character*)Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            _logger.LogWarning("No Portrait Update: LocalPlayer is null");
            return false;
        }

        var helper = UIModule.Instance()->GetUIModuleHelpers()->BannerHelper;
        if (!helper->BannerModuleEntry_IsCurrentCharaCardBannerOutdated(banner, true))
        {
            _logger.LogWarning("No Portrait Update: Banner expired");
            return false;
        }

        if (!helper->BannerModuleEntry_IsCharacterDataOutdated(banner, true))
        {
            _logger.LogWarning("No Portrait Update: Banner character data expired");
            return false;
        }

        // update Banner
        banner->LastUpdated = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        banner->Checksum = currentChecksum;
        helper->BannerModuleEntry_ApplyRaceGenderHeightTribe(banner, localPlayer);
        BannerModule.Instance()->UserFileEvent.HasChanges = true;

        var bannerData = new BannerData();
        helper->BannerData_ApplyBannerModuleEntry(&bannerData, banner);
        var result = helper->SendBannerData(&bannerData);
        if (result)
        {
            _logger.LogInformation("Portrait Update sent");
        }
        else
        {
            _logger.LogWarning("Portrait Update failed to send");
        }

        return result;
    }
}
