using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Text;
using HaselCommon.Text.Expressions;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class CastBarAetheryteNames : Tweak
{
    private TeleportInfo? TeleportInfo;
    private bool IsCastingTeleport;

    private AddressHook<HaselActionManager.Delegates.OpenCastBar>? OpenCastBarHook;
    private AddressHook<Telepo.Delegates.Teleport>? TeleportHook;

    public override void SetupHooks()
    {
        OpenCastBarHook = new(HaselActionManager.MemberFunctionPointers.OpenCastBar, OpenCastBarDetour);
        TeleportHook = new(Telepo.MemberFunctionPointers.Teleport, TeleportDetour);
    }

    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);
    }

    public override void OnTerritoryChanged(ushort id)
    {
        Clear();
    }

    public void Clear()
    {
        IsCastingTeleport = false;
        TeleportInfo = null;
    }

    public void OnCastBarPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!IsCastingTeleport || TeleportInfo == null)
        {
            Clear();
            return;
        }

        var info = TeleportInfo.Value;

        var row = GetRow<Aetheryte>(info.AetheryteId);
        if (row == null)
        {
            Clear();
            return;
        }

        var placeName = true switch
        {
            _ when info.IsApartment => GetAddonText(8518),
            _ when info.IsSharedHouse => SeString.FromAddon(8519).Resolve([new IntegerExpression(info.Ward), new IntegerExpression(info.Plot)]).ToString(),
            _ => GetSheetText<PlaceName>(row.PlaceName.Row, "Name"),
        };

        AtkStage.Instance()->GetStringArrayData()[20]->SetValue(0, placeName, false, true, false);

        Clear();
    }

    public void OpenCastBarDetour(HaselActionManager* a1, BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7)
    {
        IsCastingTeleport = type == 1 && rowId == 5 && type2 == 5;

        OpenCastBarHook!.Original(a1, a2, type, rowId, type2, rowId2, a7);
    }

    public bool TeleportDetour(Telepo* telepo, uint aetheryteID, byte subIndex)
    {
        TeleportInfo = null;

        foreach (var teleportInfo in telepo->TeleportList)
        {
            if (teleportInfo.AetheryteId == aetheryteID && teleportInfo.SubIndex == subIndex)
            {
                TeleportInfo = teleportInfo;
                break;
            }
        }

        return TeleportHook!.Original(telepo, aetheryteID, subIndex);
    }
}
