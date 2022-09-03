using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe class RefreshMaterialList : Tweak
{
    public override string Name => "Refresh Material List";
    public override string Description => "Refreshes the material list and recipe tree when an item was crafted, fished or gathered.";

    private delegate void* ReceiveEventDelegate(IntPtr addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, IntPtr resNode);

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
        var recipeMaterialList = AtkUtils.GetUnitBase("RecipeMaterialList");
        var recipeTree = AtkUtils.GetUnitBase("RecipeTree");
        if (recipeMaterialList == null && recipeTree == null) return;

        if (!ShouldRefresh()) return;

        if (recipeMaterialList != null)
        {
            Log("Refreshing RecipeMaterialList");
            var receiveEvent = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>((IntPtr)recipeMaterialList->AtkEventListener.vfunc[2]);
            receiveEvent((IntPtr)recipeMaterialList, AtkEventType.ButtonClick, 1, recipeMaterialList->RootNode->AtkEventManager.Event, (IntPtr)recipeMaterialList->RootNode);
        }

        if (recipeTree != null)
        {
            Log("Refreshing RecipeTree");
            var receiveEvent = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>((IntPtr)recipeTree->AtkEventListener.vfunc[2]);
            receiveEvent((IntPtr)recipeTree, AtkEventType.ButtonClick, 0, recipeTree->RootNode->AtkEventManager.Event, (IntPtr)recipeTree->RootNode);
        }
    }

    private bool ShouldRefresh()
    {
        var ret = false;

        // checks if any depending windows were open and are now closed
        foreach (var state in windowState)
        {
            var unitBase = AtkUtils.GetUnitBase(state.Addon);
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
