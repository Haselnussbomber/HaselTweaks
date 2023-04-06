using System.Globalization;
using System.Numerics;
using Dalamud.Interface.Raii;
using Dalamud.Memory;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedImportOverlay : Overlay
{
    public AdvancedImportOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper AdvancedImport", tweak)
    {
        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.NoMove;
        base.IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        if (Tweak.ClipboardPreset == null)
        {
            Tweak.ChangeView(ViewMode.Normal);
            return;
        }

        var state = AgentBannerEditor->EditorState;
        var unknown = StringUtils.GetAddonText(624) ?? "Unknown";

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiUtils.ColorGrey))
            ImGui.TextWrapped("Only the checked settings will be imported.");

        if (ImGui.Button(StringUtils.GetAddonText(14923) ?? "Select All"))
            Tweak.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(StringUtils.GetAddonText(14924) ?? "Deselect All"))
            Tweak.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button("Import Selected Settings"))
        {
            Tweak.PresetToState(Tweak.ClipboardPreset, Tweak.CurrentImportFlags);
            Tweak.ChangeView(ViewMode.Normal);
        }

        ImGuiUtils.DrawSection(StringUtils.GetAddonText(14684) ?? "Design");

        DrawImportSetting(
            StringUtils.GetAddonText(14687) ?? "Background",
            ImportFlags.BannerBg,
            () => ImGui.Text(StringUtils.GetSheetText<BannerBg>(Tweak.ClipboardPreset.BannerBg, "Name") ?? unknown)
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14688) ?? "Frame",
            ImportFlags.BannerFrame,
            () => ImGui.Text(StringUtils.GetSheetText<BannerFrame>(Tweak.ClipboardPreset.BannerFrame, "Name") ?? unknown)
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14689) ?? "Accent",
            ImportFlags.BannerDecoration,
            () => ImGui.Text(StringUtils.GetSheetText<BannerDecoration>(Tweak.ClipboardPreset.BannerDecoration, "Name") ?? unknown)
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14711) ?? "Zoom",
            ImportFlags.CameraZoom,
            () => ImGui.Text(Tweak.ClipboardPreset.CameraZoom.ToString())
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14712) ?? "Rotation",
            ImportFlags.ImageRotation,
            () => ImGui.Text(Tweak.ClipboardPreset.ImageRotation.ToString())
        );

        ImGuiUtils.DrawSection(StringUtils.GetAddonText(14685) ?? "Character");

        DrawImportSetting(
            StringUtils.GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                var id = Tweak.ClipboardPreset.BannerTimeline;
                var poseName = StringUtils.GetSheetText<BannerTimeline>(id, "Name");

                if (string.IsNullOrEmpty(poseName))
                {
                    var poseRow = Service.Data.GetExcelSheet<BannerTimeline>()?.GetRow(id);
                    if (poseRow != null)
                    {
                        switch (poseRow.Type)
                        {
                            case 2:
                                poseName = StringUtils.GetSheetText<Lumina.Excel.GeneratedSheets.Action>(poseRow.AdditionalData, "Name");
                                break;

                            case 10:
                            case 11:
                                poseName = StringUtils.GetSheetText<Emote>(poseRow.AdditionalData, "Name");
                                break;

                            case 20:
                                // TODO
                                break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(poseName))
                    poseName = unknown;

                ImGui.Text(poseName);
            }
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14691) ?? "Expression",
            ImportFlags.Expression,
            () =>
            {
                var id = Tweak.ClipboardPreset.Expression;
                var expressionName = unknown;

                if (id == 0)
                {
                    expressionName = StringUtils.GetAddonText(14727) ?? "None";
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

                ImGui.Text(expressionName);
            }
        );

        DrawImportSetting(
            "Animation Timestamp",
            ImportFlags.AnimationProgress,
            () => ImGui.Text(Tweak.ClipboardPreset.AnimationProgress.ToString("0.000", CultureInfo.InvariantCulture))
        );

        DrawImportSetting(
            StringUtils.GetAddonText(5972) ?? "Camera Position",
            ImportFlags.CameraPosition,
            () => DrawHalfVector4(Tweak.ClipboardPreset.CameraPosition)
        );

        DrawImportSetting(
            "Camera Target",
            ImportFlags.CameraTarget,
            () => DrawHalfVector4(Tweak.ClipboardPreset.CameraTarget)
        );

        DrawImportSetting(
            "Head Direction",
            ImportFlags.HeadDirection,
            () => DrawHalfVector2(Tweak.ClipboardPreset.HeadDirection)
        );

        DrawImportSetting(
            "Eye Direction",
            ImportFlags.EyeDirection,
            () => DrawHalfVector2(Tweak.ClipboardPreset.EyeDirection)
        );

        ImGuiUtils.DrawSection(StringUtils.GetAddonText(14692) ?? "Ambient Lighting");

        var labelBrightness = StringUtils.GetAddonText(14694) ?? "Brightness";
        var labelColor = StringUtils.GetAddonText(7008) ?? "Color";

        DrawImportSetting(
            labelBrightness,
            ImportFlags.AmbientLightingBrightness,
            () => ImGui.Text(Tweak.ClipboardPreset.AmbientLightingBrightness.ToString())
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

        ImGuiUtils.DrawSection(StringUtils.GetAddonText(14693) ?? "Directional Lighting");

        DrawImportSetting(
            labelBrightness,
            ImportFlags.DirectionalLightingBrightness,
            () => ImGui.Text(Tweak.ClipboardPreset.DirectionalLightingBrightness.ToString())
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
            StringUtils.GetAddonText(14696) ?? "Vertical Angle",
            ImportFlags.DirectionalLightingVerticalAngle,
            () => ImGui.Text(Tweak.ClipboardPreset.DirectionalLightingVerticalAngle.ToString())
        );

        DrawImportSetting(
            StringUtils.GetAddonText(14695) ?? "Horizontal Angle",
            ImportFlags.DirectionalLightingHorizontalAngle,
            () => ImGui.Text(Tweak.ClipboardPreset.DirectionalLightingHorizontalAngle.ToString())
        );
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

        var style = ImGui.GetStyle();

        ImGui.SameLine(0, style.ItemInnerSpacing.X);
        ImGui.Text(label);

        ImGui.NextColumn();
        drawFn();

        if (!isEnabled)
            ImGui.EndDisabled();

        ImGui.Columns(1);
    }

    private static void DrawColor(byte r, byte g, byte b)
    {
        var vec = new Vector3(r / 255f, g / 255f, b / 255f);
        using var table = ImRaii.Table("##Table", 5);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Preview", ImGuiTableColumnFlags.WidthFixed, 26);
        ImGui.TableSetupColumn("R", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("G", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("B", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("spacing", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.ColorEdit3("##ColorEdit3", ref vec, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoInputs);

        ImGui.TableNextColumn();
        ImGui.Text("R: " + r.ToString());

        ImGui.TableNextColumn();
        ImGui.Text("G: " + g.ToString());

        ImGui.TableNextColumn();
        ImGui.Text("B: " + b.ToString());

        ImGui.TableNextColumn();
    }

    private static void DrawHalfVector2(HalfVector2 vec)
    {
        using var table = ImRaii.Table("##Table", 3);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("spacing", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.Text("X:");
        ImGui.SameLine(24);
        ImGui.Text(vec.X.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.Text("Y:");
        ImGui.SameLine(24);
        ImGui.Text(vec.Y.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
    }

    private static void DrawHalfVector4(HalfVector4 vec)
    {
        using var table = ImRaii.Table("##Table", 5);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("W", ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn("spacing", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.Text("X:");
        ImGui.SameLine(24);
        ImGui.Text(vec.X.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.Text("Y:");
        ImGui.SameLine(24);
        ImGui.Text(vec.Y.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.Text("Z:");
        ImGui.SameLine(24);
        ImGui.Text(vec.Z.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.Text("W:");
        ImGui.SameLine(24);
        ImGui.Text(vec.W.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
    }
}
