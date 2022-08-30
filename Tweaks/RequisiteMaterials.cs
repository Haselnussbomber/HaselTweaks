using System;
using Dalamud.Game;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe class RequisiteMaterials : Tweak
{
    public override string Name => "Requisite Materials";
    public override string Description => "Always opens the Island Sanctuarys \"Requisite Materials\" window on the \"Current & Next Season\" tab.";

    [Signature("E9 ?? ?? ?? ?? 83 EB 04")]
    private Materials_SwitchTabDelegate Materials_SwitchTab { get; init; } = null!;
    private delegate void* Materials_SwitchTabDelegate(AddonMJICraftMaterialConfirmation* addon, uint tabIndex);

    private bool CurrentNextSeasonTabSwitched;
    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = (AddonMJICraftMaterialConfirmation*)AtkUtils.GetUnitBase("MJICraftMaterialConfirmation");
        if (addon == null || !addon->AtkUnitBase.IsVisible) // no clue, but seems to help
        {
            if (CurrentNextSeasonTabSwitched) CurrentNextSeasonTabSwitched = false;
            return;
        }

        var button = (AtkComponentButton*)addon->RadioButton3; // AtkComponentRadioButton inherits from AtkComponentButton
        if (CurrentNextSeasonTabSwitched || button == null) // selected flag
        {
            return;
        }

        var isRadioButtonSelected = (button->Flags & 0x40000) != 0;
        if (isRadioButtonSelected)
        {
            return;
        }

        var isLoadingData = (*(uint*)((IntPtr)addon + 0x1AF) & 0x100000) != 0; // I guess?
        if (isLoadingData)
        {
            return;
        }

        Materials_SwitchTab(addon, 2);
        CurrentNextSeasonTabSwitched = true;
    }
}
