using System.Threading.Tasks;
using Dalamud.Game.Network.Structures;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SaferItemSearch : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly MarketBoardService _marketBoardService;

    private bool _isSearching;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonLifecycle.OnPostRequestedUpdate(ItemSearch_PostRequestedUpdate, "ItemSearch"),
            _addonLifecycle.OnPostSetup(RetainerSell_PostSetup, "RetainerSell"),

            EventExtensions.Subscribe(
                handler => _marketBoardService.ListingsStart += handler.Invoke,
                handler => _marketBoardService.ListingsStart -= handler.Invoke,
                OnListingsStart),

            EventExtensions.Subscribe(
                handler => _marketBoardService.ListingsEnd += handler.Invoke,
                handler => _marketBoardService.ListingsEnd -= handler.Invoke,
                OnListingsEnd));

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        return ValueTask.CompletedTask;
    }

    private void ItemSearch_PostRequestedUpdate(AddonArgs args)
    {
        var addon = args.GetAddon<AddonItemSearch>();
        if (addon == null)
            return;

        for (var i = 0; i < addon->ResultsList->GetItemCount(); i++)
        {
            addon->ResultsList->SetItemDisabledState(i, _isSearching);
        }
    }

    private void RetainerSell_PostSetup(AddonArgs args)
    {
        UpdateRetainerSellButton(args.GetAddon<AddonRetainerSell>());
    }

    private void UpdateRetainerSellButton(AddonRetainerSell* addon = null)
    {
        if (addon == null)
            addon = GetAddon<AddonRetainerSell>("RetainerSell"u8);

        if (addon == null)
            return;

        addon->ComparePrices->SetEnabledState(!_isSearching);
    }

    private void OnListingsStart()
    {
        _isSearching = true;
        UpdateRetainerSellButton();
    }

    private void OnListingsEnd(IReadOnlyList<IMarketBoardItemListing> listings)
    {
        _isSearching = false;
        UpdateRetainerSellButton();
    }
}
