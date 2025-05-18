using System.IO;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.JsonConverters;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;

namespace HaselTweaks.Records.PortraitHelper;

[JsonConverter(typeof(PortraitPresetConverter))]
public sealed record PortraitPreset
{
    public const int Magic = 0x53505448; // HTPS => HaselTweaks Preset String
    public ushort Version = 1;

    public HalfVector4 CameraPosition;
    public HalfVector4 CameraTarget;
    public short ImageRotation;
    public byte CameraZoom;
    public ushort BannerTimeline;
    public float AnimationProgress;
    public byte Expression;
    public HalfVector2 HeadDirection;
    public HalfVector2 EyeDirection;
    public byte DirectionalLightingColorRed;
    public byte DirectionalLightingColorGreen;
    public byte DirectionalLightingColorBlue;
    public byte DirectionalLightingBrightness;
    public short DirectionalLightingVerticalAngle;
    public short DirectionalLightingHorizontalAngle;
    public byte AmbientLightingColorRed;
    public byte AmbientLightingColorGreen;
    public byte AmbientLightingColorBlue;
    public byte AmbientLightingBrightness;
    public ushort BannerBg;

    public ushort BannerFrame;
    public ushort BannerDecoration;

    public unsafe bool ReadExportedPortraitData(ExportedPortraitData* portraitData)
    {
        if (portraitData == null)
            return false;

        CameraPosition = portraitData->CameraPosition;
        CameraTarget = portraitData->CameraTarget;
        ImageRotation = portraitData->ImageRotation;
        CameraZoom = portraitData->CameraZoom;
        BannerTimeline = portraitData->BannerTimeline;
        AnimationProgress = portraitData->AnimationProgress;
        Expression = portraitData->Expression;
        HeadDirection = portraitData->HeadDirection;
        EyeDirection = portraitData->EyeDirection;
        DirectionalLightingColorRed = portraitData->DirectionalLightingColorRed;
        DirectionalLightingColorGreen = portraitData->DirectionalLightingColorGreen;
        DirectionalLightingColorBlue = portraitData->DirectionalLightingColorBlue;
        DirectionalLightingBrightness = portraitData->DirectionalLightingBrightness;
        DirectionalLightingVerticalAngle = portraitData->DirectionalLightingVerticalAngle;
        DirectionalLightingHorizontalAngle = portraitData->DirectionalLightingHorizontalAngle;
        AmbientLightingColorRed = portraitData->AmbientLightingColorRed;
        AmbientLightingColorGreen = portraitData->AmbientLightingColorGreen;
        AmbientLightingColorBlue = portraitData->AmbientLightingColorBlue;
        AmbientLightingBrightness = portraitData->AmbientLightingBrightness;
        BannerBg = portraitData->BannerBg;

        return true;
    }

    public unsafe bool WriteExportedPortraitData(ExportedPortraitData* portraitData)
    {
        if (portraitData == null)
            return false;

        portraitData->CameraPosition = CameraPosition;
        portraitData->CameraTarget = CameraTarget;
        portraitData->ImageRotation = ImageRotation;
        portraitData->CameraZoom = CameraZoom;
        portraitData->BannerTimeline = BannerTimeline;
        portraitData->AnimationProgress = AnimationProgress;
        portraitData->Expression = Expression;
        portraitData->HeadDirection = HeadDirection;
        portraitData->EyeDirection = EyeDirection;
        portraitData->DirectionalLightingColorRed = DirectionalLightingColorRed;
        portraitData->DirectionalLightingColorGreen = DirectionalLightingColorGreen;
        portraitData->DirectionalLightingColorBlue = DirectionalLightingColorBlue;
        portraitData->DirectionalLightingBrightness = DirectionalLightingBrightness;
        portraitData->DirectionalLightingVerticalAngle = DirectionalLightingVerticalAngle;
        portraitData->DirectionalLightingHorizontalAngle = DirectionalLightingHorizontalAngle;
        portraitData->AmbientLightingColorRed = AmbientLightingColorRed;
        portraitData->AmbientLightingColorGreen = AmbientLightingColorGreen;
        portraitData->AmbientLightingColorBlue = AmbientLightingColorBlue;
        portraitData->AmbientLightingBrightness = AmbientLightingBrightness;
        portraitData->BannerBg = BannerBg;

        return true;
    }

