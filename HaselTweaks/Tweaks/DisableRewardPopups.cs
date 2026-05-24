namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class DisableRewardPopups : ConfigurableTweak<DisableRewardPopupsConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;

    private static string[] AddonNames => [
        "FateReward",
        "GoldSaucerReward",
        "WKSReward"
    ];

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreShow, AddonNames, OnPreShow);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreShow, AddonNames, OnPreShow);
    }

    private void OnPreShow(AddonEvent type, AddonArgs args)
    {
        switch (args.AddonName)
        {
            case "FateReward":
                if (_config.DisableFateReward)
                    args.PreventOriginal();
                break;

            case "GoldSaucerReward":
                if (_config.DisableGoldSaucerReward)
                    args.PreventOriginal();
                break;

            case "WKSReward":
                if (_config.DisableWKSReward)
                    args.PreventOriginal();
                break;
        }
    }
}
