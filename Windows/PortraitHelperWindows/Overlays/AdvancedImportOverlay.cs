using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Memory;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static HaselTweaks.Caches.StringCache;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedImportOverlay : Overlay
{
    public AdvancedImportOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper: Advanced Import", tweak)
    {
        base.Flags &= ~ImGuiWindowFlags.NoScrollbar;
    }

    public override void OnClose()
    {
        base.OnClose();
        Tweak.CloseAdvancedImportOverlay(false);
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
        var unknown = GetAddonText(624) ?? "Unknown";

        using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Grey))
            ImGuiHelpers.SafeTextWrapped("Only the checked settings will be imported.");

        if (ImGui.Button(GetAddonText(14923) ?? "Select All"))
            Tweak.CurrentImportFlags = ImportFlags.All;

        ImGui.SameLine();

        if (ImGui.Button(GetAddonText(14924) ?? "Deselect All"))
            Tweak.CurrentImportFlags = ImportFlags.None;

        ImGui.SameLine();

        if (ImGui.Button("Import Selected Settings"))
        {
            Tweak.PresetToState(Tweak.ClipboardPreset, Tweak.CurrentImportFlags);
            Tweak.ChangeView(ViewMode.Normal);
        }

        ImGuiUtils.DrawSection(GetAddonText(14684) ?? "Design");

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

        ImGuiUtils.DrawSection(GetAddonText(14685) ?? "Character");

        DrawImportSetting(
            GetAddonText(14690) ?? "Pose",
            ImportFlags.BannerTimeline,
            () =>
            {
                var id = Tweak.ClipboardPreset.BannerTimeline;
                var poseName = GetSheetText<BannerTimeline>(id, "Name");

                if (string.IsNullOrEmpty(poseName))
                {
                    var poseRow = Service.Data.GetExcelSheet<BannerTimeline>()?.GetRow(id);
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
            "Animation Timestamp",
            ImportFlags.AnimationProgress,
            () => ImGui.TextUnformatted(Tweak.ClipboardPreset.AnimationProgress.ToString("0.000", CultureInfo.InvariantCulture))
        );

        DrawImportSetting(
            GetAddonText(5972) ?? "Camera Position",
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

        ImGuiUtils.DrawSection(GetAddonText(14692) ?? "Ambient Lighting");

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

        ImGuiUtils.DrawSection(GetAddonText(14693) ?? "Directional Lighting");

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
        ImGui.TextUnformatted(label);

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
        ImGui.TextUnformatted("R: " + r.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("G: " + g.ToString());

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("B: " + b.ToString());

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
        ImGui.TextUnformatted("X:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.X.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Y:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.Y.ToString("0.000", CultureInfo.InvariantCulture));

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
        ImGui.TextUnformatted("X:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.X.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Y:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.Y.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Z:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.Z.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("W:");
        ImGui.SameLine(24);
        ImGui.TextUnformatted(vec.W.ToString("0.000", CultureInfo.InvariantCulture));

        ImGui.TableNextColumn();
    }
}
