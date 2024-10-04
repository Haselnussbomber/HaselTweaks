using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselCommon.Extensions.Strings;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedImportOverlay(
    ILogger<AdvancedImportOverlay> Logger,
    TextService TextService,
    WindowManager windowManager,
    PluginConfig pluginConfig,
    ExcelService excelService,
    BannerUtils BannerUtils)
    : Overlay(
        windowManager,
        pluginConfig,
        excelService,
        TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.Title"))
{
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
        var unknown = TextService.GetAddonText(624) ?? "Unknown";

        if (ImGui.Button(TextService.GetAddonText(14923) ?? "Select All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(TextService.GetAddonText(14924) ?? "Deselect All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button(TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ImportSelectedSettingsButton.Label")))
        {
            PortraitHelper.ClipboardPreset.ToState(Logger, BannerUtils, PortraitHelper.CurrentImportFlags);
            MenuBar.CloseOverlays();
        }

        ImGuiUtils.DrawSection(TextService.GetAddonText(14684) ?? "Design", respectUiTheme: !IsWindow);

        var isBannerBgUnlocked = BannerUtils.IsBannerBgUnlocked(PortraitHelper.ClipboardPreset.BannerBg);
        DrawImportSetting(
            TextService.GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () =>
            {
                ImGui.TextUnformatted(ExcelService.GetRow<BannerBg>(PortraitHelper.ClipboardPreset.BannerBg)?.Name.ExtractText() ?? unknown);

                if (!isBannerBgUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Red))
                        TextService.Draw("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked");
                }
            },
            isBannerBgUnlocked
        );

        var isBannerFrameUnlocked = BannerUtils.IsBannerFrameUnlocked(PortraitHelper.ClipboardPreset.BannerFrame);
        DrawImportSetting(
            TextService.GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () =>
            {
                ImGui.TextUnformatted(ExcelService.GetRow<BannerFrame>(PortraitHelper.ClipboardPreset.BannerFrame)?.Name.ExtractText() ?? unknown);

                if (!isBannerFrameUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Red))
                        TextService.Draw("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked");
                }
            },
            isBannerFrameUnlocked
        );

        var isBannerDecorationUnlocked = BannerUtils.IsBannerDecorationUnlocked(PortraitHelper.ClipboardPreset.BannerDecoration);
        DrawImportSetting(
            TextService.GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () =>
            {
                ImGui.TextUnformatted(ExcelService.GetRow<BannerDecoration>(PortraitHelper.ClipboardPreset.BannerDecoration)?.Name.ExtractText() ?? unknown);

                if (!isBannerDecorationUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Red))
                        TextService.Draw("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked");
                }
            },
            isBannerDecorationUnlocked
        );

        DrawImportSetting(
            TextService.GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            TextService.GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(TextService.GetAddonText(14685) ?? "Character", respectUiTheme: !IsWindow);

        var isBannerTimelineUnlocked = BannerUtils.IsBannerTimelineUnlocked(PortraitHelper.ClipboardPreset.BannerTimeline);
        DrawImportSetting(
            TextService.GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                ImGui.TextUnformatted(BannerUtils.GetBannerTimelineName(PortraitHelper.ClipboardPreset.BannerTimeline));

                if (!isBannerTimelineUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Red))
                        TextService.Draw("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked");
                }
            },
            isBannerTimelineUnlocked
        );

        DrawImportSetting(
            TextService.GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = PortraitHelper.ClipboardPreset.Expression;
                var expressionName = unknown;

                if (id == 0)
                {
                    expressionName = TextService.GetAddonText(14727) ?? "None";
                }
                else
                {
                    for (var i = 0; i < state->Expressions.SortedEntriesCount; i++)
                    {
                        var entry = state->Expressions.SortedEntries[i];
                        if (entry->RowId == id && entry->SupplementalRow != 0)
                        {
                            var bannerFacialRow = ExcelService.GetRow<BannerFacial>(entry->RowId);
                            var emoteRow = ExcelService.GetRow<Emote>(bannerFacialRow?.Emote.Row ?? 0);
                            if (emoteRow != null && emoteRow.RowId != 0)
                                expressionName = emoteRow.Name;
                            break;
                        }
                    }
                }

                ImGui.TextUnformatted(expressionName);
            }
        );

        DrawImportSetting(
            TextService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.Label"),
            ImportFlags.AnimationProgress,
            () => TextService.Draw("PortraitHelperWindows.Setting.AnimationTimestamp.ValueFormat", PortraitHelper.ClipboardPreset.AnimationProgress)
        );

        DrawImportSetting(
            TextService.GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            TextService.Translate("PortraitHelperWindows.Setting.CameraTarget.Label"),
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            TextService.Translate("PortraitHelperWindows.Setting.HeadDirection.Label"),
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            TextService.Translate("PortraitHelperWindows.Setting.EyeDirection.Label"),
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(TextService.GetAddonText(14692) ?? "Ambient Lighting", respectUiTheme: !IsWindow);

        var labelBrightness = TextService.GetAddonText(14694) ?? "Brightness";
        var labelColor = TextService.GetAddonText(7008) ?? "Color";

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

        ImGuiUtils.DrawSection(TextService.GetAddonText(14693) ?? "Directional Lighting", respectUiTheme: !IsWindow);

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
            TextService.GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            TextService.GetAddonText(14695) ?? "Horizontal Angle",
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
        using var _textColor = !isEnabled ? ImRaii.PushColor(ImGuiCol.Text, (uint)(Color.From(ImGuiCol.Text) with { A = 0.5f })) : null;
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
        if (!table.Success)
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
            label = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ColorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.ColorInput.ValueFormat", value);
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
        if (!table.Success)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
    }

    private void DrawHalfVector4(HalfVector4 vec)
    {
        using var table = ImRaii.Table("##Table", 4);
        if (!table.Success)
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
            label = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = TextService.Translate("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
        drawColumn("Z", vec.Z);
        drawColumn("W", vec.W);
    }
}
