using Dalamud.Interface.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedImportOverlay : Overlay
{
    public AdvancedImportOverlay(PortraitHelper tweak) : base(t("PortraitHelperWindows.AdvancedImportOverlay.Title"), tweak)
    {
    }

    public override void Draw()
    {
        base.Draw();

        if (Tweak.ClipboardPreset == null)
        {
            IsOpen = false;
            return;
        }

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y * 2);

        var state = AgentBannerEditor->EditorState;
        var unknown = GetAddonText(624) ?? "Unknown";

        if (ImGui.Button(GetAddonText(14923) ?? "Select All"))
            Tweak.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(GetAddonText(14924) ?? "Deselect All"))
            Tweak.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button(t("PortraitHelperWindows.AdvancedImportOverlay.ImportSelectedSettingsButton.Label")))
        {
            Tweak.PresetToState(Tweak.ClipboardPreset, Tweak.CurrentImportFlags);
            Tweak.CloseWindows();
        }

        ImGuiUtils.DrawSection(GetAddonText(14684) ?? "Design", RespectUiTheme: !IsWindow);

        DrawImportSetting(
            GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () => ImGui.TextUnformatted(GetSheetText<BannerBg>(Tweak.ClipboardPreset.BannerBg, "Name") ?? unknown)
        );

        DrawImportSetting(
            GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () => ImGui.TextUnformatted(GetSheetText<BannerFrame>(Tweak.ClipboardPreset.BannerFrame, "Name") ?? unknown)
        );

        DrawImportSetting(
            GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () => ImGui.TextUnformatted(GetSheetText<BannerDecoration>(Tweak.ClipboardPreset.BannerDecoration, "Name") ?? unknown)
        );

        DrawImportSetting(
            GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(GetAddonText(14685) ?? "Character", RespectUiTheme: !IsWindow);

        DrawImportSetting(
            GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                var id = Tweak.ClipboardPreset.BannerTimeline;
                var poseName = GetSheetText<BannerTimeline>(id, "Name");

                if (string.IsNullOrEmpty(poseName))
                {
                    var poseRow = GetRow<BannerTimeline>(id);
                    if (poseRow != null)
                    {
                        switch (poseRow.Type)
                        {
                            case 2:
                                poseName = GetSheetText<Lumina.Excel.GeneratedSheets.Action>(poseRow.AdditionalData, "Name");
                                break;

                            case 10:
                            case 11:
                                poseName = GetSheetText<Emote>(poseRow.AdditionalData, "Name");
                                break;

                            case 20:
                                // TODO
                                break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(poseName))
                    poseName = unknown;

                ImGui.TextUnformatted(poseName);
            }
        );

        DrawImportSetting(
            GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = Tweak.ClipboardPreset.Expression;
                var expressionName = unknown;

                if (id == 0)
                {
                    expressionName = GetAddonText(14727) ?? "None";
                }
                else
                {
                    for (var i = 0; i < state->ExpressionItemsCount; i++)
                    {
                        var entry = state->ExpressionItems[i];
                        if (entry->Id == id && entry->Data != 0)
                        {
                            expressionName = MemoryHelper.ReadSeStringNullTerminated(entry->Data + 0x28).TextValue;
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
            () => ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.AnimationTimestamp.ValueFormat", Tweak.ClipboardPreset.AnimationProgress))
        );

        DrawImportSetting(
            GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(Tweak.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.CameraTarget.Label"),
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(Tweak.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.HeadDirection.Label"),
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(Tweak.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            t("PortraitHelperWindows.Setting.EyeDirection.Label"),
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(Tweak.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(GetAddonText(14692) ?? "Ambient Lighting", RespectUiTheme: !IsWindow);

        var labelBrightness = GetAddonText(14694) ?? "Brightness";
        var labelColor = GetAddonText(7008) ?? "Color";

        DrawImportSetting(
            labelBrightness,
            ImportFlags.AmbientLightingBrightness,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.AmbientLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.AmbientLightingColor,
            () => DrawColor(
                Tweak.ClipboardPreset.AmbientLightingColorRed,
                Tweak.ClipboardPreset.AmbientLightingColorGreen,
                Tweak.ClipboardPreset.AmbientLightingColorBlue
            )
        );

        ImGuiUtils.DrawSection(GetAddonText(14693) ?? "Directional Lighting", RespectUiTheme: !IsWindow);

        DrawImportSetting(
            labelBrightness,
            ImportFlags.DirectionalLightingBrightness,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.DirectionalLightingBrightness.ToString())
        );

        DrawImportSetting(
            labelColor,
            ImportFlags.DirectionalLightingColor,
            () => DrawColor(
                Tweak.ClipboardPreset.DirectionalLightingColorRed,
                Tweak.ClipboardPreset.DirectionalLightingColorGreen,
                Tweak.ClipboardPreset.DirectionalLightingColorBlue
            )
        );

        DrawImportSetting(
            GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            GetAddonText(14695) ?? "Horizontal Angle",
            ImportFlags.DirectionalLightingHorizontalAngle,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.DirectionalLightingHorizontalAngle.ToString())
        );

        if (IsWindow)
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
    }

    private void DrawImportSetting(string label, ImportFlags flag, System.Action drawFn)
    {
        using var id = ImRaii.PushId(flag.ToString());

        ImGui.Columns(2, "##Columns", false);

        var isEnabled = Tweak.CurrentImportFlags.HasFlag(flag);
        if (ImGui.Checkbox("##Checkbox", ref isEnabled))
        {
            if (isEnabled)
                Tweak.CurrentImportFlags |= flag;
            else
                Tweak.CurrentImportFlags &= ~flag;
        }

        if (!isEnabled)
            ImGui.BeginDisabled();

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.TextUnformatted(label);

        ImGui.NextColumn();
        drawFn();

        if (!isEnabled)
            ImGui.EndDisabled();

        ImGui.Columns(1);
    }

    private static void DrawColor(byte r, byte g, byte b)
    {
        var vec = new System.Numerics.Vector3(r / 255f, g / 255f, b / 255f);
        using var table = ImRaii.Table("##Table", 4);
        if (!table.Success)
            return;

        var scale = ImGui.GetIO().FontGlobalScale;
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

        var scale = ImGui.GetIO().FontGlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        void drawColumn(string label, Half value)
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

        var scale = ImGui.GetIO().FontGlobalScale;
        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
        ImGui.TableSetupColumn("W", ImGuiTableColumnFlags.WidthFixed, 50 * scale);

        ImGui.TableNextRow();

        void drawColumn(string label, Half value)
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
