using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselTweaks.Enums.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterScoped, AutoConstruct]
public unsafe partial class AdvancedImportOverlay : Overlay
{
    private readonly ILogger<AdvancedImportOverlay> _logger;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly BannerUtils _bannerUtils;

    public MenuBar MenuBar { get; internal set; } = null!;

    public void Open(MenuBar menuBar)
    {
        MenuBar = menuBar;
        Open();
    }

    public override void Draw()
    {
        base.Draw();

        if (PortraitHelper.ClipboardPreset == null)
        {
            Close();
            return;
        }

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y * 2);

        var state = AgentBannerEditor.Instance()->EditorState;
        var unknown = _textService.GetAddonText(624) ?? "Unknown";

        if (ImGui.Button(_textService.GetAddonText(14923) ?? "Select All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(_textService.GetAddonText(14924) ?? "Deselect All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button(_textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ImportSelectedSettingsButton.Label")))
        {
            PortraitHelper.ClipboardPreset.ToState(_logger, _bannerUtils, PortraitHelper.CurrentImportFlags);
            MenuBar.CloseOverlays();
        }

        ImGuiUtils.DrawSection(_textService.GetAddonText(14684) ?? "Design", respectUiTheme: !IsWindow);

        var isBannerBgUnlocked = _bannerUtils.IsBannerBgUnlocked(PortraitHelper.ClipboardPreset.BannerBg);
        DrawImportSetting(
            _textService.GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () =>
            {
                if (_excelService.TryGetRow<BannerBg>(PortraitHelper.ClipboardPreset.BannerBg, out var bannerBgRow))
                    ImGui.TextUnformatted(bannerBgRow.Name.ExtractText());
                else
                    ImGui.TextUnformatted(unknown);

                if (!isBannerBgUnlocked)
                {
                    ImGuiUtils.TextUnformattedColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerBgUnlocked
        );

        var isBannerFrameUnlocked = _bannerUtils.IsBannerFrameUnlocked(PortraitHelper.ClipboardPreset.BannerFrame);
        DrawImportSetting(
            _textService.GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () =>
            {
                if (_excelService.TryGetRow<BannerFrame>(PortraitHelper.ClipboardPreset.BannerFrame, out var bannerFrameRow))
                    ImGui.TextUnformatted(bannerFrameRow.Name.ExtractText());
                else
                    ImGui.TextUnformatted(unknown);

                if (!isBannerFrameUnlocked)
                {
                    ImGuiUtils.TextUnformattedColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerFrameUnlocked
        );

        var isBannerDecorationUnlocked = _bannerUtils.IsBannerDecorationUnlocked(PortraitHelper.ClipboardPreset.BannerDecoration);
        DrawImportSetting(
            _textService.GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () =>
            {
                if (_excelService.TryGetRow<BannerDecoration>(PortraitHelper.ClipboardPreset.BannerDecoration, out var bannerDecorationRow))
                    ImGui.TextUnformatted(bannerDecorationRow.Name.ExtractText());
                else
                    ImGui.TextUnformatted(unknown);

                if (!isBannerDecorationUnlocked)
                {
                    ImGuiUtils.TextUnformattedColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerDecorationUnlocked
        );

        DrawImportSetting(
            _textService.GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            _textService.GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14685) ?? "Character", respectUiTheme: !IsWindow);

        var isBannerTimelineUnlocked = _bannerUtils.IsBannerTimelineUnlocked(PortraitHelper.ClipboardPreset.BannerTimeline);
        DrawImportSetting(
            _textService.GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                ImGui.TextUnformatted(_bannerUtils.GetBannerTimelineName(PortraitHelper.ClipboardPreset.BannerTimeline));

                if (!isBannerTimelineUnlocked)
                {
                    ImGuiUtils.TextUnformattedColored(Color.Red, _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerTimelineUnlocked
        );

        DrawImportSetting(
            _textService.GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = PortraitHelper.ClipboardPreset.Expression;
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
                            expressionName = emoteRow.Name.ExtractText();
                            break;
                        }
                    }
                }

                ImGui.TextUnformatted(expressionName);
            }
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.Label"),
            ImportFlags.AnimationProgress,
            () => ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.ValueFormat", PortraitHelper.ClipboardPreset.AnimationProgress))
        );

        DrawImportSetting(
            _textService.GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.CameraTarget.Label"),
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.HeadDirection.Label"),
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            _textService.Translate("PortraitHelperWindows.Setting.EyeDirection.Label"),
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14692) ?? "Ambient Lighting", respectUiTheme: !IsWindow);

        var labelBrightness = _textService.GetAddonText(14694) ?? "Brightness";
        var labelColor = _textService.GetAddonText(7008) ?? "Color";

        DrawImportSetting(
            labelBrightness,
            ImportFlags.AmbientLightingBrightness,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.AmbientLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.AmbientLightingColor,
            () => DrawColor(
                PortraitHelper.ClipboardPreset.AmbientLightingColorRed,
                PortraitHelper.ClipboardPreset.AmbientLightingColorGreen,
                PortraitHelper.ClipboardPreset.AmbientLightingColorBlue
            )
        );

        ImGuiUtils.DrawSection(_textService.GetAddonText(14693) ?? "Directional Lighting", respectUiTheme: !IsWindow);

        DrawImportSetting(
            labelBrightness,
            ImportFlags.DirectionalLightingBrightness,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.DirectionalLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.DirectionalLightingColor,
            () => DrawColor(
                PortraitHelper.ClipboardPreset.DirectionalLightingColorRed,
                PortraitHelper.ClipboardPreset.DirectionalLightingColorGreen,
                PortraitHelper.ClipboardPreset.DirectionalLightingColorBlue
            )
        );

        DrawImportSetting(
            _textService.GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            _textService.GetAddonText(14695) ?? "Horizontal Angle",
            ImportFlags.DirectionalLightingHorizontalAngle,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.DirectionalLightingHorizontalAngle.ToString())
        );

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
    }

    private static void DrawImportSetting(string label, ImportFlags flag, System.Action drawFn, bool isUnlocked = true)
    {
        using var id = ImRaii.PushId(flag.ToString());

        ImGui.Columns(2, "##Columns", false);

        var isEnabled = isUnlocked && PortraitHelper.CurrentImportFlags.HasFlag(flag);
        using var _textColor = !isEnabled ? (Color.From(ImGuiCol.Text) with { A = 0.5f }).Push(ImGuiCol.Text) : null;
        using var _disabled = ImRaii.Disabled(!isUnlocked);

        if (ImGui.Checkbox(label + "##Checkbox", ref isEnabled))
        {
            if (isEnabled)
                PortraitHelper.CurrentImportFlags |= flag;
            else
                PortraitHelper.CurrentImportFlags &= ~flag;
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
            ImGui.TextUnformatted(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ColorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
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
            ImGui.TextUnformatted(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
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
            ImGui.TextUnformatted(label);

            var valueStr = _textService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
        drawColumn("Z", vec.Z);
        drawColumn("W", vec.W);
    }
}
