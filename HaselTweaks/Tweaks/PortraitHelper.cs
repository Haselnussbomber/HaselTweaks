using System.Threading;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
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
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public unsafe partial class PortraitHelper(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ILogger<PortraitHelper> Logger,
    IGameInteropProvider GameInteropProvider,
    DalamudPluginInterface PluginInterface,
    IFramework Framework,
    IClientState ClientState,
    IChatGui ChatGui,
    AddonObserver AddonObserver,
    MenuBar MenuBar)
    : IConfigurableTweak
{
    public string InternalName => nameof(PortraitHelper);
    public TweakStatus Status { get; set; } = TweakStatus.Outdated;

    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(500);

    private CancellationTokenSource? MismatchCheckCTS;
    private DalamudLinkPayload? _openPortraitEditPayload;

    public static ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;
    public static PortraitPreset? ClipboardPreset { get; set; }

    private Hook<UIClipboard.Delegates.OnClipboardDataChanged>? OnClipboardDataChangedHook;
    private Hook<RaptureGearsetModule.Delegates.UpdateGearset>? UpdateGearsetHook;
    private Hook<UIModule.Delegates.HandlePacket>? HandleUIModulePacketHook;

    public void OnInitialize()
    {
        OnClipboardDataChangedHook = GameInteropProvider.HookFromAddress<UIClipboard.Delegates.OnClipboardDataChanged>(
            UIClipboard.MemberFunctionPointers.OnClipboardDataChanged,
            OnClipboardDataChangedDetour);

        UpdateGearsetHook = GameInteropProvider.HookFromAddress<RaptureGearsetModule.Delegates.UpdateGearset>(
            RaptureGearsetModule.MemberFunctionPointers.UpdateGearset,
            UpdateGearsetDetour);

        HandleUIModulePacketHook = GameInteropProvider.HookFromAddress<UIModule.Delegates.HandlePacket>(
            UIModule.StaticVirtualTablePointer->HandlePacket,
            HandleUIModulePacketDetour);
    }

    public void OnEnable()
    {
        _openPortraitEditPayload = PluginInterface.AddChatLinkHandler(1000, OpenPortraitEditChatHandler);

        if (IsAddonOpen(AgentId.BannerEditor))
            OnAddonOpen("BannerEditor");

        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        OnClipboardDataChangedHook?.Enable();
        UpdateGearsetHook?.Enable();
        HandleUIModulePacketHook?.Enable();
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        PluginInterface.RemoveChatLinkHandler(1000);

        MenuBar.Close();

        OnClipboardDataChangedHook?.Disable();
        UpdateGearsetHook?.Disable();
        HandleUIModulePacketHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        OnClipboardDataChangedHook?.Dispose();
        UpdateGearsetHook?.Dispose();
        HandleUIModulePacketHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OpenPortraitEditChatHandler(uint commandId, SeString message)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();
        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;
        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        AgentBannerEditor.Instance()->OpenForGearset(gearsetId);
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

    private void UpdateGearsetDetour(RaptureGearsetModule* raptureGearsetModule, int gearsetId)
    {
        UpdateGearsetHook!.Original(raptureGearsetModule, gearsetId);

        MismatchCheckCTS?.Cancel();
        MismatchCheckCTS = new();

        Framework.RunOnTick(
            () => CheckForGearChecksumMismatch(gearsetId),
            CheckDelay,
            cancellationToken: MismatchCheckCTS.Token);
    }

    private void HandleUIModulePacketDetour(UIModule* uiModule, UIModulePacketType type, uint uintParam, void* packet)
    {
        HandleUIModulePacketHook!.Original(uiModule, type, uintParam, packet);

        if (type != UIModulePacketType.ClassJobChange || !ClientState.IsLoggedIn)
            return;

        MismatchCheckCTS?.Cancel();
        MismatchCheckCTS = new();

        Framework.RunOnTick(
            () => CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex, true),
            CheckDelay,
            cancellationToken: MismatchCheckCTS.Token);
    }

    private void CheckForGearChecksumMismatch(int gearsetId, bool isJobChange = false)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        if (!raptureGearsetModule->IsValidGearset(gearsetId))
            return;

        var gearset = raptureGearsetModule->GetGearset(gearsetId);
        if (gearset == null)
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

        var checksum = GetEquippedGearChecksum();

        if (banner->Checksum == checksum)
        {
            Logger.LogInformation("Gear checksum matches! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);
            return;
        }

        Logger.LogInformation("Gear checksum mismatch detected! (Portrait: {bannerChecksum:X}, Equipped: {checksum:X})", banner->Checksum, checksum);

        if (!isJobChange && Config.ReequipGearsetOnUpdate && gearset->GlamourSetLink > 0 && GameMain.IsInSanctuary())
        {
            Logger.LogInformation("Re-equipping Gearset #{gearsetId} to reapply glamour plate", gearset->Id + 1);
            raptureGearsetModule->EquipGearset(gearset->Id, gearset->GlamourSetLink);
            RecheckGearChecksum(banner);
        }
        else if (!isJobChange && Config.AutoUpdatePotraitOnGearUpdate && gearset->GlamourSetLink == 0)
        {
            Logger.LogInformation("Trying to send portrait update...");
            if (SendPortraitUpdate(banner))
                RecheckGearChecksum(banner);
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
            if (banner->Checksum != GetEquippedGearChecksum())
            {
                Logger.LogInformation("Gear checksum still mismatching (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, GetEquippedGearChecksum());
                NotifyMismatch();
            }
            else
            {
                Logger.LogInformation("Gear checksum matches now (Portrait: {bannerChecksum:X}, Equipped: {equippedChecksum:X})", banner->Checksum, GetEquippedGearChecksum());
            }
        }, delay: CheckDelay, cancellationToken: MismatchCheckCTS.Token); // TODO: find out when it's safe to check again instead of randomly picking a delay. ping may vary
    }

    private void NotifyMismatch()
    {
        var text = TextService.Translate("PortraitHelper.GearChecksumMismatch"); // based on LogMessage#5876

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            if (_openPortraitEditPayload != null)
            {
                sb.Add(_openPortraitEditPayload)
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

    private uint GetEquippedGearChecksum()
    {
        var localPlayer = (Character*)Control.GetLocalPlayer();
        if (localPlayer == null)
            return 0;

        // not the real struct... just enough to be able to call LoadEquipmentData
        var data = new FakeAgentBannerListData
        {
            UIModule = UIModule.Instance()
        };

        var itemIds = stackalloc uint[14];
        var stainIds = stackalloc byte[14 * 2];

        if (!data.LoadEquipmentData(itemIds, stainIds))
            return 0;

        var gearVisibilityFlag = BannerGearVisibilityFlag.None;

        if (localPlayer->DrawData.IsHatHidden)
            gearVisibilityFlag |= BannerGearVisibilityFlag.HeadgearHidden;

        if (localPlayer->DrawData.IsWeaponHidden)
            gearVisibilityFlag |= BannerGearVisibilityFlag.WeaponHidden;

        if (localPlayer->DrawData.IsVisorToggled)
            gearVisibilityFlag |= BannerGearVisibilityFlag.VisorClosed;

        return BannerModuleEntry.GenerateChecksum(itemIds, stainIds, gearVisibilityFlag);
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
            Logger.LogWarning($"No Portrait Update: Banner index mismatch (Banner: {banner->BannerIndex}, Gearset Banner Link: {bannerIndex - 1})");
            return false;
        }

        var currentChecksum = GetEquippedGearChecksum();
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

        var helper = Structs.UIModuleHelpers.Instance()->BannerModuleHelper;

        // TODO: check E8 ?? ?? ?? ?? 84 C0 74 4A 48 8D 4C 24

        if (!helper->IsBannerNotExpired(banner, 1))
        {
            Logger.LogWarning("No Portrait Update: Banner expired");
            return false;
        }

        if (!helper->IsBannerCharacterDataNotExpired(banner, 1))
        {
            Logger.LogWarning("No Portrait Update: Banner character data expired");
            return false;
        }

        var bannerUpdateData = new BannerUpdateData();

        if (!helper->InitializeBannerUpdateData(&bannerUpdateData))
        {
            Logger.LogWarning("No Portrait Update: InitializeBannerUpdateData failed");
            return false;
        }

        // update Banner
        banner->LastUpdated = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        banner->Checksum = currentChecksum;
        helper->CopyRaceGenderHeightTribe(banner, localPlayer);
        BannerModule.Instance()->UserFileEvent.HasChanges = true;

        if (!helper->CopyBannerEntryToBannerUpdateData(&bannerUpdateData, banner))
        {
            Logger.LogWarning("No Portrait Update: CopyBannerEntryToBannerUpdateData failed");
            return false;
        }

        var result = helper->SendBannerUpdateData(&bannerUpdateData);

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
