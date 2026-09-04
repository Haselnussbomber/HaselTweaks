using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedIsleworksAgenda : ConfigurableTweak<EnhancedIsleworksAgendaConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly AddonObserver _addonObserver;
    private readonly MJICraftScheduleSettingSearchBar _window;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonLifecycle.OnPreReceiveEvent(OnPreReceiveEvent, "MJICraftScheduleSetting"),
            _addonObserver.OnShow(OnShow, "MJICraftScheduleSetting"),
            _addonObserver.OnHide(OnHide, "MJICraftScheduleSetting"));

        if (_config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"u8))
            _window.Open();

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _window.Close();
        return ValueTask.CompletedTask;
    }

    private void OnPreReceiveEvent(AddonArgs addonArgs)
    {
        if (!_config.DisableTreeListTooltips || addonArgs is not AddonReceiveEventArgs args)
            return;

        if (args.EventType != AtkEventType.ListItemRollOver || args.EventParam != 2)
            return;

        var addon = args.GetAddon<AddonMJICraftScheduleSetting>();
        var eventData = args.GetEventData<AtkEventData.AtkListItemData>();
        var index = eventData->SelectedIndex;
        var item = addon->TreeList->GetItem(index);
        if (item == null || item->Type == TreeListItemType.Group)
            return;

        args.PreventOriginal();
    }

    private void OnShow(AtkUnitBase* addon)
    {
        if (_config.EnableSearchBar)
            _window.Open();
    }

    private void OnHide(AtkUnitBase* addon)
    {
        _window.Close();
    }
}
