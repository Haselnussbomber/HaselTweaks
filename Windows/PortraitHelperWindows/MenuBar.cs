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
    private PortraitPreset? _initialPreset;
    private string _portraitName = string.Empty;

    private readonly CreatePresetDialog _saveAsPresetDialog = new();

    public MenuBar(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper MenuBar")
    {
        Tweak = tweak;

        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.NoMove;
        base.DisableWindowSounds = true;
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
        if (_initialPreset != null || !AgentBannerEditor->EditorState->CharaView->CharacterLoaded)
            return;

        _initialPreset = Tweak.StateToPreset();

        if (AgentBannerEditor->EditorState->OpenType == EditorOpenType.AdventurerPlate)
        {
            _portraitName = StringCache.GetAddonText(14761) ?? "Adventurer Plate";
        }
        else if (AgentBannerEditor->EditorState->GearsetId > -1)
        {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(AgentBannerEditor->EditorState->GearsetId);
            if (gearset != null)
            {
                _portraitName = $"{StringCache.GetAddonText(756) ?? "Gear Set"} #{gearset->ID + 1}: {MemoryHelper.ReadString((nint)gearset->Name, 0x2F)}";
            }
        }
    }

    public override void Draw()
    {
        if (_initialPreset == null)
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
            Tweak.PresetToState(_initialPreset, ImportFlags.All);
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
                Tweak.CloseWindows();
            }
        }

        ImGui.SameLine();
        if (Tweak.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode", disabled: true);
        }
        else if (Tweak.AdvancedImportOverlay != null && Tweak.AdvancedImportOverlay.IsOpen)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode"))
            {
                Tweak.AdvancedImportOverlay.IsOpen = false;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, "Toggle Advanced Import Mode"))
        {
            if (Tweak.AdvancedImportOverlay == null)
                Plugin.WindowSystem.AddWindow(Tweak.AdvancedImportOverlay = new(Tweak));

            Tweak.CloseWindows();
            Tweak.AdvancedImportOverlay.IsOpen = true;
        }

        ImGui.SameLine();
        if (Tweak.AdvancedEditOverlay != null && Tweak.AdvancedEditOverlay.IsOpen)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, "Toggle Advanced Edit Mode"))
            {
                Tweak.AdvancedEditOverlay.IsOpen = false;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedEdit", FontAwesomeIcon.FilePen, "Toggle Advanced Edit Mode"))
        {
            if (Tweak.AdvancedEditOverlay == null)
                Plugin.WindowSystem.AddWindow(Tweak.AdvancedEditOverlay = new(Tweak));

            Tweak.CloseWindows();
            Tweak.AdvancedEditOverlay.IsOpen = true;
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("SaveAsPreset", FontAwesomeIcon.Download, "Save as Preset"))
        {
            _saveAsPresetDialog.Open(_portraitName, Tweak.StateToPreset(), Tweak.GetCurrentCharaViewImage());
        }

        ImGui.SameLine();
        if (Tweak.PresetBrowserOverlay != null && Tweak.PresetBrowserOverlay.IsOpen)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal2", FontAwesomeIcon.List, "Toggle Preset Browser"))
            {
                Tweak.PresetBrowserOverlay.IsOpen = false;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModePresetBrowser", FontAwesomeIcon.List, "Toggle Preset Browser"))
        {
            if (Tweak.PresetBrowserOverlay == null)
                Plugin.WindowSystem.AddWindow(Tweak.PresetBrowserOverlay = new(Tweak));

            Tweak.CloseWindows();
            Tweak.PresetBrowserOverlay.IsOpen = true;
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (Tweak.AlignmentToolSettingsOverlay != null && Tweak.AlignmentToolSettingsOverlay.IsOpen)
        {
            using var color1 = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942);
            using var color2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xFFB06C2B);
            using var color3 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ToggleAlignmentToolOff", FontAwesomeIcon.Hashtag, "Toggle Alignment Tool\nShift: Close Settings"))
            {
                if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                {
                    Tweak.AlignmentToolSettingsOverlay.IsOpen = false;
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
                if (Tweak.AlignmentToolSettingsOverlay == null)
                    Plugin.WindowSystem.AddWindow(Tweak.AlignmentToolSettingsOverlay = new(Tweak));

                Tweak.CloseWindows();
                Tweak.AlignmentToolSettingsOverlay.IsOpen = true;
            }
            else
            {
                Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                Plugin.Config.Save();
            }
        }

        if (!string.IsNullOrEmpty(_portraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.TextUnformatted(_portraitName);
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

        _saveAsPresetDialog.Draw();
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

            if (!(ImGui.GetIO().FontGlobalScale <= 1 && ((Tweak.AdvancedImportOverlay != null && Tweak.AdvancedImportOverlay.IsOpen) || (Tweak.PresetBrowserOverlay != null && Tweak.PresetBrowserOverlay.IsOpen))))
            {
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
    }

    public override void OnClose()
    {
        _saveAsPresetDialog.Hide();
    }
}
