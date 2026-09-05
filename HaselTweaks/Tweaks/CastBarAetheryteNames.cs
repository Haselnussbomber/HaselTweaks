using System.Threading.Tasks;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class CastBarAetheryteNames : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IClientState _clientState;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly IFramework _framework;
    private readonly TeleportService _teleportService; // to update aetheryte list

    private Hook<ActionManager.Delegates.OpenCastBar>? _openCastBarHook;
    private Hook<Telepo.Delegates.Teleport>? _teleportHook;

    private TeleportInfo? _teleportInfo;
    private bool _isCastingTeleport;

    public override unsafe ValueTask OnEnable()
    {
        return new ValueTask(_framework.Run(() =>
        {
            _disposables = DisposableBag.Create(
                _openCastBarHook = _gameInteropProvider.EnabledHookFromAddress<ActionManager.Delegates.OpenCastBar>(
                    ActionManager.MemberFunctionPointers.OpenCastBar,
                    OpenCastBarDetour),

                _teleportHook = _gameInteropProvider.EnabledHookFromAddress<Telepo.Delegates.Teleport>(
                    Telepo.MemberFunctionPointers.Teleport,
                    TeleportDetour),

                _addonLifecycle.OnPreRefresh(OnCastBarPreRefresh, "_CastBar"),
                _clientState.OnTerritoryChanged(OnTerritoryChanged));

        }));
    }

    public override ValueTask OnDisable()
    {
        return new ValueTask(_framework.Run(() =>
        {
            DisposeAndNull(ref _disposables);
            Clear();
        }));
    }

    private void OnTerritoryChanged(uint id)
    {
        Clear();
    }

    private void Clear()
    {
        _isCastingTeleport = false;
        _teleportInfo = null;
    }

    private unsafe void OnCastBarPreRefresh(AddonArgs args)
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
            _ when row.PlaceName.IsValid => row.PlaceName.Value.Name.ToString(),
            _ => string.Empty
        };

        AtkStage.Instance()->GetStringArrayData(StringArrayType.CastBar)->SetValue(0, placeName.StripSoftHyphen(), false, true, false);

        Clear();
    }

    private unsafe void OpenCastBarDetour(ActionManager* thisPtr, BattleChara* character, ActionType actionType, uint actionId, uint spellId, uint extraParam, float castTimeElapsed, float castTimeTotal)
    {
        _isCastingTeleport = actionType == ActionType.Action && actionId == 5;
        _openCastBarHook!.OriginalDisposeSafe(thisPtr, character, actionType, actionId, spellId, extraParam, castTimeElapsed, castTimeTotal);
    }

    private unsafe bool TeleportDetour(Telepo* telepo, uint aetheryteID, byte subIndex)
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

        return _teleportHook!.OriginalDisposeSafe(telepo, aetheryteID, subIndex);
    }
}
