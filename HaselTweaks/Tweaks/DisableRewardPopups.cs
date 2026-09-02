namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class DisableRewardPopups : ConfigurableTweak<DisableRewardPopupsConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonLifecycle.OnPreShow(OnFateReward, "FateReward"),
            _addonLifecycle.OnPreShow(OnGoldSaucerReward, "GoldSaucerReward"),
            _addonLifecycle.OnPreShow(OnWKSReward, "WKSReward"));
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }

    private void OnFateReward(AddonArgs args)
    {
        if (_config.DisableFateReward)
            args.PreventOriginal();
    }

    private void OnGoldSaucerReward(AddonArgs args)
    {
        if (_config.DisableGoldSaucerReward)
            args.PreventOriginal();
    }

    private void OnWKSReward(AddonArgs args)
    {
        if (_config.DisableWKSReward)
            args.PreventOriginal();
    }
}
