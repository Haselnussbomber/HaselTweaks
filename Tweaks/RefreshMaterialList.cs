using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe class RefreshMaterialList : Tweak
{
    public override string Name => "Refresh Material List";
    public override string Description => "Refreshes the material list and recipe tree when an item was crafted, fished or gathered.";

    private readonly AddonObserver CatchObserver = new("Catch");
    private readonly AddonObserver SynthesisObserver = new("Synthesis");
    private readonly AddonObserver SynthesisSimpleObserver = new("SynthesisSimple");
    private readonly AddonObserver GatheringObserver = new("Gathering");

    public override void Enable()
    {
        CatchObserver.OnOpen += Refresh;

        SynthesisObserver.OnClose += Refresh;
        SynthesisSimpleObserver.OnClose += Refresh;
        GatheringObserver.OnClose += Refresh;
    }

    public override void Disable()
    {
        CatchObserver.OnOpen -= Refresh;

        SynthesisObserver.OnClose -= Refresh;
        SynthesisSimpleObserver.OnClose -= Refresh;
        GatheringObserver.OnClose -= Refresh;
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        CatchObserver.Update();

        SynthesisObserver.Update();
        SynthesisSimpleObserver.Update();
        GatheringObserver.Update();
    }

    private void Refresh(AddonObserver sender)
    {
        var recipeMaterialList = GetAgent<AgentRecipeMaterialList>(AgentId.RecipeMaterialList)->GetAddon();
        var recipeTree = GetAgent<AgentRecipeTree>(AgentId.RecipeTree)->GetAddon();

        if (recipeMaterialList == null && recipeTree == null)
            return;

        if (recipeMaterialList != null)
        {
            Log("Refreshing RecipeMaterialList");
            var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
            recipeMaterialList->ReceiveEvent(AtkEventType.ButtonClick, 1, atkEvent, 0);
            IMemorySpace.Free(atkEvent);
        }

        if (recipeTree != null)
        {
            Log("Refreshing RecipeTree");
            var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
            recipeTree->ReceiveEvent(AtkEventType.ButtonClick, 0, atkEvent, 0);
            IMemorySpace.Free(atkEvent);
        }
    }
}
