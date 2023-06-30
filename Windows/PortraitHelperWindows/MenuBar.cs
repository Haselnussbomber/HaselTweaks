using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Caches;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using ImGuiNET;
using static HaselTweaks.Structs.AgentBannerEditorState;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class MenuBar : Window, IDisposable
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private PortraitHelper Tweak { get; init; }
    private AgentBannerEditor* AgentBannerEditor => Tweak.AgentBannerEditor;
    private AddonBannerEditor* AddonBannerEditor => Tweak.AddonBannerEditor;

    // Menu Bar
    private PortraitPreset? InitialPreset;
    private string PortraitName = string.Empty;

    private readonly CreatePresetDialog SaveAsPresetDialog = new();

    public MenuBar(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper MenuBar")
    {
        Tweak = tweak;

        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.NoMove;
        base.RespectCloseHotkey = false;
        base.IsOpen = true;
    }

    public void Dispose()
    {
        base.IsOpen = false;
    }

    public override bool DrawConditions()
        => AgentBannerEditor->EditorState != null;

    public override void PreDraw()
    {
        if (InitialPreset != null || !AgentBannerEditor->EditorState->CharaView->CharacterLoaded)
            return;

        InitialPreset = Tweak.StateToPreset();

        if (AgentBannerEditor->EditorState->OpenType == EditorOpenType.AdventurerPlate)
        {
            PortraitName = StringCache.GetAddonText(14761) ?? "Adventurer Plate";
        }
        else if (AgentBannerEditor->EditorState->GearsetId > -1)
        {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(AgentBannerEditor->EditorState->GearsetId);
            if (gearset != null)
            {
                PortraitName = $"{StringCache.GetAddonText(756) ?? "Gear Set"} #{gearset->ID + 1}: {MemoryHelper.ReadString((nint)gearset->Name, 0x2F)}";
            }
        }
    }

    public override void Draw()
    {
        if (InitialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            ImGui.TextUnformatted("Initializing...");
            return;
        }

        using var id = ImRaii.PushId("##HaselTweaks_PortraitHelper");

        if (!AgentBannerEditor->EditorState->HasDataChanged)
        {
            ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, StringCache.GetAddonText(4830) ?? "Reset", disabled: true);
        }
        else if (ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, StringCache.GetAddonText(4830) ?? "Reset"))
        {
            Tweak.PresetToState(InitialPreset, ImportFlags.All);
            AgentBannerEditor->EditorState->SetHasChanged(false);
        }

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("Copy", FontAwesomeIcon.Copy, "Export to Clipboard")) // GetAddonText(100) ?? "Copy"
        {
            Tweak.PresetToClipboard(Tweak.StateToPreset());
        }

        ImGui.SameLine();
        if (Tweak.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, "Import from Clipboard", disabled: true); // GetAddonText(101) ?? "Paste"
        }
        else
        {
            if (ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, "Import from Clipboard (All Settings)"))
            {
                Tweak.PresetToState(Tweak.ClipboardPreset, ImportFlags.All);
                Tweak.ChangeView(ViewMode.Normal);
            }
        }

        ImGui.SameLine();
        if (Tweak.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode", disabled: true);
        }
        else if (Tweak.OverlayViewMode == ViewMode.AdvancedImport)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode"))
            {
                Tweak.ChangeView(ViewMode.Normal);
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode"))
        {
            Tweak.ChangeView(ViewMode.AdvancedImport);
        }

        ImGui.SameLine();
        if (Tweak.OverlayViewMode == ViewMode.AdvancedEdit)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, "Toggle Advanced Edit Mode"))
            {
                Tweak.ChangeView(ViewMode.Normal);
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedEdit", FontAwesomeIcon.FilePen, "Toggle Advanced Edit Mode"))
        {
            Tweak.ChangeView(ViewMode.AdvancedEdit);
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("SaveAsPreset", FontAwesomeIcon.Download, "Save as Preset"))
        {
            SaveAsPresetDialog.Open(PortraitName, Tweak.StateToPreset(), Tweak.GetCurrentCharaViewImage());
        }

        ImGui.SameLine();
        if (Tweak.OverlayViewMode == ViewMode.PresetBrowser)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal2", FontAwesomeIcon.List, "Toggle Preset Browser"))
            {
                Tweak.ChangeView(ViewMode.Normal);
            }
        }
        else if (ImGuiUtils.IconButton("ViewModePresetBrowser", FontAwesomeIcon.List, "Toggle Preset Browser"))
        {
            Tweak.ChangeView(ViewMode.PresetBrowser);
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (Tweak.OverlayViewMode == ViewMode.AlignmentToolSettings)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ToggleAlignmentToolOff", FontAwesomeIcon.Hashtag, "Toggle Alignment Tool\nShift: Close Settings"))
            {
                if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                {
                    Tweak.ChangeView(ViewMode.Normal);
                }
                else
                {
                    Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                    Plugin.Config.Save();
                }
            }
        }
        else if (ImGuiUtils.IconButton("ToggleAlignmentToolOn", FontAwesomeIcon.Hashtag, "Toggle Alignment Tool\nShift: Open Settings"))
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                Tweak.ChangeView(ViewMode.AlignmentToolSettings);
            }
            else
            {
                Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                Plugin.Config.Save();
            }
        }

        if (!string.IsNullOrEmpty(PortraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.TextUnformatted(PortraitName);
        }

        var scale = ImGui.GetIO().FontGlobalScale;
        var scaledown = 1 / scale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * scaledown;

        Position = new(
            AddonBannerEditor->AtkUnitBase.X + 4,
            AddonBannerEditor->AtkUnitBase.Y + 3 - height * scale
        );

        Size = new(
            (AddonBannerEditor->AtkUnitBase.GetScaledWidth(true) - 8) * scaledown,
            height
        );

        SaveAsPresetDialog.Draw();
    }

    public override void PostDraw()
    {
        if (IsOpen && DrawConditions() && Config.ShowAlignmentTool)
        {
            var rightPanel = GetNode((AtkUnitBase*)AddonBannerEditor, 107);
            var charaView = GetNode((AtkUnitBase*)AddonBannerEditor, 130);
            var scale = GetNodeScale(charaView);

            var position = new Vector2(
                AddonBannerEditor->AtkUnitBase.X + rightPanel->X * scale.X,
                AddonBannerEditor->AtkUnitBase.Y + rightPanel->Y * scale.Y
            );

            var size = new Vector2(
                charaView->GetWidth() * scale.X,
                charaView->GetHeight() * scale.Y
            );

            ImGui.SetNextWindowPos(position);
            ImGui.SetNextWindowSize(size);

            ImGui.Begin("AlignmentTool", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);

            var drawList = ImGui.GetWindowDrawList();

            if (Config.AlignmentToolVerticalLines > 0)
            {
                var x = size.X / (Config.AlignmentToolVerticalLines + 1);

                for (var i = 1; i <= Config.AlignmentToolVerticalLines + 1; i++)
                {
                    drawList.AddLine(
                        position + new Vector2(i * x, 0),
                        position + new Vector2(i * x, size.Y),
                        ImGui.ColorConvertFloat4ToU32(Config.AlignmentToolVerticalColor)
                    );
                }
            }

            if (Config.AlignmentToolHorizontalLines > 0)
            {
                var y = size.Y / (Config.AlignmentToolHorizontalLines + 1);

                for (var i = 1; i <= Config.AlignmentToolHorizontalLines + 1; i++)
                {
                    drawList.AddLine(
                        position + new Vector2(0, i * y),
                        position + new Vector2(size.X, i * y),
                        ImGui.ColorConvertFloat4ToU32(Config.AlignmentToolHorizontalColor)
                    );
                }
            }

            ImGui.End();
        }
    }

    public override void OnClose()
    {
        SaveAsPresetDialog.Hide();
    }
}
