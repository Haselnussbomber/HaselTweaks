using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedImportOverlay(ILogger<PortraitHelper> Logger, WindowManager windowManager)
    : Overlay(windowManager, t("PortraitHelperWindows.AdvancedImportOverlay.Title"))
{
    public MenuBar MenuBar { get; internal set; } = null!;

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

        var state = GetAgent<AgentBannerEditor>()->EditorState;
        var unknown = GetAddonText(624) ?? "Unknown";

        if (ImGui.Button(GetAddonText(14923) ?? "Select All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(GetAddonText(14924) ?? "Deselect All"))
            PortraitHelper.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button(t("PortraitHelperWindows.AdvancedImportOverlay.ImportSelectedSettingsButton.Label")))
        {
            PortraitHelper.ClipboardPreset.ToState(Logger, PortraitHelper.CurrentImportFlags);
            MenuBar.CloseOverlays();
        }

        ImGuiUtils.DrawSection(GetAddonText(14684) ?? "Design", RespectUiTheme: !IsWindow);

        var isBannerBgUnlocked = PortraitHelper.IsBannerBgUnlocked(PortraitHelper.ClipboardPreset.BannerBg);
        DrawImportSetting(
            GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () =>
            {
                ImGui.TextUnformatted(GetSheetText<BannerBg>(PortraitHelper.ClipboardPreset.BannerBg, "Name") ?? unknown);

                if (!isBannerBgUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                        ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerBgUnlocked
        );

        var isBannerFrameUnlocked = PortraitHelper.IsBannerFrameUnlocked(PortraitHelper.ClipboardPreset.BannerFrame);
        DrawImportSetting(
            GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () =>
            {
                ImGui.TextUnformatted(GetSheetText<BannerFrame>(PortraitHelper.ClipboardPreset.BannerFrame, "Name") ?? unknown);

                if (!isBannerFrameUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                        ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerFrameUnlocked
        );

        var isBannerDecorationUnlocked = PortraitHelper.IsBannerDecorationUnlocked(PortraitHelper.ClipboardPreset.BannerDecoration);
        DrawImportSetting(
            GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () =>
            {
                ImGui.TextUnformatted(GetSheetText<BannerDecoration>(PortraitHelper.ClipboardPreset.BannerDecoration, "Name") ?? unknown);

                if (!isBannerDecorationUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                        ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerDecorationUnlocked
        );

        DrawImportSetting(
            GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(GetAddonText(14685) ?? "Character", RespectUiTheme: !IsWindow);

        var isBannerTimelineUnlocked = PortraitHelper.IsBannerTimelineUnlocked(PortraitHelper.ClipboardPreset.BannerTimeline);
        DrawImportSetting(
            GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                ImGui.TextUnformatted(PortraitHelper.GetBannerTimelineName(PortraitHelper.ClipboardPreset.BannerTimeline));

                if (!isBannerTimelineUnlocked)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                        ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedImportOverlay.NotUnlocked"));
                }
            },
            isBannerTimelineUnlocked
        );

        DrawImportSetting(
            GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = PortraitHelper.ClipboardPreset.Expression;
                var expressionName = unknown;

                if (id == 0)
                {
                    expressionName = GetAddonText(14727) ?? "None";
                }
                else
                {
                    for (var i = 0; i < state->Expressions.SortedEntriesCount; i++)
                    {
                        var entry = state->Expressions.SortedEntries[i];
                        if (entry->RowId == id && entry->SupplementalRow != 0)
                        {
                            var bannerFacialRow = GetRow<BannerFacial>(entry->RowId);
                            var emoteRow = GetRow<Emote>(bannerFacialRow?.Emote.Row ?? 0);
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
            t("PortraitHelperWindows.Setting.AnimationTimestamp.Label"),
            ImportFlags.AnimationProgress,
            () => ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.AnimationTimestamp.ValueFormat", PortraitHelper.ClipboardPreset.AnimationProgress))
        );

        DrawImportSetting(
            GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.CameraTarget.Label"),
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(PortraitHelper.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.HeadDirection.Label"),
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.EyeDirection.Label"),
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(PortraitHelper.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(GetAddonText(14692) ?? "Ambient Lighting", RespectUiTheme: !IsWindow);

        var labelBrightness = GetAddonText(14694) ?? "Brightness";
        var labelColor = GetAddonText(7008) ?? "Color";

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

        ImGuiUtils.DrawSection(GetAddonText(14693) ?? "Directional Lighting", RespectUiTheme: !IsWindow);

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
            GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.TextUnformatted(PortraitHelper.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            GetAddonText(14695) ?? "Horizontal Angle",
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
        using var _textColor = !isEnabled ? ImRaii.PushColor(ImGuiCol.Text, (uint)HaselColor.From(ImGuiCol.Text).WithAlpha(0.5f)) : null;
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

    private static void DrawColor(byte r, byte g, byte b)
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

        static void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = t("PortraitHelperWindows.AdvancedImportOverlay.ColorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = t("PortraitHelperWindows.AdvancedImportOverlay.ColorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("R", r);
        drawColumn("G", g);
        drawColumn("B", b);
    }

    private static void DrawHalfVector2(HalfVector2 vec)
    {
        using var table = ImRaii.Table("##Table", 2);
        if (!table.Success)
            return;

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        static void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = t("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = t("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
    }

    private static void DrawHalfVector4(HalfVector4 vec)
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

        static void drawColumn(string label, Half value)
        {
            ImGui.TableNextColumn();
            label = t("PortraitHelperWindows.AdvancedImportOverlay.VectorInput." + label);
            var labelWidth = ImGui.CalcTextSize(label).X;
            ImGui.TextUnformatted(label);

            var valueStr = t("PortraitHelperWindows.AdvancedImportOverlay.VectorInput.ValueFormat", value);
            ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - labelWidth - ImGui.CalcTextSize(valueStr).X);
            ImGui.TextUnformatted(valueStr);
        }

        drawColumn("X", vec.X);
        drawColumn("Y", vec.Y);
        drawColumn("Z", vec.Z);
        drawColumn("W", vec.W);
    }
}