    public string ToExportedString()
    {
        using var outputStream = new MemoryStream();
        using var writer = new BinaryWriter(outputStream);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(CameraPosition);
        writer.Write(CameraTarget);
        writer.Write(ImageRotation);
        writer.Write(CameraZoom);
        writer.Write(BannerTimeline);
        writer.Write(AnimationProgress);
        writer.Write(Expression);
        writer.Write(HeadDirection);
        writer.Write(EyeDirection);
        writer.Write(DirectionalLightingColorRed);
        writer.Write(DirectionalLightingColorGreen);
        writer.Write(DirectionalLightingColorBlue);
        writer.Write(DirectionalLightingBrightness);
        writer.Write(DirectionalLightingVerticalAngle);
        writer.Write(DirectionalLightingHorizontalAngle);
        writer.Write(AmbientLightingColorRed);
        writer.Write(AmbientLightingColorGreen);
        writer.Write(AmbientLightingColorBlue);
        writer.Write(AmbientLightingBrightness);
        writer.Write(BannerBg);
        writer.Write(BannerFrame);
        writer.Write(BannerDecoration);
        writer.Flush();

        return Convert.ToBase64String(outputStream.ToArray());
    }

    public static PortraitPreset? FromExportedString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] rawInput;
        try
        {
            rawInput = Convert.FromBase64String(input);
        }
        catch
        {
            return null;
        }

        if (rawInput.Length < 8)
            return null;

        using var inputStream = new MemoryStream(rawInput);
        using var reader = new BinaryReader(inputStream);

        var magic = reader.ReadInt32();
        if (magic != Magic)
            return null;

        var preset = new PortraitPreset
        {
            Version = reader.ReadUInt16()
        };

        switch (preset.Version)
        {
            case 1:
                preset.CameraPosition = reader.ReadHalfVector4();
                preset.CameraTarget = reader.ReadHalfVector4();
                preset.ImageRotation = reader.ReadInt16();
                preset.CameraZoom = reader.ReadByte();
                preset.BannerTimeline = reader.ReadUInt16();
                preset.AnimationProgress = reader.ReadSingle();
                preset.Expression = reader.ReadByte();
                preset.HeadDirection = reader.ReadHalfVector2();
                preset.EyeDirection = reader.ReadHalfVector2();
                preset.DirectionalLightingColorRed = reader.ReadByte();
                preset.DirectionalLightingColorGreen = reader.ReadByte();
                preset.DirectionalLightingColorBlue = reader.ReadByte();
                preset.DirectionalLightingBrightness = reader.ReadByte();
                preset.DirectionalLightingVerticalAngle = reader.ReadInt16();
                preset.DirectionalLightingHorizontalAngle = reader.ReadInt16();
                preset.AmbientLightingColorRed = reader.ReadByte();
                preset.AmbientLightingColorGreen = reader.ReadByte();
                preset.AmbientLightingColorBlue = reader.ReadByte();
                preset.AmbientLightingBrightness = reader.ReadByte();
                preset.BannerBg = reader.ReadUInt16();
                preset.BannerFrame = reader.ReadUInt16();
                preset.BannerDecoration = reader.ReadUInt16();
                break;

            default:
                throw new Exception($"Unknown Preset version {preset.Version}");
        }

        return preset;
    }

    public async void ToClipboard(ILogger logger)
    {
        await ClipboardUtils.OpenClipboard();
        try
        {
            PInvoke.EmptyClipboard();

            var clipboardText = Marshal.StringToHGlobalAnsi(ToExportedString());
            if (PInvoke.SetClipboardData((uint)CLIPBOARD_FORMAT.CF_TEXT, (HANDLE)clipboardText) != 0)
                Tweaks.PortraitHelper.ClipboardPreset = this;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during PortraitPreset.ToClipboard");
        }
        finally
        {
            PInvoke.CloseClipboard();
        }
    }

    public static unsafe PortraitPreset? FromState()
    {
        var state = AgentBannerEditor.Instance()->EditorState;
        var preset = new PortraitPreset();

        var portraitData = stackalloc ExportedPortraitData[1];
        state->CharaView->ExportPortraitData(portraitData);
        preset.ReadExportedPortraitData(portraitData);

        preset.BannerFrame = state->BannerEntry.BannerFrame;
        preset.BannerDecoration = state->BannerEntry.BannerDecoration;

        return preset;
    }

    public unsafe void ToState(ILogger logger, BannerUtils bannerUtils, ImportFlags importFlags)
    {
        if (!TryGetAddon<AddonBannerEditor>(AgentId.BannerEditor, out var addonBannerEditor))
            return;

        logger.LogDebug($"Importing Preset {ToExportedString()} with ImportFlags {importFlags}");

        var state = AgentBannerEditor.Instance()->EditorState;
        var bannerEntry = state->BannerEntry;

        // read current portrait and then overwrite what the flags allow below
        var tempPortraitData = stackalloc ExportedPortraitData[1];

        state->CharaView->ExportPortraitData(tempPortraitData);

        var hasBgChanged =
            importFlags.HasFlag(ImportFlags.BannerBg) &&
            bannerUtils.IsBannerBgUnlocked(BannerBg) &&
            tempPortraitData->BannerBg != BannerBg;

        var hasFrameChanged =
            importFlags.HasFlag(ImportFlags.BannerFrame) &&
            bannerUtils.IsBannerFrameUnlocked(BannerFrame) &&
            bannerEntry.BannerFrame != BannerFrame;

        var hasDecorationChanged =
            importFlags.HasFlag(ImportFlags.BannerDecoration) &&
            bannerUtils.IsBannerDecorationUnlocked(BannerDecoration) &&
            bannerEntry.BannerDecoration != BannerDecoration;

        var hasBannerTimelineChanged =
            importFlags.HasFlag(ImportFlags.BannerTimeline) &&
            bannerUtils.IsBannerTimelineUnlocked(BannerTimeline) &&
            tempPortraitData->BannerTimeline != BannerTimeline;

        var hasExpressionChanged =
            importFlags.HasFlag(ImportFlags.Expression) &&
            tempPortraitData->Expression != Expression;

        var hasAmbientLightingBrightnessChanged =
            importFlags.HasFlag(ImportFlags.AmbientLightingBrightness) &&
            tempPortraitData->AmbientLightingBrightness != AmbientLightingBrightness;

        var hasAmbientLightingColorChanged =
            importFlags.HasFlag(ImportFlags.AmbientLightingColor) && (
                tempPortraitData->AmbientLightingColorRed != AmbientLightingColorRed ||
                tempPortraitData->AmbientLightingColorGreen != AmbientLightingColorGreen ||
                tempPortraitData->AmbientLightingColorBlue != AmbientLightingColorBlue
            );

        var hasDirectionalLightingBrightnessChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingBrightness) &&
            tempPortraitData->DirectionalLightingBrightness != DirectionalLightingBrightness;

        var hasDirectionalLightingColorChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingColor) && (
                tempPortraitData->DirectionalLightingColorRed != DirectionalLightingColorRed ||
                tempPortraitData->DirectionalLightingColorGreen != DirectionalLightingColorGreen ||
                tempPortraitData->DirectionalLightingColorBlue != DirectionalLightingColorBlue
            );

        var hasDirectionalLightingVerticalAngleChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingVerticalAngle) &&
            tempPortraitData->DirectionalLightingVerticalAngle != DirectionalLightingVerticalAngle;

        var hasDirectionalLightingHorizontalAngleChanged =
            importFlags.HasFlag(ImportFlags.DirectionalLightingHorizontalAngle) &&
            tempPortraitData->DirectionalLightingHorizontalAngle != DirectionalLightingHorizontalAngle;

        var hasAnimationProgressChanged =
            importFlags.HasFlag(ImportFlags.AnimationProgress) &&
            !tempPortraitData->AnimationProgress.IsApproximately(AnimationProgress, 0.01f);

        var hasCameraPositionChanged =
            importFlags.HasFlag(ImportFlags.CameraPosition) &&
            !tempPortraitData->CameraPosition.IsApproximately(CameraPosition);

        var hasCameraTargetChanged =
            importFlags.HasFlag(ImportFlags.CameraTarget) &&
            !tempPortraitData->CameraTarget.IsApproximately(CameraTarget);

        var hasHeadDirectionChanged =
            importFlags.HasFlag(ImportFlags.HeadDirection) &&
            !tempPortraitData->HeadDirection.IsApproximately(HeadDirection);

        var hasEyeDirectionChanged =
            importFlags.HasFlag(ImportFlags.EyeDirection) &&
            !tempPortraitData->EyeDirection.IsApproximately(EyeDirection);

        var hasCameraZoomChanged =
            importFlags.HasFlag(ImportFlags.CameraZoom) &&
            tempPortraitData->CameraZoom != CameraZoom;

        var hasImageRotationChanged =
            importFlags.HasFlag(ImportFlags.ImageRotation) &&
            tempPortraitData->ImageRotation != ImageRotation;

        if (hasBgChanged)
        {
            logger.LogDebug($"- BannerBg changed from {tempPortraitData->BannerBg} to {BannerBg}");

            bannerEntry.BannerBg = BannerBg;
            tempPortraitData->BannerBg = BannerBg;

            addonBannerEditor->BackgroundDropdown->SelectItem(GetUnlockedIndex(&state->Backgrounds, BannerBg));
        }

        if (hasFrameChanged)
        {
            logger.LogDebug($"- BannerFrame changed from {bannerEntry.BannerFrame} to {BannerFrame}");

            state->SetFrame(BannerFrame);

            addonBannerEditor->FrameDropdown->SelectItem(GetUnlockedIndex(&state->Frames, BannerFrame));
        }

        if (hasDecorationChanged)
        {
            logger.LogDebug($"- BannerDecoration changed from {bannerEntry.BannerDecoration} to {BannerDecoration}");

            state->SetAccent(BannerDecoration);

            addonBannerEditor->AccentDropdown->SelectItem(GetUnlockedIndex(&state->Accents, BannerDecoration));
        }

        if (hasBgChanged || hasFrameChanged || hasDecorationChanged)
        {
            logger.LogDebug("- Preset changed");

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
            logger.LogDebug($"- BannerTimeline changed from {tempPortraitData->BannerTimeline} to {BannerTimeline}");

            bannerEntry.BannerTimeline = BannerTimeline;
            tempPortraitData->BannerTimeline = BannerTimeline;

            addonBannerEditor->PoseDropdown->SelectItem(GetUnlockedIndex(&state->Poses, BannerTimeline));
        }

        if (hasExpressionChanged)
        {
            logger.LogDebug($"- Expression changed from {tempPortraitData->Expression} to {Expression}");

            bannerEntry.Expression = Expression;
            tempPortraitData->Expression = Expression;

            addonBannerEditor->ExpressionDropdown->SelectItem(GetSortedIndex(&state->Expressions, Expression));
        }

        if (hasAmbientLightingBrightnessChanged)
        {
            logger.LogDebug($"- AmbientLightingBrightness changed from {tempPortraitData->AmbientLightingBrightness} to {AmbientLightingBrightness}");

            tempPortraitData->AmbientLightingBrightness = AmbientLightingBrightness;

            addonBannerEditor->AmbientLightingBrightnessSlider->SetValue(AmbientLightingBrightness);
        }

        if (hasAmbientLightingColorChanged)
        {
            logger.LogDebug($"- AmbientLightingColor changed from {tempPortraitData->AmbientLightingColorRed}, {tempPortraitData->AmbientLightingColorGreen}, {tempPortraitData->AmbientLightingColorBlue} to {AmbientLightingColorRed}, {AmbientLightingColorGreen}, {AmbientLightingColorBlue}");

            tempPortraitData->AmbientLightingColorRed = AmbientLightingColorRed;
            tempPortraitData->AmbientLightingColorGreen = AmbientLightingColorGreen;
            tempPortraitData->AmbientLightingColorBlue = AmbientLightingColorBlue;

            addonBannerEditor->AmbientLightingColorRedSlider->SetValue(AmbientLightingColorRed);
            addonBannerEditor->AmbientLightingColorGreenSlider->SetValue(AmbientLightingColorGreen);
            addonBannerEditor->AmbientLightingColorBlueSlider->SetValue(AmbientLightingColorBlue);
        }

        if (hasDirectionalLightingBrightnessChanged)
        {
            logger.LogDebug($"- DirectionalLightingBrightness changed from {tempPortraitData->DirectionalLightingBrightness} to {DirectionalLightingBrightness}");

            tempPortraitData->DirectionalLightingBrightness = DirectionalLightingBrightness;

            addonBannerEditor->DirectionalLightingBrightnessSlider->SetValue(DirectionalLightingBrightness);
        }

        if (hasDirectionalLightingColorChanged)
        {
            logger.LogDebug($"- DirectionalLightingColor changed from {tempPortraitData->DirectionalLightingColorRed}, {tempPortraitData->DirectionalLightingColorGreen}, {tempPortraitData->DirectionalLightingColorBlue} to {DirectionalLightingColorRed}, {DirectionalLightingColorGreen}, {DirectionalLightingColorBlue}");

            tempPortraitData->DirectionalLightingColorRed = DirectionalLightingColorRed;
            tempPortraitData->DirectionalLightingColorGreen = DirectionalLightingColorGreen;
            tempPortraitData->DirectionalLightingColorBlue = DirectionalLightingColorBlue;

            addonBannerEditor->DirectionalLightingColorRedSlider->SetValue(DirectionalLightingColorRed);
            addonBannerEditor->DirectionalLightingColorGreenSlider->SetValue(DirectionalLightingColorGreen);
            addonBannerEditor->DirectionalLightingColorBlueSlider->SetValue(DirectionalLightingColorBlue);
        }

        if (hasDirectionalLightingVerticalAngleChanged)
        {
            logger.LogDebug($"- DirectionalLightingVerticalAngle changed from {tempPortraitData->DirectionalLightingVerticalAngle} to {DirectionalLightingVerticalAngle}");

            tempPortraitData->DirectionalLightingVerticalAngle = DirectionalLightingVerticalAngle;

            addonBannerEditor->DirectionalLightingVerticalAngleSlider->SetValue(DirectionalLightingVerticalAngle);
        }

        if (hasDirectionalLightingHorizontalAngleChanged)
        {
            logger.LogDebug($"- DirectionalLightingHorizontalAngle changed from {tempPortraitData->DirectionalLightingHorizontalAngle} to {DirectionalLightingHorizontalAngle}");

            tempPortraitData->DirectionalLightingHorizontalAngle = DirectionalLightingHorizontalAngle;

            addonBannerEditor->DirectionalLightingHorizontalAngleSlider->SetValue(DirectionalLightingHorizontalAngle);
        }

        if (hasAnimationProgressChanged)
        {
            logger.LogDebug($"- AnimationProgress changed from {tempPortraitData->AnimationProgress} to {AnimationProgress}");

            tempPortraitData->AnimationProgress = AnimationProgress;
        }

        if (hasCameraPositionChanged)
        {
            logger.LogDebug($"- CameraPosition changed from {tempPortraitData->CameraPosition.X}, {tempPortraitData->CameraPosition.Y}, {tempPortraitData->CameraPosition.Z}, {tempPortraitData->CameraPosition.W} to {CameraPosition.X}, {CameraPosition.Y}, {CameraPosition.Z}, {CameraPosition.W}");

            tempPortraitData->CameraPosition = CameraPosition;
        }

        if (hasCameraTargetChanged)
        {
            logger.LogDebug($"- CameraTarget changed from {tempPortraitData->CameraTarget.X}, {tempPortraitData->CameraTarget.Y}, {tempPortraitData->CameraTarget.Z}, {tempPortraitData->CameraTarget.W} to {CameraTarget.X}, {CameraTarget.Y}, {CameraTarget.Z}, {CameraTarget.W}");

            tempPortraitData->CameraTarget = CameraTarget;
        }

        if (hasHeadDirectionChanged)
        {
            logger.LogDebug($"- HeadDirection changed from {tempPortraitData->HeadDirection.X}, {tempPortraitData->HeadDirection.Y} to {HeadDirection.X}, {HeadDirection.Y}");

            tempPortraitData->HeadDirection = HeadDirection;
        }

        if (hasEyeDirectionChanged)
        {
            logger.LogDebug($"- EyeDirection changed from {tempPortraitData->EyeDirection.X}, {tempPortraitData->EyeDirection.Y} to {EyeDirection.X}, {EyeDirection.Y}");

            tempPortraitData->EyeDirection = EyeDirection;
        }

        if (hasCameraZoomChanged)
        {
            logger.LogDebug($"- CameraZoom changed from {tempPortraitData->CameraZoom} to {CameraZoom}");

            tempPortraitData->CameraZoom = CameraZoom;

            addonBannerEditor->CameraZoomSlider->SetValue(CameraZoom);
        }

        if (hasImageRotationChanged)
        {
            logger.LogDebug($"- ImageRotation changed from {tempPortraitData->ImageRotation} to {ImageRotation}");

            tempPortraitData->ImageRotation = ImageRotation;

            addonBannerEditor->ImageRotation->SetValue(ImageRotation);
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

        logger.LogDebug("Import complete");
    }

    private static unsafe int GetUnlockedIndex(AgentBannerEditorState.Dataset* dataset, ushort id)
    {
        for (var i = 0; i < dataset->UnlockedEntriesCount; i++)
        {
            var entry = dataset->UnlockedEntries[i];
            if (entry->RowId == id && entry->Row != 0)
            {
                return i;
            }
        }

        return 0;
    }

    private static unsafe int GetSortedIndex(AgentBannerEditorState.Dataset* dataset, ushort id)
    {
        for (var i = 0; i < dataset->SortedEntriesCount; i++)
        {
            var entry = dataset->SortedEntries[i];
            if (entry->RowId == id && entry->Row != 0)
            {
                return i;
            }
        }

        return 0;
    }
}
