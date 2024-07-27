using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class CastBarAetheryteNames(
    IGameInteropProvider GameInteropProvider,
    IAddonLifecycle AddonLifecycle,
    IClientState ClientState,
    ExcelService ExcelService,
    TextService TextService,
    SeStringEvaluatorService SeStringEvaluator)
    : ITweak
{
    public string InternalName => nameof(CastBarAetheryteNames);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private TeleportInfo? TeleportInfo;
    private bool IsCastingTeleport;

    private Hook<HaselActionManager.Delegates.OpenCastBar>? OpenCastBarHook;
    private Hook<Telepo.Delegates.Teleport>? TeleportHook;

    public void OnInitialize()
    {
        OpenCastBarHook = GameInteropProvider.HookFromAddress<HaselActionManager.Delegates.OpenCastBar>(
            HaselActionManager.MemberFunctionPointers.OpenCastBar,
            OpenCastBarDetour);

        TeleportHook = GameInteropProvider.HookFromAddress<Telepo.Delegates.Teleport>(
            Telepo.MemberFunctionPointers.Teleport,
            TeleportDetour);
    }

    public void OnEnable()
    {
        ClientState.TerritoryChanged += OnTerritoryChanged;

        AddonLifecycle.RegisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);

        OpenCastBarHook?.Enable();
        TeleportHook?.Enable();
    }

    public void OnDisable()
    {
        ClientState.TerritoryChanged -= OnTerritoryChanged;

        AddonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);

        OpenCastBarHook?.Disable();
        TeleportHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        OpenCastBarHook?.Dispose();
        TeleportHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnTerritoryChanged(ushort id)
    {
        Clear();
    }

    private void Clear()
    {
        IsCastingTeleport = false;
        TeleportInfo = null;
    }

    private void OnCastBarPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!IsCastingTeleport || TeleportInfo == null)
        {
            Clear();
            return;
        }

        var info = TeleportInfo.Value;

        var row = ExcelService.GetRow<Aetheryte>(info.AetheryteId);
        if (row == null)
        {
            Clear();
            return;
        }

        var placeName = true switch
        {
            _ when info.IsApartment => TextService.GetAddonText(8518),
            _ when info.IsSharedHouse => SeStringEvaluator.EvaluateFromAddon(8519, new SeStringParameter[] { (uint)info.Ward, (uint)info.Plot }).ToString(),
            _ => ExcelService.GetRow<PlaceName>(row.PlaceName.Row)?.Name?.ExtractText() ?? string.Empty,
        };

        AtkStage.Instance()->GetStringArrayData()[20]->SetValue(0, placeName, false, true, false);

        Clear();
    }

    private void OpenCastBarDetour(HaselActionManager* a1, BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7, float a8)
    {
        IsCastingTeleport = type == 1 && rowId == 5 && type2 == 5;

        OpenCastBarHook!.Original(a1, a2, type, rowId, type2, rowId2, a7, a8);
    }

    private bool TeleportDetour(Telepo* telepo, uint aetheryteID, byte subIndex)
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
