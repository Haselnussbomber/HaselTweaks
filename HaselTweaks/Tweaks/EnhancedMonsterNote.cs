using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedMonsterNote : ITweak
{
    private readonly ILogger<EnhancedMonsterNote> _logger;
    private readonly AddonObserver _addonObserver;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    public void OnAddonOpen(string addonName)
    {
        if (addonName != "MonsterNote")
            return;

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        // prevent item selection for controller users to reset to the first entry
        if (AgentMonsterNote.Instance()->Filter == 2)
            return;

        _logger.LogDebug("Changing selected tab...");

        AgentMonsterNote.Instance()->Filter = 2;
    }
}
