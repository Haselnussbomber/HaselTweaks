using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PreventMovementReelIn : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private Hook<FishingEventHandler.Delegates.CancelByPlayerMovement>? _hook;

    public override void OnEnable()
    {
        _hook = _gameInteropProvider.HookFromAddress<FishingEventHandler.Delegates.CancelByPlayerMovement>(
            (nint)FishingEventHandler.StaticVirtualTablePointer->CancelByPlayerMovement,
            CancelByPlayerMovementDetour);

        _hook.Enable();
    }

    public override void OnDisable()
    {
        _hook?.Dispose();
        _hook = null;
    }

    private void CancelByPlayerMovementDetour(FishingEventHandler* thisPtr, bool a2, bool a3)
    {
        // do nothing :)
    }
}
