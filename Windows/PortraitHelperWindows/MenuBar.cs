using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
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
            PortraitName = StringUtils.GetAddonText(14761) ?? "Adventurer Plate";
        }
        else if (AgentBannerEditor->EditorState->GearsetId > -1)
        {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(AgentBannerEditor->EditorState->GearsetId);
            if (gearset != null)
            {
                PortraitName = $"{StringUtils.GetAddonText(756) ?? "Gear Set"} #{gearset->ID + 1}: {MemoryHelper.ReadString((nint)gearset->Name, 0x2F)}";
            }
        }
    }

    public override void Draw()
    {
        if (InitialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            ImGui.Text("Initializing...");
            return;
        }

        using var id = ImRaii.PushId("##HaselTweaks_PortraitHelper");

        if (!AgentBannerEditor->EditorState->HasDataChanged)
        {
            ImGuiUtils.IconButtonDisabled("Reset", FontAwesomeIcon.Undo, StringUtils.GetAddonText(4830) ?? "Reset");
        }
        else if (ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, StringUtils.GetAddonText(4830) ?? "Reset"))
        {
            Tweak.PresetToState(InitialPreset, ImportFlags.All);
            AgentBannerEditor->EditorState->SetHasChanged(false);
        }

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("Copy", FontAwesomeIcon.Copy, "Export to Clipboard")) // StringUtils.GetAddonText(100) ?? "Copy"
        {
            Tweak.PresetToClipboard(Tweak.StateToPreset());
        }

        ImGui.SameLine();
        if (Tweak.ClipboardPreset == null)
        {
            ImGuiUtils.IconButtonDisabled("Paste", FontAwesomeIcon.Paste, "Import from Clipboard"); // StringUtils.GetAddonText(101) ?? "Paste"
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
            ImGuiUtils.IconButtonDisabled("ViewModeAdvancedImport", FontAwesomeIcon.FilePen, "Toggle Advanced Import Mode");
        }
        else if (Tweak.OverlayViewMode == ViewMode.AdvancedImport)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, "Toggle Advanced Import Mode"))
            {
                Tweak.ChangeView(ViewMode.Normal);
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FilePen, "Toggle Advanced Import Mode"))
        {
            Tweak.ChangeView(ViewMode.AdvancedImport);
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

        if (!string.IsNullOrEmpty(PortraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.Text(PortraitName);
        }

        Position = new(
            AddonBannerEditor->AtkUnitBase.X + 4,
            AddonBannerEditor->AtkUnitBase.Y + 3 - 40
        );

        Size = new(
            AddonBannerEditor->AtkUnitBase.GetScaledWidth(true) - 8,
            40
        );

        SaveAsPresetDialog.Draw();
    }

    public override void OnClose()
    {
        SaveAsPresetDialog.Hide();
    }
}
