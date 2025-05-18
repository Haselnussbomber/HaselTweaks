using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Windows;

[RegisterTransient, AutoConstruct]
public unsafe partial class CompanionColorPreviewWindow : SimpleWindow
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    private Stain[] _stains;

    [AutoPostConstruct]
    private void Initialize()
    {
        LoadStains();

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override void OnLanguageChanged(string langCode)
    {
        base.OnLanguageChanged(langCode);
        LoadStains();
    }

    private void LoadStains()
    {
        _stains = [.. _excelService.GetSheet<Stain>().Skip(1).Take(85)];
    }

    public override bool DrawConditions()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
            return false;

        var battleChara = CharacterManager.Instance()->LookupBuddyByOwnerObject(localPlayer);
        if (battleChara == null)
            return false;

        var drawObject = (Demihuman*)battleChara->DrawObject;
        if (drawObject == null)
            return false;

        return true;
    }

    public override void Draw()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null) return;

        var battleChara = CharacterManager.Instance()->LookupBuddyByOwnerObject(localPlayer);
        if (battleChara == null) return;

        var drawObject = (Demihuman*)battleChara->DrawObject;
        if (drawObject == null) return;

        var stainSlot = battleChara->DrawData.EquipmentModelIds.GetPointer((int)DrawDataContainer.EquipmentSlot.Legs);
        ref var stainId = ref stainSlot->Stain0;

        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##Color", _textService.GetStainName(stainId), ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                foreach (var stain in _stains)
                {
                    using var id = ImRaii.PushId($"Stain{stain.RowId}");
                    var isCurrentStain = stain.RowId == stainId;
                    var stainName = _textService.GetStainName(stain.RowId);

                    ImGui.ColorButton(stainName + "##ColorBtn", stain.GetColor(), ImGuiColorEditFlags.NoLabel);
                    ImGui.SameLine();

                    if (isCurrentStain)
                        ImGui.SetItemDefaultFocus();

                    if (ImGui.Selectable(stainName, isCurrentStain, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        stainId = (byte)stain.RowId;
                        drawObject->FlagSlotForUpdate((uint)DrawDataContainer.EquipmentSlot.Legs, stainSlot);
                    }
                }
            }
        }

        ImGui.SameLine();
        var defaultStainId = UIState.Instance()->Buddy.CompanionInfo.CurrentColorStainId;
        using var disabled = ImRaii.Disabled(stainId == defaultStainId);
        if (ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, _textService.Translate("HaselTweaks.Config.ResetToDefault", _textService.GetStainName(defaultStainId))))
        {
            stainId = defaultStainId;
            drawObject->FlagSlotForUpdate((uint)DrawDataContainer.EquipmentSlot.Legs, stainSlot);
        }

        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (!TryGetAddon<AtkUnitBase>("Buddy", out var addon))
            return;

        Position = new(
            addon->X + 4,
            addon->Y + 3 - (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2)
        );
    }
}
