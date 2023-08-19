using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Extensions;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
using static HaselTweaks.Structs.AgentBannerEditorState;
using DalamudFramework = Dalamud.Game.Framework;

namespace HaselTweaks.Tweaks;

[Tweak]
public partial class PortraitHelper : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    public class Configuration
    {
        public List<SavedPreset> Presets = new();
        public List<SavedPresetTag> PresetTags = new();
        public bool ShowAlignmentTool = false;
        public int AlignmentToolVerticalLines = 2;
        public Vector4 AlignmentToolVerticalColor = new(0, 0, 0, 1f);
        public int AlignmentToolHorizontalLines = 2;
        public Vector4 AlignmentToolHorizontalColor = new(0, 0, 0, 1f);

        [BoolConfig]
        public bool NotifyGearChecksumMismatch = true;

        [BoolConfig(DependsOn = nameof(NotifyGearChecksumMismatch))]
        public bool ReequipGearsetOnUpdate = false;

        [BoolConfig(DependsOn = nameof(NotifyGearChecksumMismatch))]
        public bool AutoSavePotraitOnGearUpdate = false;

        public static string GetPortraitThumbnailPath(string hash)
        {
            var portraitsPath = Path.Join(Service.PluginInterface.ConfigDirectory.FullName, "Portraits");

            if (!Directory.Exists(portraitsPath))
                Directory.CreateDirectory(portraitsPath);

            return Path.Join(portraitsPath, $"{hash}.png");
        }
    }

    public unsafe AgentBannerEditor* AgentBannerEditor;
    public unsafe AgentBannerEditorState* AgentBannerEditorState;
    public unsafe AgentStatus* AgentStatus;
    public unsafe AddonBannerEditor* AddonBannerEditor;

    public MenuBar? MenuBar { get; private set; }
    public AdvancedImportOverlay? AdvancedImportOverlay { get; set; }
    public AdvancedEditOverlay? AdvancedEditOverlay { get; set; }
    public PresetBrowserOverlay? PresetBrowserOverlay { get; set; }
    public AlignmentToolSettingsOverlay? AlignmentToolSettingsOverlay { get; set; }

    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(100);

    private DateTime _lastClipboardCheck = default;
    private uint _lastClipboardSequenceNumber;
    private CancellationTokenSource? _jobChangedOrGearsetUpdatedCTS;
    private uint _lastJob = 0;
    private DalamudLinkPayload? _openPortraitEditPayload;

    public ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;

    public PortraitPreset? ClipboardPreset { get; private set; }

    public override unsafe void Enable()
    {
        _lastJob = Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0;

        _openPortraitEditPayload = Service.PluginInterface.AddChatLinkHandler(1000, OpenPortraitEditChatHandler);

        AgentBannerEditor = GetAgent<AgentBannerEditor>();
        AgentStatus = GetAgent<AgentStatus>();

        if (IsAddonOpen(AgentId.BannerEditor))
            OnAddonOpen("BannerEditor");
    }

    public override void Disable()
    {
        Service.PluginInterface.RemoveChatLinkHandler(1000);

        if (MenuBar != null)
            MenuBar.IsOpen = false;

        CloseWindows();
    }

    public override void Dispose()
    {
        if (MenuBar != null && Plugin.WindowSystem.Windows.Contains(MenuBar))
        {
            Plugin.WindowSystem.RemoveWindow(MenuBar);
            MenuBar = null;
        }

        if (AdvancedImportOverlay != null && Plugin.WindowSystem.Windows.Contains(AdvancedImportOverlay))
        {
            Plugin.WindowSystem.RemoveWindow(AdvancedImportOverlay);
            AdvancedImportOverlay = null;
        }

        if (AdvancedEditOverlay != null && Plugin.WindowSystem.Windows.Contains(AdvancedEditOverlay))
        {
            Plugin.WindowSystem.RemoveWindow(AdvancedEditOverlay);
            AdvancedEditOverlay = null;
        }

        if (PresetBrowserOverlay != null && Plugin.WindowSystem.Windows.Contains(PresetBrowserOverlay))
        {
            Plugin.WindowSystem.RemoveWindow(PresetBrowserOverlay);
            PresetBrowserOverlay.Dispose();
            PresetBrowserOverlay = null;
        }

        if (AlignmentToolSettingsOverlay != null && Plugin.WindowSystem.Windows.Contains(AlignmentToolSettingsOverlay))
        {
            Plugin.WindowSystem.RemoveWindow(AlignmentToolSettingsOverlay);
            AlignmentToolSettingsOverlay = null;
        }
    }

    public override void OnLogin()
    {
        _lastJob = Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0;
    }

    public override void OnLogout()
    {
        _lastJob = 0;
    }

    private unsafe int? GetCurrentGearsetId()
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();
        var gearsetId = raptureGearsetModule->CurrentGearsetIndex;
        return raptureGearsetModule->IsValidGearset(gearsetId) == 0 ? null : gearsetId;
    }

    private unsafe void SavePortrait(BannerModuleEntry* banner)
    {
        var gearsetId = GetCurrentGearsetId();
        if (gearsetId != null)
        {
            AgentBannerEditor->OpenForGearset((int)gearsetId);

            // We need to wait until the character potrait is actually loaded before we can call save
            Task.Run(() =>
            {
                Log("Attempting to save portrait in task");
                var maxTries = 5;
                var loopCount = 0;
                while(!GetAgent<AgentBannerEditor>()->EditorState->CharaView->CharacterLoaded)
                {
                    Thread.Sleep(100);
                    if (loopCount > maxTries)
                    {
                        Log(new Exception(), $"Could not open portrait window after {maxTries} attempts");
                        break;
                    }
                }

                GetAgent<AgentBannerEditor>()->EditorState->Save();
                RecheckGearChecksumOrOpenPortraitEditor(banner);

                // one final check to determine if we can mark the save button as grayed out
                if (banner->GearChecksum == GetEquippedGearChecksum())
                {
                    GetAgent<AgentBannerEditor>()->EditorState->SetHasChanged(false);

                    // Tell the user what we did
                    var rapture = RaptureGearsetModule.Instance();
                    var gearsetID = rapture->CurrentGearsetIndex;
                    var gearset = rapture->GetGearset(gearsetID);
                    var gearsetName = System.Text.Encoding.ASCII.GetString(gearset->Name, 0x2F);

                    var text = $"Portrait has been saved and updated for \"{gearsetName}\"";
                    UIModule.Instance()->ShowText(0, text);
                    Service.ChatGui.Print($"Portrait has been saved and updated for \"{gearsetName}\"");
                }
            });
        }
    }

    private unsafe void OpenPortraitEditChatHandler(uint commandId, SeString message)
    {
        var gearsetId = GetCurrentGearsetId();
        if (gearsetId != null)
            AgentBannerEditor->OpenForGearset((int)gearsetId);
    }

    public override unsafe void OnAddonOpen(string addonName)
    {
        if (addonName != "BannerEditor")
            return;

        if (!TryGetAddon(addonName, out AddonBannerEditor))
            return;

        if (MenuBar == null)
            Plugin.WindowSystem.AddWindow(MenuBar = new(this));

        MenuBar.IsOpen = true;
    }

    public override unsafe void OnAddonClose(string addonName)
    {
        if (addonName != "BannerEditor")
            return;

        AddonBannerEditor = null;

        if (MenuBar != null)
            MenuBar.IsOpen = false;

        CloseWindows();
    }

    public void CloseWindows()
    {
        if (AdvancedImportOverlay != null)
            AdvancedImportOverlay.IsOpen = false;

        if (AdvancedEditOverlay != null)
            AdvancedEditOverlay.IsOpen = false;

        if (PresetBrowserOverlay != null)
            PresetBrowserOverlay.IsOpen = false;

        if (AlignmentToolSettingsOverlay != null)
            AlignmentToolSettingsOverlay.IsOpen = false;
    }

    public override unsafe void OnFrameworkUpdate(DalamudFramework framework)
    {
        var currentJob = Service.ClientState.LocalPlayer?.ClassJob.Id ?? 0;
        if (currentJob != 0 && currentJob != _lastJob)
        {
            _jobChangedOrGearsetUpdatedCTS?.Cancel();
            _jobChangedOrGearsetUpdatedCTS = new();

            _lastJob = currentJob;

            if (Config.NotifyGearChecksumMismatch)
            {
                Service.Framework.RunOnTick(() =>
                {
                    CheckForGearChecksumMismatch(RaptureGearsetModule.Instance()->CurrentGearsetIndex, true, true);
                }, CheckDelay, cancellationToken: _jobChangedOrGearsetUpdatedCTS.Token);
            }
        }

        if (MenuBar != null && !MenuBar.IsOpen)
            return;

        CheckClipboard();
    }

    public void CheckClipboard()
    {
        if (DateTime.Now - _lastClipboardCheck <= TimeSpan.FromMilliseconds(100))
            return;

        if (!PInvoke.IsClipboardFormatAvailable((uint)CLIPBOARD_FORMAT.CF_TEXT))
            return;

        var clipboardSequenceNumber = PInvoke.GetClipboardSequenceNumber();

        if (_lastClipboardSequenceNumber == clipboardSequenceNumber)
            return;

        if (!PInvoke.OpenClipboard(HWND.Null))
            return;

        try
        {
            _lastClipboardSequenceNumber = clipboardSequenceNumber;

            var data = PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_TEXT);
            if (!data.IsNull)
            {
                var clipboardText = MemoryHelper.ReadString(data, 1024);
                ClipboardPreset = PortraitPreset.FromExportedString(clipboardText);

                if (ClipboardPreset != null)
                    Debug($"Parsed ClipboardPreset: {ClipboardPreset}");
            }
        }
        catch (Exception e)
        {
            Error(e, "Error during CheckClipboard");
        }
        finally
        {
            PInvoke.CloseClipboard();

            _lastClipboardCheck = DateTime.Now;
        }
    }

    public async void PresetToClipboard(PortraitPreset? preset)
    {
        if (preset == null)
            return;

        await ClipboardUtils.OpenClipboard();

        try
        {
            PInvoke.EmptyClipboard();

            var clipboardText = Marshal.StringToHGlobalAnsi(preset.ToExportedString());
            if (PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_TEXT, (HANDLE)clipboardText) != 0)
                ClipboardPreset = preset;
        }
        catch (Exception e)
        {
            Error(e, "Error during PresetToClipboard");
        }
        finally
        {
            PInvoke.CloseClipboard();
        }
    }

    public unsafe Image<Bgra32>? GetCurrentCharaViewImage()
    {
        var agentBannerEditor = GetAgent<AgentBannerEditor>();
        var charaViewTexture = RenderTargetManager.Instance()->GetCharaViewTexture(agentBannerEditor->EditorState->CharaView->Base.ClientObjectIndex);
        if (charaViewTexture == null || charaViewTexture->D3D11Texture2D == null)
            return null;

        var device = Service.PluginInterface.UiBuilder.Device;
        var texture = CppObject.FromPointer<Texture2D>((nint)charaViewTexture->D3D11Texture2D);

        // thanks to ChatGPT
        // Get the texture description
        var desc = texture.Description;

        // Create a staging texture with the same description
        using var stagingTexture = new Texture2D(device, new Texture2DDescription()
        {
            ArraySize = 1,
            BindFlags = BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.Read,
            Format = desc.Format,
            Height = desc.Height,
            Width = desc.Width,
            MipLevels = 1,
            OptionFlags = desc.OptionFlags,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        });

        // Copy the texture data to the staging texture
        device.ImmediateContext.CopyResource(texture, stagingTexture);

        // Map the staging texture
        device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var dataStream);

        using var pixelDataStream = new MemoryStream();
        dataStream.CopyTo(pixelDataStream);

        // Unmap the staging texture
        device.ImmediateContext.UnmapSubresource(stagingTexture, 0);

        return Image.LoadPixelData<Bgra32>(pixelDataStream.ToArray(), desc.Width, desc.Height);
    }

    public unsafe PortraitPreset? StateToPreset()
    {
        var state = GetAgent<AgentBannerEditor>()->EditorState;
        var preset = new PortraitPreset();

        using var portraitData = new DisposableStruct<ExportedPortraitData>();
        state->CharaView->ExportPortraitData(portraitData);
        preset.ReadExportedPortraitData(portraitData);

        preset.BannerFrame = state->BannerEntry.BannerFrame;
        preset.BannerDecoration = state->BannerEntry.BannerDecoration;

        return preset;
    }

    public unsafe void PresetToState(PortraitPreset? preset, ImportFlags importFlags)
    {
        if (preset == null)
            return;

        if (!TryGetAddon<AddonBannerEditor>(AgentId.BannerEditor, out var addonBannerEditor))
            return;

        Debug($"Importing Preset {preset.ToExportedString()} with ImportFlags {importFlags}");

        var state = GetAgent<AgentBannerEditor>()->EditorState;
        var bannerEntry = state->BannerEntry;

        // read current portrait and then overwrite what the flags allow below
        using var tempPortraitDataHolder = new DisposableStruct<ExportedPortraitData>();
        var tempPortraitData = tempPortraitDataHolder.Ptr;

        state->CharaView->ExportPortraitData(tempPortraitData);

        var hasBgChanged =
            importFlags.HasFlag(ImportFlags.BannerBg) &&
            tempPortraitData->BannerBg != preset.BannerBg;

        var hasFrameChanged =
            importFlags.HasFlag(ImportFlags.BannerFrame) &&
            bannerEntry.BannerFrame != preset.BannerFrame;

        var hasDecorationChanged =
            importFlags.HasFlag(ImportFlags.BannerDecoration) &&
            bannerEntry.BannerDecoration != preset.BannerDecoration;

        var hasBannerTimelineChanged =
            importFlags.HasFlag(ImportFlags.BannerTimeline) &&
            tempPortraitData->BannerTimeline != preset.BannerTimeline;

        var hasExpressionChanged =
            importFlags.HasFlag(ImportFlags.Expression) &&
            tempPortraitData->Expression != preset.Expression;

        var hasAmbientLightingBrightnessChanged =
            importFlags.HasFlag(ImportFlags.AmbientLightingBrightness) &&
            tempPortraitData->AmbientLightingBrightness != preset.AmbientLightingBrightness;

        var hasAmbientLightingColorChanged =
            importFlags.HasFlag(ImportFlags.AmbientLightingColor) && (
                tempPortraitData->AmbientLightingColorRed != preset.AmbientLightingColorRed ||
                tempPortraitData->AmbientLightingColorGreen != preset.AmbientLightingColorGreen ||
                tempPortraitData->AmbientLightingColorBlue != preset.AmbientLightingColorBlue
            );

        var hasDirectionalLightingBrightnessChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingBrightness) &&
            tempPortraitData->DirectionalLightingBrightness != preset.DirectionalLightingBrightness;

        var hasDirectionalLightingColorChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingColor) && (
                tempPortraitData->DirectionalLightingColorRed != preset.DirectionalLightingColorRed ||
                tempPortraitData->DirectionalLightingColorGreen != preset.DirectionalLightingColorGreen ||
                tempPortraitData->DirectionalLightingColorBlue != preset.DirectionalLightingColorBlue
            );

        var hasDirectionalLightingVerticalAngleChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingVerticalAngle) &&
            tempPortraitData->DirectionalLightingVerticalAngle != preset.DirectionalLightingVerticalAngle;

        var hasDirectionalLightingHorizontalAngleChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingHorizontalAngle) &&
            tempPortraitData->DirectionalLightingHorizontalAngle != preset.DirectionalLightingHorizontalAngle;

        var hasAnimationProgressChanged =
            importFlags.HasFlag(ImportFlags.AnimationProgress) &&
            !tempPortraitData->AnimationProgress.IsApproximately(preset.AnimationProgress, 0.01f);

        var hasCameraPositionChanged =
            importFlags.HasFlag(ImportFlags.CameraPosition) &&
            !tempPortraitData->CameraPosition.IsApproximately(preset.CameraPosition);

        var hasCameraTargetChanged =
            importFlags.HasFlag(ImportFlags.CameraTarget) &&
            !tempPortraitData->CameraTarget.IsApproximately(preset.CameraTarget);

        var hasHeadDirectionChanged =
            importFlags.HasFlag(ImportFlags.HeadDirection) &&
            !tempPortraitData->HeadDirection.IsApproximately(preset.HeadDirection);

        var hasEyeDirectionChanged =
            importFlags.HasFlag(ImportFlags.EyeDirection) &&
            !tempPortraitData->EyeDirection.IsApproximately(preset.EyeDirection);

        var hasCameraZoomChanged =
            importFlags.HasFlag(ImportFlags.CameraZoom) &&
            tempPortraitData->CameraZoom != preset.CameraZoom;

        var hasImageRotationChanged =
            importFlags.HasFlag(ImportFlags.ImageRotation) &&
            tempPortraitData->ImageRotation != preset.ImageRotation;

        if (hasBgChanged)
        {
            Debug($"- BannerBg changed from {tempPortraitData->BannerBg} to {preset.BannerBg}");

            bannerEntry.BannerBg = preset.BannerBg;
            tempPortraitData->BannerBg = preset.BannerBg;

            addonBannerEditor->BackgroundDropdown->SetValue(GetListIndex(state->BackgroundItems, state->BackgroundItemsCount, preset.BannerBg));
        }

        if (hasFrameChanged)
        {
            Debug($"- BannerFrame changed from {bannerEntry.BannerFrame} to {preset.BannerFrame}");

            state->SetFrame(preset.BannerFrame);

            addonBannerEditor->FrameDropdown->SetValue(GetListIndex(state->FrameItems, state->FrameItemsCount, preset.BannerFrame));
        }

        if (hasDecorationChanged)
        {
            Debug($"- BannerDecoration changed from {bannerEntry.BannerDecoration} to {preset.BannerDecoration}");

            state->SetAccent(preset.BannerDecoration);

            addonBannerEditor->AccentDropdown->SetValue(GetListIndex(state->AccentItems, state->AccentItemsCount, preset.BannerDecoration));
        }

        if (hasBgChanged || hasFrameChanged || hasDecorationChanged)
        {
            Debug("- Preset changed");

            var presetIndex = state->GetPresetIndex(bannerEntry.BannerBg, bannerEntry.BannerFrame, bannerEntry.BannerDecoration);
            if (presetIndex < 0)
            {
                presetIndex = addonBannerEditor->NumPresets - 1;

                addonBannerEditor->PresetDropdown->List->SetListLength(addonBannerEditor->NumPresets); // increase to maximum, so "Custom" is displayed
            }

            addonBannerEditor->PresetDropdown->SetValue(presetIndex);
        }

        if (hasBannerTimelineChanged)
        {
            Debug($"- BannerTimeline changed from {tempPortraitData->BannerTimeline} to {preset.BannerTimeline}");

            bannerEntry.BannerTimeline = preset.BannerTimeline;
            tempPortraitData->BannerTimeline = preset.BannerTimeline;

            addonBannerEditor->PoseDropdown->SetValue(GetListIndex(state->BannerTimelineItems, state->BannerTimelineItemsCount, preset.BannerTimeline));
        }

        if (hasExpressionChanged)
        {
            Debug($"- Expression changed from {tempPortraitData->Expression} to {preset.Expression}");

            bannerEntry.Expression = preset.Expression;
            tempPortraitData->Expression = preset.Expression;

            addonBannerEditor->ExpressionDropdown->SetValue(GetExpressionListIndex(state->ExpressionItems, state->ExpressionItemsCount, preset.Expression));
        }

        if (hasAmbientLightingBrightnessChanged)
        {
            Debug($"- AmbientLightingBrightness changed from {tempPortraitData->AmbientLightingBrightness} to {preset.AmbientLightingBrightness}");

            tempPortraitData->AmbientLightingBrightness = preset.AmbientLightingBrightness;

            addonBannerEditor->AmbientLightingBrightnessSlider->SetValue(preset.AmbientLightingBrightness);
        }

        if (hasAmbientLightingColorChanged)
        {
            Debug($"- AmbientLightingColor changed from {tempPortraitData->AmbientLightingColorRed}, {tempPortraitData->AmbientLightingColorGreen}, {tempPortraitData->AmbientLightingColorBlue} to {preset.AmbientLightingColorRed}, {preset.AmbientLightingColorGreen}, {preset.AmbientLightingColorBlue}");

            tempPortraitData->AmbientLightingColorRed = preset.AmbientLightingColorRed;
            tempPortraitData->AmbientLightingColorGreen = preset.AmbientLightingColorGreen;
            tempPortraitData->AmbientLightingColorBlue = preset.AmbientLightingColorBlue;

            addonBannerEditor->AmbientLightingColorRedSlider->SetValue(preset.AmbientLightingColorRed);
            addonBannerEditor->AmbientLightingColorGreenSlider->SetValue(preset.AmbientLightingColorGreen);
            addonBannerEditor->AmbientLightingColorBlueSlider->SetValue(preset.AmbientLightingColorBlue);
        }

        if (hasDirectionalLightingBrightnessChanged)
        {
            Debug($"- DirectionalLightingBrightness changed from {tempPortraitData->DirectionalLightingBrightness} to {preset.DirectionalLightingBrightness}");

            tempPortraitData->DirectionalLightingBrightness = preset.DirectionalLightingBrightness;

            addonBannerEditor->DirectionalLightingBrightnessSlider->SetValue(preset.DirectionalLightingBrightness);
        }

        if (hasDirectionalLightingColorChanged)
        {
            Debug($"- DirectionalLightingColor changed from {tempPortraitData->DirectionalLightingColorRed}, {tempPortraitData->DirectionalLightingColorGreen}, {tempPortraitData->DirectionalLightingColorBlue} to {preset.DirectionalLightingColorRed}, {preset.DirectionalLightingColorGreen}, {preset.DirectionalLightingColorBlue}");

            tempPortraitData->DirectionalLightingColorRed = preset.DirectionalLightingColorRed;
            tempPortraitData->DirectionalLightingColorGreen = preset.DirectionalLightingColorGreen;
            tempPortraitData->DirectionalLightingColorBlue = preset.DirectionalLightingColorBlue;

            addonBannerEditor->DirectionalLightingColorRedSlider->SetValue(preset.DirectionalLightingColorRed);
            addonBannerEditor->DirectionalLightingColorGreenSlider->SetValue(preset.DirectionalLightingColorGreen);
            addonBannerEditor->DirectionalLightingColorBlueSlider->SetValue(preset.DirectionalLightingColorBlue);
        }

        if (hasDirectionalLightingVerticalAngleChanged)
        {
            Debug($"- DirectionalLightingVerticalAngle changed from {tempPortraitData->DirectionalLightingVerticalAngle} to {preset.DirectionalLightingVerticalAngle}");

            tempPortraitData->DirectionalLightingVerticalAngle = preset.DirectionalLightingVerticalAngle;

            addonBannerEditor->DirectionalLightingVerticalAngleSlider->SetValue(preset.DirectionalLightingVerticalAngle);
        }

        if (hasDirectionalLightingHorizontalAngleChanged)
        {
            Debug($"- DirectionalLightingHorizontalAngle changed from {tempPortraitData->DirectionalLightingHorizontalAngle} to {preset.DirectionalLightingHorizontalAngle}");

            tempPortraitData->DirectionalLightingHorizontalAngle = preset.DirectionalLightingHorizontalAngle;

            addonBannerEditor->DirectionalLightingHorizontalAngleSlider->SetValue(preset.DirectionalLightingHorizontalAngle);
        }

        if (hasAnimationProgressChanged)
        {
            Debug($"- AnimationProgress changed from {tempPortraitData->AnimationProgress} to {preset.AnimationProgress}");

            tempPortraitData->AnimationProgress = preset.AnimationProgress;
        }

        if (hasCameraPositionChanged)
        {
            Debug($"- CameraPosition changed from {tempPortraitData->CameraPosition.X}, {tempPortraitData->CameraPosition.Y}, {tempPortraitData->CameraPosition.Z}, {tempPortraitData->CameraPosition.W} to {preset.CameraPosition.X}, {preset.CameraPosition.Y}, {preset.CameraPosition.Z}, {preset.CameraPosition.W}");

            tempPortraitData->CameraPosition = preset.CameraPosition;
        }

        if (hasCameraTargetChanged)
        {
            Debug($"- CameraTarget changed from {tempPortraitData->CameraTarget.X}, {tempPortraitData->CameraTarget.Y}, {tempPortraitData->CameraTarget.Z}, {tempPortraitData->CameraTarget.W} to {preset.CameraTarget.X}, {preset.CameraTarget.Y}, {preset.CameraTarget.Z}, {preset.CameraTarget.W}");

            tempPortraitData->CameraTarget = preset.CameraTarget;
        }

        if (hasHeadDirectionChanged)
        {
            Debug($"- HeadDirection changed from {tempPortraitData->HeadDirection.X}, {tempPortraitData->HeadDirection.Y} to {preset.HeadDirection.X}, {preset.HeadDirection.Y}");

            tempPortraitData->HeadDirection = preset.HeadDirection;
        }

        if (hasEyeDirectionChanged)
        {
            Debug($"- EyeDirection changed from {tempPortraitData->EyeDirection.X}, {tempPortraitData->EyeDirection.Y} to {preset.EyeDirection.X}, {preset.EyeDirection.Y}");

            tempPortraitData->EyeDirection = preset.EyeDirection;
        }

        if (hasCameraZoomChanged)
        {
            Debug($"- CameraZoom changed from {tempPortraitData->CameraZoom} to {preset.CameraZoom}");

            tempPortraitData->CameraZoom = preset.CameraZoom;

            addonBannerEditor->CameraZoomSlider->SetValue(preset.CameraZoom);
        }

        if (hasImageRotationChanged)
        {
            Debug($"- ImageRotation changed from {tempPortraitData->ImageRotation} to {preset.ImageRotation}");

            tempPortraitData->ImageRotation = preset.ImageRotation;

            addonBannerEditor->ImageRotation->SetValue(preset.ImageRotation);
        }

        state->CharaView->ImportPortraitData(tempPortraitData);

        addonBannerEditor->PlayAnimationCheckbox->SetValue(false);
        addonBannerEditor->HeadFacingCameraCheckbox->SetValue(false);
        addonBannerEditor->EyesFacingCameraCheckbox->SetValue(false);

        state->SetHasChanged(
            state->HasDataChanged ||
            hasBgChanged ||
            hasFrameChanged ||
            hasDecorationChanged ||
            hasBannerTimelineChanged ||
            hasExpressionChanged ||
            hasAmbientLightingBrightnessChanged ||
            hasAmbientLightingColorChanged ||
            hasDirectionalLightingBrightnessChanged ||
            hasDirectionalLightingColorChanged ||
            hasDirectionalLightingVerticalAngleChanged ||
            hasDirectionalLightingHorizontalAngleChanged ||
            hasAnimationProgressChanged ||
            hasCameraPositionChanged ||
            hasCameraTargetChanged ||
            hasHeadDirectionChanged ||
            hasEyeDirectionChanged ||
            hasCameraZoomChanged ||
            hasImageRotationChanged
        );

        Debug("Import complete");
    }

    private static unsafe int GetListIndex(GenericDropdownItem** items, uint itemCount, ushort id)
    {
        for (var i = 0; i < itemCount; i++)
        {
            var entry = items[i];
            if (entry->Id == id && entry->Data != 0)
            {
                return i;
            }
        }

        return 0;
    }

    private static unsafe int GetExpressionListIndex(ExpressionDropdownItem** items, uint itemCount, ushort id)
    {
        for (var i = 0; i < itemCount; i++)
        {
            var entry = items[i];
            if (entry->Id == id && entry->Data != 0)
            {
                return i;
            }
        }

        return 0;
    }

    [AddressHook<RaptureGearsetModule>(nameof(RaptureGearsetModule.Addresses.UpdateGearset))]
    public unsafe int RaptureGearsetModule_UpdateGearset(RaptureGearsetModule* raptureGearsetModule, int gearsetId)
    {
        var ret = RaptureGearsetModule_UpdateGearsetHook.OriginalDisposeSafe(raptureGearsetModule, gearsetId);

        _jobChangedOrGearsetUpdatedCTS?.Cancel();
        _jobChangedOrGearsetUpdatedCTS = new();

        Service.Framework.RunOnTick(() =>
        {
            CheckForGearChecksumMismatch(gearsetId);
        }, delay: CheckDelay, cancellationToken: _jobChangedOrGearsetUpdatedCTS.Token);

        return ret;
    }

    private unsafe void CheckForGearChecksumMismatch(int gearsetId, bool disableReequip = false, bool disablePortraitSave = false)
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        if (!Config.NotifyGearChecksumMismatch || raptureGearsetModule->IsValidGearset(gearsetId) == 0)
            return;

        var gearset = raptureGearsetModule->GetGearset((int)gearsetId);
        if (gearset == null)
            return;

        var bannerIndex = *(byte*)((nint)gearset + 0x36);
        if (bannerIndex == 0) // no banner linked
            return;

        var bannerModule = BannerModule.Instance();
        var bannerId = bannerModule->GetBannerIdByBannerIndex(bannerIndex - 1);
        if (bannerId < 0) // banner not found
            return;

        var banner = bannerModule->GetBannerById(bannerId);
        if (banner == null) // banner not found
            return;

        if (banner->GearChecksum == GetEquippedGearChecksum())
        {
            Log($"Gear checksum matches! (Portrait: {banner->GearChecksum:X}, Equipped: {GetEquippedGearChecksum():X})");
            return;
        }

        Log($"Gear checksum mismatch detected! (Portrait: {banner->GearChecksum:X}, Equipped: {GetEquippedGearChecksum():X})");

        if (!disableReequip && Config.ReequipGearsetOnUpdate && gearset->GlamourSetLink > 0 && GameMain.IsInSanctuary())
        {
            Log($"Re-equipping Gearset #{gearset->ID + 1} to reapply Glamour Plate");
            raptureGearsetModule->EquipGearset(gearset->ID, gearset->GlamourSetLink);
            RecheckGearChecksumOrOpenPortraitEditor(banner);
        }
        else if(!disablePortraitSave && Config.AutoSavePotraitOnGearUpdate && gearset->GlamourSetLink == 0)
        {
            // Attempt to save the portrait with current gear
            Log($"Attempting to save portrait with currently equiped gear (Portrait: {banner->GearChecksum:X}, Equipped: {GetEquippedGearChecksum():X})");
            SavePortrait(banner);
        }
        else
        {
            NotifyMismatchOrOpenPortraitEditor();
        }
    }

    private unsafe void RecheckGearChecksumOrOpenPortraitEditor(BannerModuleEntry* banner)
    {
        _jobChangedOrGearsetUpdatedCTS?.Cancel();
        _jobChangedOrGearsetUpdatedCTS = new();

        Service.Framework.RunOnTick(() =>
        {
            if (banner->GearChecksum != GetEquippedGearChecksum())
            {
                Log($"Gear checksum still mismatching (Portrait: {banner->GearChecksum:X}, Equipped: {GetEquippedGearChecksum():X}), opening Banner Editor");
                NotifyMismatchOrOpenPortraitEditor();
            }
            else
            {
                Log($"Gear checksum matches now (Portrait: {banner->GearChecksum:X}, Equipped: {GetEquippedGearChecksum():X})");
            }
        }, delay: CheckDelay, cancellationToken: _jobChangedOrGearsetUpdatedCTS.Token); // TODO: find out when it's safe to check again instead of randomly picking a delay. ping may vary
    }

    private unsafe void NotifyMismatchOrOpenPortraitEditor()
    {
        var text = t("PortraitHelper.GearChecksumMismatch"); // based on LogMessage#5876

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex) == 1)
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

        Service.ChatGui.PrintError(sb.Build());
    }

    private unsafe uint GetEquippedGearChecksum()
    {
        using var checksumData = new DisposableStruct<GearsetChecksumData>();
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);

        for (var i = 0; i < 14; i++)
        {
            var item = container->Items[i];
            checksumData.Ptr->ItemIds[i] = item.GlamourID != 0 ? item.GlamourID : item.ItemID;
            checksumData.Ptr->StainIds[i] = item.Stain;
        }

        AgentStatus->UpdateGearVisibilityInNumberArray();

        var numberArray = AtkStage.GetSingleton()->GetNumberArrayData()[62];

        var gearVisibilityFlag = BannerGearVisibilityFlag.None;

        if (numberArray->IntArray[268] == 0)
            gearVisibilityFlag |= BannerGearVisibilityFlag.HeadgearHidden;

        if (numberArray->IntArray[269] == 0)
            gearVisibilityFlag |= BannerGearVisibilityFlag.WeaponHidden;

        if (numberArray->IntArray[270] == 1)
            gearVisibilityFlag |= BannerGearVisibilityFlag.VisorClosed;

        return GearsetChecksumData.GenerateChecksum(checksumData.Ptr->ItemIds, checksumData.Ptr->StainIds, gearVisibilityFlag);
    }
}
