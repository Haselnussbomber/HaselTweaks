using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Runtime.InteropServices;

namespace HaselTweaks.Tweaks;

public unsafe class RefreshMaterialList : BaseTweak
{
    public override string Name => "Refresh Material List";

    private delegate void* ReceiveEventDelegate(IntPtr addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, IntPtr resNode);

    private record WindowState
    {
        public string Addon = string.Empty;
        public bool WasOpen = false;
        public bool IsOpen = false;
        public bool OnOpen = false;
    }

    private readonly WindowState[] windowState = new[]
    {
        new WindowState { Addon = "Synthesis" },
        new WindowState { Addon = "SynthesisSimple" },
        new WindowState { Addon = "Gathering" },
        new WindowState { Addon = "Catch", OnOpen = true }
    };

    public override void OnFrameworkUpdate(Framework framework)
    {
        var recipeMaterialList = Utils.GetUnitBase("RecipeMaterialList");
        var recipeTree = Utils.GetUnitBase("RecipeTree");
        if (recipeMaterialList == null && recipeTree == null) return;

        if (!ShouldRefresh()) return;

        if (recipeMaterialList != null)
        {
            PluginLog.Verbose("[RefreshMaterialList] Refreshing RecipeMaterialList");
            var receiveEvent = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>((IntPtr)recipeMaterialList->AtkEventListener.vfunc[2]);
            receiveEvent((IntPtr)recipeMaterialList, AtkEventType.ButtonClick, 1, recipeMaterialList->RootNode->AtkEventManager.Event, (IntPtr)recipeMaterialList->RootNode);
        }

        if (recipeTree != null)
        {
            PluginLog.Verbose("[RefreshMaterialList] Refreshing RecipeTree");
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
            var unitBase = Utils.GetUnitBase(state.Addon);
            state.WasOpen = state.IsOpen;
            state.IsOpen = unitBase != null && unitBase->IsVisible;

            var changed = (!state.OnOpen && state.WasOpen && !state.IsOpen) || (state.OnOpen && !state.WasOpen && state.IsOpen);
            if (changed)
            {
                var changestr = state.OnOpen ? "opened" : "closed";
                PluginLog.Verbose($"[RefreshMaterialList] {state.Addon} {changestr}");
            }
            ret = ret || changed;
        }

        return ret;
    }
}
