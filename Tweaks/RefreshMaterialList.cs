using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class RefreshMaterialList : Tweak
{
    public override string Name => "Refresh Material List";
    public override string Description => "Refreshes the material list and recipe tree when an item was crafted, fished or gathered.";

    private record WindowState
    {
        public string Addon = string.Empty;
        public bool WasOpen;
        public bool IsOpen;
        public bool OnOpen;

        public WindowState(string Addon, bool OnOpen = false)
        {
            this.Addon = Addon;
            this.OnOpen = OnOpen;
        }
    }

    private readonly WindowState[] windowState = {
        new("Synthesis"),
        new("SynthesisSimple"),
        new("Gathering"),
        new("Catch", true)
    };

    public override void OnFrameworkUpdate(Framework framework)
    {
        var recipeMaterialList = GetAgent<AgentRecipeMaterialList>(AgentId.RecipeMaterialList)->GetAddon();
        var recipeTree = GetAgent<AgentRecipeTree>(AgentId.RecipeTree)->GetAddon();

        if ((recipeMaterialList == null && recipeTree == null) || !ShouldRefresh())
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

    private bool ShouldRefresh()
    {
        var ret = false;

        // checks if any depending windows were open and are now closed
        foreach (var state in windowState)
        {
            var unitBase = GetAddon(state.Addon);
            state.WasOpen = state.IsOpen;
            state.IsOpen = unitBase != null && unitBase->IsVisible;

            var changed = (!state.OnOpen && state.WasOpen && !state.IsOpen) || (state.OnOpen && !state.WasOpen && state.IsOpen);
            if (changed)
            {
                var changestr = state.OnOpen ? "opened" : "closed";
                Debug($"{state.Addon} {changestr}");
            }
            ret = ret || changed;
        }

        return ret;
    }
}
