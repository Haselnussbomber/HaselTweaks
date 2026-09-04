using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedRecipeNote : ConfigurableTweak<EnhancedRecipeNoteConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IClientState _clientState;
    private readonly TextService _textService;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonLifecycle.OnPreFinalize(OnRecipeNotePreFinalize, "RecipeNote"),
            _clientState.OnLogin(OnLogin));

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        return ValueTask.CompletedTask;
    }

    private void OnLogin()
    {
        var agent = AgentRecipeNote.Instance();
        var haselAgent = (HaselAgentRecipeNote*)agent;

        using var tempStr = new Utf8String();
        using var rssb = new RentedSeStringBuilder();

        HaselAgentRecipeNote.ClearHistory(&agent->RecipeSearchHistory);

        foreach (var term in _config.SearchHistory)
        {
            tempStr.SetString(rssb.Builder.Clear().Append(term).GetViewAsSpan());
            haselAgent->AddRecipeSearchHistoryTerm(&tempStr);
        }
    }

    private void OnRecipeNotePreFinalize(AddonArgs args)
    {
        var agent = AgentRecipeNote.Instance();

        _config.SearchHistory.Clear();

        foreach (ref var term in agent->RecipeSearchHistory)
        {
            _config.SearchHistory.Add(term.StringPtr.AsReadOnlySeString());
        }

        _config.SearchHistory.Reverse();
    }
}
