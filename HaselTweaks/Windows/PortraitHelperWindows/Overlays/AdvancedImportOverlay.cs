using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterTransient, AutoConstruct]
public unsafe partial class AdvancedImportOverlay : Overlay
{
    private readonly MenuBarState _state;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly BannerService _bannerService;
    private readonly ClipboardService _clipboardService;

    public override void Draw()
    {
        base.Draw();

        if (_clipboardService.ClipboardPreset == null)
        {
            Close();
            return;
        }

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y * 2);

        var state = AgentBannerEditor.Instance()->EditorState;
        var unknown = _textService.GetAddonText(624) ?? "Unknown";

        if (ImGui.Button(_textService.GetAddonText(14923) ?? "Select All"))
            _clipboardService.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(_textService.GetAddonText(14924) ?? "Deselect All"))
            _clipboardService.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button(_textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ImportSelectedSettingsButton.Label")))
        {
            _bannerService.ImportPresetToState(_clipboardService.ClipboardPreset, _clipboardService.CurrentImportFlags);
            _state.CloseOverlay();
        }

        ImGuiUtils.DrawSection(_textService.GetAddonText(14684) ?? "Design", respectUiTheme: !IsWindow);

        var isBannerBgUnlocked = _bannerService.IsBannerBgUnlocked(_clipboardService.ClipboardPreset.BannerBg);
        DrawImportSetting(
            _textService.GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () =>
            {
                if (_excelService.TryGetRow<BannerBg>(_clipboardService.ClipboardPreset.BannerBg, out var bannerBgRow))
                    ImGui.Text(bannerBgRow.Name.ToString());
                else
                    ImGui.Text(unknown);

                if (!isBannerBgUnlocked)
                {
                    ImGui.TextColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerBgUnlocked
        );

        var isBannerFrameUnlocked = _bannerService.IsBannerFrameUnlocked(_clipboardService.ClipboardPreset.BannerFrame);
        DrawImportSetting(
            _textService.GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () =>
            {
                if (_excelService.TryGetRow<BannerFrame>(_clipboardService.ClipboardPreset.BannerFrame, out var bannerFrameRow))
                    ImGui.Text(bannerFrameRow.Name.ToString());
                else
                    ImGui.Text(unknown);

                if (!isBannerFrameUnlocked)
                {
                    ImGui.TextColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerFrameUnlocked
        );

        var isBannerDecorationUnlocked = _bannerService.IsBannerDecorationUnlocked(_clipboardService.ClipboardPreset.BannerDecoration);
        DrawImportSetting(
            _textService.GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () =>
            {
                if (_excelService.TryGetRow<BannerDecoration>(_clipboardService.ClipboardPreset.BannerDecoration, out var bannerDecorationRow))
                    ImGui.Text(bannerDecorationRow.Name.ToString());
                else
                    ImGui.Text(unknown);

                if (!isBannerDecorationUnlocked)
                {
                    ImGui.TextColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerDecorationUnlocked
        );

        DrawImportSetting(
            _textService.GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.Text(_clipboardService.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            _textService.GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.Text(_clipboardService.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14685) ?? "Character", respectUiTheme: !IsWindow);

        var isBannerTimelineUnlocked = _bannerService.IsBannerTimelineUnlocked(_clipboardService.ClipboardPreset.BannerTimeline);
        DrawImportSetting(
            _textService.GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                ImGui.Text(_bannerService.GetBannerTimelineName(_clipboardService.ClipboardPreset.BannerTimeline));

                if (!isBannerTimelineUnlocked)
                {
                    ImGui.TextColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerTimelineUnlocked
        );

        DrawImportSetting(
            _textService.GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = _clipboardService.ClipboardPreset.Expression;
                var expressionName = unknown;

                if (id == 0)
                {
                    expressionName = _textService.GetAddonText(14727) ?? "None";
                }
                else
                {
                    for (var i = 0; i < state->Expressions.SortedEntriesCount; i++)
                    {
                        var entry = state->Expressions.SortedEntries[i];
                        if (entry->RowId == id
                        && entry->SupplementalRow != 0
                        && _excelService.TryGetRow<BannerFacial>(entry->RowId, out var bannerFacialRow)
                        && _excelService.TryGetRow<Emote>(bannerFacialRow.Emote.RowId, out var emoteRow))
                        {
                            expressionName = emoteRow.Name.ToString();
                            break;
                        }
                    }
                }

                ImGui.Text(expressionName);
            }
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.Label"),
            ImportFlags.AnimationProgress,
            () => ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.ValueFormat", _clipboardService.ClipboardPreset.AnimationProgress))
        );

        DrawImportSetting(
            _textService.GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(_clipboardService.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.CameraTarget.Label"),
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(_clipboardService.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.HeadDirection.Label"),
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(_clipboardService.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.EyeDirection.Label"),
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(_clipboardService.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14692) ?? "Ambient Lighting", respectUiTheme: !IsWindow);

        var labelBrightness = _textService.GetAddonText(14694) ?? "Brightness";
        var labelColor = _textService.GetAddonText(7008) ?? "Color";

        DrawImportSetting(
            labelBrightness,
            ImportFlags.AmbientLightingBrightness,
            () => ImGui.Text(_clipboardService.ClipboardPreset.AmbientLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.AmbientLightingColor,
            () => DrawColor(
                _clipboardService.ClipboardPreset.AmbientLightingColorRed,
                _clipboardService.ClipboardPreset.AmbientLightingColorGreen,
                _clipboardService.ClipboardPreset.AmbientLightingColorBlue
            )
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14693) ?? "Directional Lighting", respectUiTheme: !IsWindow);

        DrawImportSetting(
            labelBrightness,
            ImportFlags.DirectionalLightingBrightness,
            () => ImGui.Text(_clipboardService.ClipboardPreset.DirectionalLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.DirectionalLightingColor,
            () => DrawColor(
                _clipboardService.ClipboardPreset.DirectionalLightingColorRed,
                _clipboardService.ClipboardPreset.DirectionalLightingColorGreen,
                _clipboardService.ClipboardPreset.DirectionalLightingColorBlue
            )
        );

        DrawImportSetting(
            _textService.GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.Text(_clipboardService.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            _textService.GetAddonText(14695) ?? "Horizontal Angle",
            ImportFlags.DirectionalLightingHorizontalAngle,
            () => ImGui.Text(_clipboardService.ClipboardPreset.DirectionalLightingHorizontalAngle.ToString())
        );

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
    }

    private void DrawImportSetting(string label, ImportFlags flag, System.Action drawFn, bool isUnlocked = true)
    {
        using var id = ImRaii.PushId(flag.ToString());

        ImGui.Columns(2, "##Columns", false);

        var isEnabled = isUnlocked && _clipboardService.CurrentImportFlags.HasFlag(flag);
        using var _textColor = !isEnabled ? (Color.From(ImGuiCol.Text) with { A = 0.5f }).Push(ImGuiCol.Text) : null;
        using var _disabled = ImRaii.Disabled(!isUnlocked);

        if (ImGui.Checkbox(label + "##Checkbox", ref isEnabled))
        {
            if (isEnabled)
                _clipboardService.CurrentImportFlags |= flag;
            else
                _clipboardService.CurrentImportFlags &= ~flag;
        }

        _disabled?.Dispose();
        _textColor?.Dispose();

        using (ImRaii.Disabled(!isEnabled))
        {
            ImGui.NextColumn();
            drawFn();
        }

        ImGui.Columns(1);
    }

    private void DrawColor(byte r, byte g, byte b)
    {
        var vec = new System.Numerics.Vector3(r / 255f, g / 255f, b / 255f);
        using var table = ImRaii.Table("##Table", 4);
        if (!table)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TableSetupColumn("Preview", ImGuiTableColumnFlags.WidthFixed, 26 * scale);
        ImGui.TableSetupColumn("R", ImGuiTableColumnFlags.WidthFixed, 40 * scale);
        ImGui.TableSetupColumn("G", ImGuiTableColumnFlags.WidthFixed, 40 * scale);
        ImGui.TableSetupColumn("B", ImGuiTableColumnFlags.WidthFixed, 40 * scale);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.ColorEdit3("##ColorEdit3", ref vec, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoInputs);

        void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ColorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.Text(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ColorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.Text(valueStr);
        }

        drawColumn("R", r);
        drawColumn("G", g);
        drawColumn("B", b);
    }

    private void DrawHalfVector2(HalfVector2 vec)
    {
        using var table = ImRaii.Table("##Table", 2);
        if (!table)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.Text(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.Text(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
    }

    private void DrawHalfVector4(HalfVector4 vec)
    {
        using var table = ImRaii.Table("##Table", 4);
        if (!table)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("W", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.Text(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.Text(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
        drawColumn("Z", vec.Z);
        drawColumn("W", vec.W);
    }
}
