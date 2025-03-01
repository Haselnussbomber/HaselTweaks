using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CastBarAetheryteNames : ITweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IClientState _clientState;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly SeStringEvaluatorService _seStringEvaluator;

    private Hook<HaselActionManager.Delegates.OpenCastBar>? _openCastBarHook;
    private Hook<Telepo.Delegates.Teleport>? _teleportHook;

    private TeleportInfo? _teleportInfo;
    private bool _isCastingTeleport;

    public string InternalName => nameof(CastBarAetheryteNames);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _openCastBarHook = _gameInteropProvider.HookFromAddress<HaselActionManager.Delegates.OpenCastBar>(
            HaselActionManager.MemberFunctionPointers.OpenCastBar,
            OpenCastBarDetour);

        _teleportHook = _gameInteropProvider.HookFromAddress<Telepo.Delegates.Teleport>(
            Telepo.MemberFunctionPointers.Teleport,
            TeleportDetour);
    }

    public void OnEnable()
    {
        _clientState.TerritoryChanged += OnTerritoryChanged;

        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);

        _openCastBarHook?.Enable();
        _teleportHook?.Enable();
    }

    public void OnDisable()
    {
        _clientState.TerritoryChanged -= OnTerritoryChanged;

        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "_CastBar", OnCastBarPreRefresh);

        _openCastBarHook?.Disable();
        _teleportHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _openCastBarHook?.Dispose();
        _teleportHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnTerritoryChanged(ushort id)
    {
        Clear();
    }

    private void Clear()
    {
        _isCastingTeleport = false;
        _teleportInfo = null;
    }

    private void OnCastBarPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_isCastingTeleport || _teleportInfo == null)
        {
            Clear();
            return;
        }

        var info = _teleportInfo.Value;

        if (!_excelService.TryGetRow<Aetheryte>(info.AetheryteId, out var row))
        {
            Clear();
            return;
        }

        var placeName = true switch
        {
            _ when info.IsApartment => _textService.GetAddonText(8518),
            _ when info.IsSharedHouse => _seStringEvaluator.EvaluateFromAddon(8519, [(uint)info.Ward, (uint)info.Plot]).ToString(),
            _ when row.PlaceName.IsValid => row.PlaceName.Value.Name.ExtractText(),
            _ => string.Empty
        };

        AtkStage.Instance()->GetStringArrayData(StringArrayType.CastBar)->SetValue(0, placeName, false, true, false);

        Clear();
    }

    private void OpenCastBarDetour(HaselActionManager* a1, BattleChara* a2, int type, uint rowId, uint type2, int rowId2, float a7, float a8)
    {
        _isCastingTeleport = type == 1 && rowId == 5 && type2 == 5;

        _openCastBarHook!.Original(a1, a2, type, rowId, type2, rowId2, a7, a8);
    }

    private bool TeleportDetour(Telepo* telepo, uint aetheryteID, byte subIndex)
    {
        _teleportInfo = null;

        foreach (var teleportInfo in telepo->TeleportList)
        {
            if (teleportInfo.AetheryteId == aetheryteID && teleportInfo.SubIndex == subIndex)
            {
                _teleportInfo = teleportInfo;
                break;
            }
        }

        return _teleportHook!.Original(telepo, aetheryteID, subIndex);
    }
}
