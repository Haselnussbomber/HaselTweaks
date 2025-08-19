using System.IO;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Exd;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Services.PortraitHelper;

[RegisterSingleton, AutoConstruct]
public unsafe partial class BannerService
{
    private readonly ILogger<BannerService> _logger;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    public Image<Bgra32>? GetCurrentCharaViewImage()
    {
        var agent = AgentBannerEditor.Instance();
        if (agent->EditorState == null || agent->EditorState->CharaView == null)
            return null;

        var charaViewTexture = RenderTargetManager.Instance()->GetCharaViewTexture(agent->EditorState->CharaView->ClientObjectIndex);
        if (charaViewTexture == null || charaViewTexture->D3D11Texture2D == null)
            return null;

        var device = new SharpDX.Direct3D11.Device(_pluginInterface.UiBuilder.DeviceHandle);
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

    public bool IsBannerBgUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerBg>(id, out var bannerBg))
            return false;

        return IsBannerConditionUnlocked(bannerBg.UnlockCondition.RowId);
    }

    public bool IsBannerFrameUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerFrame>(id, out var bannerFrame))
            return false;

        return IsBannerConditionUnlocked(bannerFrame.UnlockCondition.RowId);
    }

    public bool IsBannerDecorationUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerDecoration>(id, out var bannerDecoration))
            return false;

        return IsBannerConditionUnlocked(bannerDecoration.UnlockCondition.RowId);
    }

    public bool IsBannerTimelineUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerTimeline>(id, out var bannerTimeline))
            return false;

        return IsBannerConditionUnlocked(bannerTimeline.UnlockCondition.RowId);
    }

    public bool IsBannerConditionUnlocked(uint id)
    {
        if (id == 0)
            return true;

        var exdModule = UIModule.Instance()->GetExcelModuleInterface()->ExdModule;
        if (exdModule == null)
            return false;

        var bannerConditionSheet = exdModule->GetSheetByName("BannerCondition");
        if (bannerConditionSheet == null)
            return false;

        var row = exdModule->GetRowBySheetAndRowId(bannerConditionSheet, id);
        if (row == null || row->Data == null)
            return false;

        return ExdModule.GetBannerConditionUnlockState(row->Data) == 0;
    }

    public string GetBannerTimelineName(uint id)
    {
        if (!_excelService.TryGetRow<BannerTimeline>(id, out var bannerTimeline))
            return _textService.GetAddonText(624); // Unknown

        var poseName = bannerTimeline.Name.ToString();

        if (string.IsNullOrEmpty(poseName) && bannerTimeline.Type != 0)
        {
            if (bannerTimeline.AdditionalData.TryGetValue<ActionSheet>(out var actionRow))
                poseName = actionRow.Name.ToString();
            else if (bannerTimeline.AdditionalData.TryGetValue<Emote>(out var emoteRow))
                poseName = emoteRow.Name.ToString();
        }

        return !string.IsNullOrEmpty(poseName)
            ? poseName
            : _textService.GetAddonText(624);
    }

    public unsafe void ImportPresetToState(PortraitPreset? preset, ImportFlags importFlags = ImportFlags.All)
    {
        if (preset == null)
            return;

        if (!TryGetAddon<AddonBannerEditor>(AgentId.BannerEditor, out var addonBannerEditor))
            return;

        _logger.LogDebug("Importing Preset {exportedString} with ImportFlags {importFlags}", preset.ToExportedString(), importFlags);

        var state = AgentBannerEditor.Instance()->EditorState;
        var bannerEntry = state->BannerEntry;

        // read current portrait and then overwrite what the flags allow below
        var tempPortraitData = stackalloc ExportedPortraitData[1];

        state->CharaView->ExportPortraitData(tempPortraitData);

        var hasBgChanged =
            importFlags.HasFlag(ImportFlags.BannerBg) &&
            IsBannerBgUnlocked(preset.BannerBg) &&
            tempPortraitData->BannerBg != preset.BannerBg;

        var hasFrameChanged =
            importFlags.HasFlag(ImportFlags.BannerFrame) &&
            IsBannerFrameUnlocked(preset.BannerFrame) &&
            bannerEntry.BannerFrame != preset.BannerFrame;

        var hasDecorationChanged =
            importFlags.HasFlag(ImportFlags.BannerDecoration) &&
            IsBannerDecorationUnlocked(preset.BannerDecoration) &&
            bannerEntry.BannerDecoration != preset.BannerDecoration;

        var hasBannerTimelineChanged =
            importFlags.HasFlag(ImportFlags.BannerTimeline) &&
            IsBannerTimelineUnlocked(preset.BannerTimeline) &&
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
            _logger.LogDebug("- BannerBg changed from {oldValue} to {newValue}",
                tempPortraitData->BannerBg,
                preset.BannerBg);

            bannerEntry.BannerBg = preset.BannerBg;
            tempPortraitData->BannerBg = preset.BannerBg;

            addonBannerEditor->BackgroundDropdown->SelectItem(GetUnlockedIndex(&state->Backgrounds, preset.BannerBg));
        }

        if (hasFrameChanged)
        {
            _logger.LogDebug("- BannerFrame changed from {oldValue} to {newValue}",
                bannerEntry.BannerFrame,
                preset.BannerFrame);

            state->SetFrame(preset.BannerFrame);

            addonBannerEditor->FrameDropdown->SelectItem(GetUnlockedIndex(&state->Frames, preset.BannerFrame));
        }

        if (hasDecorationChanged)
        {
            _logger.LogDebug("- BannerDecoration changed from {oldValue} to {newValue}",
                bannerEntry.BannerDecoration,
                preset.BannerDecoration);

            state->SetAccent(preset.BannerDecoration);

            addonBannerEditor->AccentDropdown->SelectItem(GetUnlockedIndex(&state->Accents, preset.BannerDecoration));
        }

        if (hasBgChanged || hasFrameChanged || hasDecorationChanged)
        {
            _logger.LogDebug("- Preset changed");

            var presetIndex = state->GetPresetIndex(bannerEntry.BannerBg, bannerEntry.BannerFrame, bannerEntry.BannerDecoration);
            if (presetIndex < 0)
            {
                presetIndex = addonBannerEditor->NumPresets - 1;

                addonBannerEditor->PresetDropdown->List->SetItemCount(addonBannerEditor->NumPresets); // increase to maximum, so "Custom" is displayed
            }

            addonBannerEditor->PresetDropdown->SelectItem(presetIndex);
        }

        if (hasBannerTimelineChanged)
        {
            _logger.LogDebug("- BannerTimeline changed from {oldValue} to {newValue}",
                tempPortraitData->BannerTimeline,
                preset.BannerTimeline);

            bannerEntry.BannerTimeline = preset.BannerTimeline;
            tempPortraitData->BannerTimeline = preset.BannerTimeline;

            addonBannerEditor->PoseDropdown->SelectItem(GetUnlockedIndex(&state->Poses, preset.BannerTimeline));
        }

        if (hasExpressionChanged)
        {
            _logger.LogDebug("- Expression changed from {oldValue} to {newValue}",
                tempPortraitData->Expression,
                preset.Expression);

            bannerEntry.Expression = preset.Expression;
            tempPortraitData->Expression = preset.Expression;

            addonBannerEditor->ExpressionDropdown->SelectItem(GetSortedIndex(&state->Expressions, preset.Expression));
        }

        if (hasAmbientLightingBrightnessChanged)
        {
            _logger.LogDebug("- AmbientLightingBrightness changed from {oldValue} to {newValue}",
                tempPortraitData->AmbientLightingBrightness,
                preset.AmbientLightingBrightness);

            tempPortraitData->AmbientLightingBrightness = preset.AmbientLightingBrightness;

            addonBannerEditor->AmbientLightingBrightnessSlider->SetValue(preset.AmbientLightingBrightness);
        }

        if (hasAmbientLightingColorChanged)
        {
            _logger.LogDebug("- AmbientLightingColor changed from {oldR}, {oldG}, {oldB} to {newR}, {newG}, {newB}",
                tempPortraitData->AmbientLightingColorRed,
                tempPortraitData->AmbientLightingColorGreen,
                tempPortraitData->AmbientLightingColorBlue,
                preset.AmbientLightingColorRed,
                preset.AmbientLightingColorGreen,
                preset.AmbientLightingColorBlue);

            tempPortraitData->AmbientLightingColorRed = preset.AmbientLightingColorRed;
            tempPortraitData->AmbientLightingColorGreen = preset.AmbientLightingColorGreen;
            tempPortraitData->AmbientLightingColorBlue = preset.AmbientLightingColorBlue;

            addonBannerEditor->AmbientLightingColorRedSlider->SetValue(preset.AmbientLightingColorRed);
            addonBannerEditor->AmbientLightingColorGreenSlider->SetValue(preset.AmbientLightingColorGreen);
            addonBannerEditor->AmbientLightingColorBlueSlider->SetValue(preset.AmbientLightingColorBlue);
        }

        if (hasDirectionalLightingBrightnessChanged)
        {
            _logger.LogDebug("- DirectionalLightingBrightness changed from {oldValue} to {newValue}",
                tempPortraitData->DirectionalLightingBrightness,
                preset.DirectionalLightingBrightness);

            tempPortraitData->DirectionalLightingBrightness = preset.DirectionalLightingBrightness;

            addonBannerEditor->DirectionalLightingBrightnessSlider->SetValue(preset.DirectionalLightingBrightness);
        }

        if (hasDirectionalLightingColorChanged)
        {
            _logger.LogDebug("- DirectionalLightingColor changed from {oldR}, {oldG}, {oldB} to {newR}, {newG}, {newB}",
                tempPortraitData->DirectionalLightingColorRed,
                tempPortraitData->DirectionalLightingColorGreen,
                tempPortraitData->DirectionalLightingColorBlue,
                preset.DirectionalLightingColorRed,
                preset.DirectionalLightingColorGreen,
                preset.DirectionalLightingColorBlue);

            tempPortraitData->DirectionalLightingColorRed = preset.DirectionalLightingColorRed;
            tempPortraitData->DirectionalLightingColorGreen = preset.DirectionalLightingColorGreen;
            tempPortraitData->DirectionalLightingColorBlue = preset.DirectionalLightingColorBlue;

            addonBannerEditor->DirectionalLightingColorRedSlider->SetValue(preset.DirectionalLightingColorRed);
            addonBannerEditor->DirectionalLightingColorGreenSlider->SetValue(preset.DirectionalLightingColorGreen);
            addonBannerEditor->DirectionalLightingColorBlueSlider->SetValue(preset.DirectionalLightingColorBlue);
        }

        if (hasDirectionalLightingVerticalAngleChanged)
        {
            _logger.LogDebug("- DirectionalLightingVerticalAngle changed from {oldValue} to {newValue}",
                tempPortraitData->DirectionalLightingVerticalAngle,
                preset.DirectionalLightingVerticalAngle);

            tempPortraitData->DirectionalLightingVerticalAngle = preset.DirectionalLightingVerticalAngle;

            addonBannerEditor->DirectionalLightingVerticalAngleSlider->SetValue(preset.DirectionalLightingVerticalAngle);
        }

        if (hasDirectionalLightingHorizontalAngleChanged)
        {
            _logger.LogDebug("- DirectionalLightingHorizontalAngle changed from {oldValue} to {newValue}",
                tempPortraitData->DirectionalLightingHorizontalAngle,
                preset.DirectionalLightingHorizontalAngle);

            tempPortraitData->DirectionalLightingHorizontalAngle = preset.DirectionalLightingHorizontalAngle;

            addonBannerEditor->DirectionalLightingHorizontalAngleSlider->SetValue(preset.DirectionalLightingHorizontalAngle);
        }

        if (hasAnimationProgressChanged)
        {
            _logger.LogDebug("- AnimationProgress changed from {oldValue} to {newValue}",
                tempPortraitData->AnimationProgress,
                preset.AnimationProgress);

            tempPortraitData->AnimationProgress = preset.AnimationProgress;
        }

        if (hasCameraPositionChanged)
        {
            _logger.LogDebug("- CameraPosition changed from {oldX}, {oldY}, {oldZ}, {oldW} to {newX}, {newY}, {newZ}, {newW}",
                tempPortraitData->CameraPosition.X,
                tempPortraitData->CameraPosition.Y,
                tempPortraitData->CameraPosition.Z,
                tempPortraitData->CameraPosition.W,
                preset.CameraPosition.X,
                preset.CameraPosition.Y,
                preset.CameraPosition.Z,
                preset.CameraPosition.W);

            tempPortraitData->CameraPosition = preset.CameraPosition;
        }

        if (hasCameraTargetChanged)
        {
            _logger.LogDebug("- CameraTarget changed from {oldX}, {oldY}, {oldZ}, {oldW} to {newX}, {newY}, {newZ}, {newW}",
                tempPortraitData->CameraTarget.X,
                tempPortraitData->CameraTarget.Y,
                tempPortraitData->CameraTarget.Z,
                tempPortraitData->CameraTarget.W,
                preset.CameraTarget.X,
                preset.CameraTarget.Y,
                preset.CameraTarget.Z,
                preset.CameraTarget.W);

            tempPortraitData->CameraTarget = preset.CameraTarget;
        }

        if (hasHeadDirectionChanged)
        {
            _logger.LogDebug("- HeadDirection changed from {oldX}, {oldY} to {newX}, {newY}",
                tempPortraitData->HeadDirection.X,
                tempPortraitData->HeadDirection.Y,
                preset.HeadDirection.X,
                preset.HeadDirection.Y);

            tempPortraitData->HeadDirection = preset.HeadDirection;
        }

        if (hasEyeDirectionChanged)
        {
            _logger.LogDebug("- EyeDirection changed from {oldX}, {oldY} to {newX}, {newY}",
                tempPortraitData->EyeDirection.X,
                tempPortraitData->EyeDirection.Y,
                preset.EyeDirection.X,
                preset.EyeDirection.Y);

            tempPortraitData->EyeDirection = preset.EyeDirection;
        }

        if (hasCameraZoomChanged)
        {
            _logger.LogDebug("- CameraZoom changed from {oldValue} to {newValue}",
                tempPortraitData->CameraZoom,
                preset.CameraZoom);

            tempPortraitData->CameraZoom = preset.CameraZoom;

            addonBannerEditor->CameraZoomSlider->SetValue(preset.CameraZoom);
        }

        if (hasImageRotationChanged)
        {
            _logger.LogDebug("- ImageRotation changed from {oldValue} to {newValue}",
                tempPortraitData->ImageRotation,
                preset.ImageRotation);

            tempPortraitData->ImageRotation = preset.ImageRotation;

            addonBannerEditor->ImageRotation->SetValue(preset.ImageRotation);
        }

        state->CharaView->ImportPortraitData(tempPortraitData);

        addonBannerEditor->PlayAnimationCheckbox->AtkComponentButton.IsChecked = false;
        addonBannerEditor->HeadFacingCameraCheckbox->AtkComponentButton.IsChecked = false;
        addonBannerEditor->EyesFacingCameraCheckbox->AtkComponentButton.IsChecked = false;

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

        _logger.LogDebug("Import complete");
    }

    private static unsafe int GetUnlockedIndex(AgentBannerEditorState.Dataset* dataset, ushort id)
    {
        for (var i = 0; i < dataset->UnlockedEntriesCount; i++)
        {
            var entry = dataset->UnlockedEntries[i];
            if (entry->RowId == id && entry->Row != 0)
                return i;
        }

        return 0;
    }

    private static unsafe int GetSortedIndex(AgentBannerEditorState.Dataset* dataset, ushort id)
    {
        for (var i = 0; i < dataset->SortedEntriesCount; i++)
        {
            var entry = dataset->SortedEntries[i];
            if (entry->RowId == id && entry->Row != 0)
                return i;
        }

        return 0;
    }
}
