using System.Threading;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Game;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Extensions;
using HaselTweaks.Interfaces;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using Microsoft.Extensions.Logging;

using DSeString = Dalamud.Game.Text.SeStringHandling.SeString;
using LSeStringBuilder = Lumina.Text.SeStringBuilder;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PortraitHelper : IConfigurableTweak
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(500);

    public static ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;
    public static PortraitPreset? ClipboardPreset { get; set; }

    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ILogger<PortraitHelper> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly AddonObserver _addonObserver;
    private readonly MenuBar _menuBar;

    private CancellationTokenSource? _mismatchCheckCTS;
    private DalamudLinkPayload? _openPortraitEditPayload;

    private Hook<UIClipboard.Delegates.OnClipboardDataChanged>? _onClipboardDataChangedHook;
    private Hook<RaptureGearsetModule.Delegates.UpdateGearset>? _updateGearsetHook;
    private Hook<AgentBannerPreview.Delegates.Show>? _agentBannerPreviewShowHook;
    private bool _wasBoundByDuty;
    private bool _blockBannerPreview;

    public string InternalName => nameof(PortraitHelper);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _onClipboardDataChangedHook = _gameInteropProvider.HookFromAddress<UIClipboard.Delegates.OnClipboardDataChanged>(
            UIClipboard.MemberFunctionPointers.OnClipboardDataChanged,
            OnClipboardDataChangedDetour);

        _updateGearsetHook = _gameInteropProvider.HookFromAddress<RaptureGearsetModule.Delegates.UpdateGearset>(
            RaptureGearsetModule.MemberFunctionPointers.UpdateGearset,
            UpdateGearsetDetour);

        _agentBannerPreviewShowHook = _gameInteropProvider.HookFromAddress<AgentBannerPreview.Delegates.Show>(
            AgentBannerPreview.Instance()->VirtualTable->Show,
            AgentBannerPreviewShowDetour);
    }

    public void OnEnable()
    {
        _openPortraitEditPayload = _pluginInterface.AddChatLinkHandler(1000, OpenPortraitEditChatHandler);

        if (IsAddonOpen(AgentId.BannerEditor))
            OnAddonOpen("BannerEditor");

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _clientState.TerritoryChanged += OnTerritoryChanged;
        _clientState.ClassJobChanged += OnClassJobChange;

        _onClipboardDataChangedHook?.Enable();
        _updateGearsetHook?.Enable();
        _agentBannerPreviewShowHook?.Enable();
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _clientState.TerritoryChanged -= OnTerritoryChanged;
        _clientState.ClassJobChanged -= OnClassJobChange;

        _pluginInterface.RemoveChatLinkHandler(1000);

        _menuBar.Close();

        _onClipboardDataChangedHook?.Disable();
        _updateGearsetHook?.Disable();
        _agentBannerPreviewShowHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _onClipboardDataChangedHook?.Dispose();
        _updateGearsetHook?.Dispose();
        _agentBannerPreviewShowHook?.Dispose();

        Status = TweakStatus.Disposed;
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
        if (addonName != "BannerEditor")
            return;

        _menuBar.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName != "BannerEditor")
            return;

        _menuBar.Close();
    }

    private void OnTerritoryChanged(ushort territoryTypeId)
    {
        if (_wasBoundByDuty && !Conditions.Instance()->BoundByDuty56)
        {
            _wasBoundByDuty = false;

            _mismatchCheckCTS?.Cancel();
            _mismatchCheckCTS = new();

            _framework.RunOnTick(
                () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex),
                CheckDelay,
                cancellationToken: _mismatchCheckCTS.Token);
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
        if (Config.AutoUpdatePotraitOnGearUpdate)
            _blockBannerPreview = true;

        var ret = _updateGearsetHook!.Original(raptureGearsetModule, gearsetId);

        _blockBannerPreview = false;

        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS = new();

        _framework.RunOnTick(
            () => CheckForGearChecksumMismatch(gearsetId),
            CheckDelay,
            cancellationToken: _mismatchCheckCTS.Token);

        return ret;
    }

    private void AgentBannerPreviewShowDetour(AgentBannerPreview* thisPtr)
    {
        if (!Config.AutoUpdatePotraitOnGearUpdate)
        {
            _agentBannerPreviewShowHook!.Original(thisPtr);
            return;
        }

        if (_blockBannerPreview)
        {
            _blockBannerPreview = false;
            return;
        }

        _agentBannerPreviewShowHook!.Original(thisPtr);
    }

    private void OnClassJobChange(uint classJobId)
    {
        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS = new();

        _framework.RunOnTick(
            () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex, true),
            CheckDelay,
            cancellationToken: _mismatchCheckCTS.Token);
    }

    private void CheckForGearChecksumMismatch(int gearsetId, bool isJobChange = false)
    {
        if (Conditions.Instance()->BoundByDuty56) // delay when bound by duty
        {
            _wasBoundByDuty = true;
            return;
        }

        if (Conditions.Instance()->BetweenAreas) // requeue when moving
        {
            _mismatchCheckCTS?.Cancel();
            _mismatchCheckCTS = new();

            _framework.RunOnTick(
                () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex),
                CheckDelay,
                cancellationToken: _mismatchCheckCTS.Token);

            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();

        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
            return;

        if (Config.IgnoreDoHDoL && (!_excelService.TryGetRow<ClassJob>(gearset->ClassJob, out var classJobRow) || classJobRow.DohDolJobIndex != -1))
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

        if (banner->Checksum == checksum)
        {
            _logger.LogInformation("Gear checksum matches! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);
            return;
        }

        _logger.LogInformation("Gear checksum mismatch detected! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);

        if (!isJobChange && Config.ReequipGearsetOnUpdate && gearset->GlamourSetLink > 0 && UIGlobals.CanApplyGlamourPlates())
        {
            _logger.LogInformation("Re-equipping Gearset #{gearsetId} to reapply glamour plate", gearset->Id + 1);
            raptureGearsetModule->EquipGearset(gearset->Id, gearset->GlamourSetLink);
            RecheckGearChecksum(banner);
        }
        else if (!isJobChange && Config.AutoUpdatePotraitOnGearUpdate && gearset->GlamourSetLink == 0)
        {
            _logger.LogInformation("Trying to send portrait update...");

            if (SendPortraitUpdate(banner))
            {
                RecheckGearChecksum(banner);
            }
            else
            {
                _agentBannerPreviewShowHook?.Original(AgentBannerPreview.Instance());
            }
        }
        else if (Config.NotifyGearChecksumMismatch)
        {
            NotifyMismatch();
        }
    }

    private void RecheckGearChecksum(BannerModuleEntry* banner)
    {
        _mismatchCheckCTS?.Cancel();
        _mismatchCheckCTS = new();

        _framework.RunOnTick(() =>
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

        var sb = new LSeStringBuilder()
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
