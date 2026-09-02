using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PreventMovementReelIn : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private Hook<FishingEventHandler.Delegates.CancelByPlayerMovement>? _hook;

    public override void OnEnable()
    {
        _hook = _gameInteropProvider.EnabledHookFromAddress<FishingEventHandler.Delegates.CancelByPlayerMovement>(
            (nint)FishingEventHandler.StaticVirtualTablePointer->CancelByPlayerMovement,
            CancelByPlayerMovementDetour);
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _hook);
    }

    private void CancelByPlayerMovementDetour(FishingEventHandler* thisPtr, bool a2, bool a3)
    {
        // do nothing :)
    }
}
