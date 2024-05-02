using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Text;
using HaselCommon.Text.Expressions;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class CastBarAetheryteNames : Tweak
{
    private TeleportInfo? TeleportInfo;
    private bool IsCastingTeleport;

    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);
    }

    public void OnCastBarPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!IsCastingTeleport || TeleportInfo == null)
            return;

        var info = TeleportInfo.Value;

        var row = GetRow<Aetheryte>(info.AetheryteId);
        if (row == null)
            return;

        var placeName = true switch
        {
            _ when info.IsAppartment => GetAddonText(8518),
            _ when info.IsSharedHouse => SeString.FromAddon(8519).Resolve([new IntegerExpression(info.Ward), new IntegerExpression(info.Plot)]).ToString(),
            _ => GetSheetText<PlaceName>(row.PlaceName.Row, "Name"),
        };

        AtkStage.GetSingleton()->GetStringArrayData()[20]->SetValue(0, placeName, false, true, false);
    }

    [AddressHook<HaselActionManager>(nameof(HaselActionManager.OpenCastBar))]
    public void ActionManager_OpenCastBar(ActionManager* a1, BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7)
    {
        IsCastingTeleport = type == 1 && rowId == 5 && type2 == 5;

        ActionManager_OpenCastBarHook.OriginalDisposeSafe(a1, a2, type, rowId, type2, rowId2, a7);
    }

    [AddressHook<Telepo>(nameof(Telepo.Teleport))]
    public bool Teleport(Telepo* telepo, uint aetheryteID, byte subIndex)
    {
        TeleportInfo = null;

        foreach (var teleportInfo in telepo->TeleportList.Span)
        {
            if (teleportInfo.AetheryteId == aetheryteID && teleportInfo.SubIndex == subIndex)
            {
                TeleportInfo = teleportInfo;
                break;
            }
        }

        return TeleportHook.OriginalDisposeSafe(telepo, aetheryteID, subIndex);
    }
}
