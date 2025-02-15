using System.Threading;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Interfaces;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Structs;
using HaselTweaks.Windows.PortraitHelperWindows;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class PortraitHelper(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ExcelService ExcelService,
    ILogger<PortraitHelper> Logger,
    IGameInteropProvider GameInteropProvider,
    IDalamudPluginInterface PluginInterface,
    ICondition Condition,
    IFramework Framework,
    IClientState ClientState,
    IChatGui ChatGui,
    AddonObserver AddonObserver,
    MenuBar MenuBar)
    : IConfigurableTweak
{
    public string InternalName => nameof(PortraitHelper);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(500);

    private CancellationTokenSource? MismatchCheckCTS;
    private DalamudLinkPayload? OpenPortraitEditPayload;

    public static ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;
    public static PortraitPreset? ClipboardPreset { get; set; }

    private Hook<UIClipboard.Delegates.OnClipboardDataChanged>? OnClipboardDataChangedHook;
    private Hook<RaptureGearsetModule.Delegates.UpdateGearset>? UpdateGearsetHook;
    private Hook<AgentBannerPreview.Delegates.Show>? AgentBannerPreviewShowHook;
    private bool WasBoundByDuty;
    private bool BlockBannerPreview;

    public void OnInitialize()
    {
        OnClipboardDataChangedHook = GameInteropProvider.HookFromAddress<UIClipboard.Delegates.OnClipboardDataChanged>(
            UIClipboard.MemberFunctionPointers.OnClipboardDataChanged,
            OnClipboardDataChangedDetour);

        UpdateGearsetHook = GameInteropProvider.HookFromAddress<RaptureGearsetModule.Delegates.UpdateGearset>(
            RaptureGearsetModule.MemberFunctionPointers.UpdateGearset,
            UpdateGearsetDetour);

        AgentBannerPreviewShowHook = GameInteropProvider.HookFromAddress<AgentBannerPreview.Delegates.Show>(
            AgentBannerPreview.Instance()->VirtualTable->Show,
            AgentBannerPreviewShowDetour);
    }

    public void OnEnable()
    {
        OpenPortraitEditPayload = PluginInterface.AddChatLinkHandler(1000, OpenPortraitEditChatHandler);

        if (IsAddonOpen(AgentId.BannerEditor))
            OnAddonOpen("BannerEditor");

        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;
        ClientState.TerritoryChanged += OnTerritoryChanged;
        ClientState.ClassJobChanged += OnClassJobChange;

        OnClipboardDataChangedHook?.Enable();
        UpdateGearsetHook?.Enable();
        AgentBannerPreviewShowHook?.Enable();
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        ClientState.ClassJobChanged -= OnClassJobChange;

        PluginInterface.RemoveChatLinkHandler(1000);

        MenuBar.Close();

        OnClipboardDataChangedHook?.Disable();
        UpdateGearsetHook?.Disable();
        AgentBannerPreviewShowHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        OnClipboardDataChangedHook?.Dispose();
        UpdateGearsetHook?.Dispose();
        AgentBannerPreviewShowHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OpenPortraitEditChatHandler(uint commandId, SeString message)
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

        MenuBar.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName != "BannerEditor")
            return;

        MenuBar.Close();
    }

    private void OnTerritoryChanged(ushort territoryTypeId)
    {
        if (WasBoundByDuty && !Condition[ConditionFlag.BoundByDuty56])
        {
            WasBoundByDuty = false;

            MismatchCheckCTS?.Cancel();
            MismatchCheckCTS = new();

            Framework.RunOnTick(
                () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex),
                CheckDelay,
                cancellationToken: MismatchCheckCTS.Token);
        }
    }

    private void OnClipboardDataChangedDetour(UIClipboard* uiClipboard)
    {
        OnClipboardDataChangedHook!.Original(uiClipboard);

        try
        {
            ClipboardPreset = PortraitPreset.FromExportedString(uiClipboard->Data.SystemClipboardText.ToString());
            if (ClipboardPreset != null)
                Logger.LogDebug("Parsed ClipboardPreset: {ClipboardPreset}", ClipboardPreset);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reading preset");
        }
    }

    private int UpdateGearsetDetour(RaptureGearsetModule* raptureGearsetModule, int gearsetId)
    {
        if (Config.AutoUpdatePotraitOnGearUpdate)
            BlockBannerPreview = true;

        var ret = UpdateGearsetHook!.Original(raptureGearsetModule, gearsetId);

        BlockBannerPreview = false;

        MismatchCheckCTS?.Cancel();
        MismatchCheckCTS = new();

        Framework.RunOnTick(
            () => CheckForGearChecksumMismatch(gearsetId),
            CheckDelay,
            cancellationToken: MismatchCheckCTS.Token);

        return ret;
    }

    private void AgentBannerPreviewShowDetour(AgentBannerPreview* thisPtr)
    {
        if (!Config.AutoUpdatePotraitOnGearUpdate)
        {
            AgentBannerPreviewShowHook!.Original(thisPtr);
            return;
        }

        if (BlockBannerPreview)
        {
            BlockBannerPreview = false;
            return;
        }

        AgentBannerPreviewShowHook!.Original(thisPtr);
    }

    private void OnClassJobChange(uint classJobId)
    {
        MismatchCheckCTS?.Cancel();
        MismatchCheckCTS = new();

        Framework.RunOnTick(
            () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex, true),
            CheckDelay,
            cancellationToken: MismatchCheckCTS.Token);
    }

    private void CheckForGearChecksumMismatch(int gearsetId, bool isJobChange = false)
    {
        if (Condition[ConditionFlag.BoundByDuty56]) // delay when bound by duty
        {
            WasBoundByDuty = true;
            return;
        }

        if (Condition[ConditionFlag.BetweenAreas]) // requeue when moving
        {
            MismatchCheckCTS?.Cancel();
            MismatchCheckCTS = new();

            Framework.RunOnTick(
                () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex),
                CheckDelay,
                cancellationToken: MismatchCheckCTS.Token);

            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();

        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
            return;

        if (Config.IgnoreDoHDoL && (!ExcelService.TryGetRow<ClassJob>(gearset->ClassJob, out var classJobRow) || classJobRow.DohDolJobIndex != -1))
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

        var checksum = HaselUIGlobals.GenerateEquippedItemsChecksum();

        if (banner->Checksum == checksum)
        {
            Logger.LogInformation("Gear checksum matches! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);
            return;
        }

        Logger.LogInformation("Gear checksum mismatch detected! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);

        if (!isJobChange && Config.ReequipGearsetOnUpdate && gearset->GlamourSetLink > 0 && UIGlobals.CanApplyGlamourPlates())
        {
            Logger.LogInformation("Re-equipping Gearset #{gearsetId} to reapply glamour plate", gearset->Id + 1);
            raptureGearsetModule->EquipGearset(gearset->Id, gearset->GlamourSetLink);
            RecheckGearChecksum(banner);
        }
        else if (!isJobChange && Config.AutoUpdatePotraitOnGearUpdate && gearset->GlamourSetLink == 0)
        {
            Logger.LogInformation("Trying to send portrait update...");

            if (SendPortraitUpdate(banner))
            {
                RecheckGearChecksum(banner);
            }
            else
            {
                AgentBannerPreviewShowHook?.Original(AgentBannerPreview.Instance());
            }
        }
        else if (Config.NotifyGearChecksumMismatch)
        {
            NotifyMismatch();
        }
    }

    private void RecheckGearChecksum(BannerModuleEntry* banner)
    {
        MismatchCheckCTS?.Cancel();
        MismatchCheckCTS = new();

        Framework.RunOnTick(() =>
        {
            var checksum = HaselUIGlobals.GenerateEquippedItemsChecksum();

            if (banner->Checksum != checksum)
            {
                Logger.LogInformation("Gear checksum still mismatching (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, checksum);
                NotifyMismatch();
            }
            else
            {
                Logger.LogInformation("Gear checksum matches now (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, checksum);
            }
        }, delay: CheckDelay, cancellationToken: MismatchCheckCTS.Token);
    }

    private void NotifyMismatch()
    {
        var text = TextService.Translate("PortraitHelper.GearChecksumMismatch"); // based on LogMessage#5876

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            if (OpenPortraitEditPayload != null)
            {
                sb.Add(OpenPortraitEditPayload)
                  .AddText(text)
                  .Add(RawPayload.LinkTerminator);
            }
            else
            {
                sb.AddText(text);
            }
        }
        else
        {
            sb.AddText(text);
        }

        UIModule.Instance()->ShowErrorText(text, false);

        ChatGui.PrintError(sb.Build());
    }

    private bool SendPortraitUpdate(BannerModuleEntry* banner)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;
        if (!raptureGearsetModule->IsValidGearset(gearsetId))
        {
            Logger.LogWarning("No Portrait Update: Gearset invalid");
            return false;
        }

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
        {
            Logger.LogWarning("No Portrait Update: Gearset is null");
            return false;
        }

        var bannerIndex = gearset->BannerIndex;
        if (bannerIndex == 0) // no banner linked
        {
            Logger.LogInformation("No Portrait Update: Gearset not linked to Banner");
            return false;
        }

        if (banner->BannerIndex != bannerIndex - 1)
        {
            Logger.LogWarning("No Portrait Update: Banner index mismatch (Banner: {bannerIndex}, Gearset Banner Link: {gearsetBannerIndex})", banner->BannerIndex, bannerIndex - 1);
            return false;
        }

        var currentChecksum = HaselUIGlobals.GenerateEquippedItemsChecksum();
        if (banner->Checksum == currentChecksum)
        {
            Logger.LogInformation("No Portrait Update: Checksum still matches");
            return false;
        }

        var localPlayer = (Character*)Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            Logger.LogWarning("No Portrait Update: LocalPlayer is null");
            return false;
        }

        var helper = UIModule.Instance()->GetUIModuleHelpers()->BannerHelper;
        if (!helper->BannerModuleEntry_IsCurrentCharaCardBannerOutdated(banner, true))
        {
            Logger.LogWarning("No Portrait Update: Banner expired");
            return false;
        }

        if (!helper->BannerModuleEntry_IsCharacterDataOutdated(banner, true))
        {
            Logger.LogWarning("No Portrait Update: Banner character data expired");
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
            Logger.LogInformation("Portrait Update sent");
        }
        else
        {
            Logger.LogWarning("Portrait Update failed to send");
        }

        return result;
    }
}
