using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Tweaks;

// TODO: use the actual sorting function rather than the command?
public unsafe class AutoSortArmouryChest : BaseTweak
{
    public override string Name => "Auto Sort Armoury Chest";

    private bool wasVisible = false;

    public override void OnFrameworkUpdate(Framework framework)
    {
        var unitBase = Utils.GetUnitBase("ArmouryBoard");
        if (unitBase == null || unitBase->RootNode == null) return;

        var isVisible = unitBase->RootNode->IsVisible;

        if (!wasVisible && isVisible)
            Run();

        wasVisible = isVisible;
    }

    private void Run()
    {
        var uiModulePtr = (UIModule*)Service.GameGui.GetUIModule();
        if (uiModulePtr == null) return;

        var raptureShellModulePtr = uiModulePtr->GetRaptureShellModule();
        if (raptureShellModulePtr == null) return;

        var raptureMacroModulePtr = uiModulePtr->GetRaptureMacroModule();
        if (raptureMacroModulePtr == null) return;

        if (raptureShellModulePtr->MacroCurrentLine >= 0)
        {
            Service.Chat.PrintError("Can't sort armoury chest while macros are running.");
            return;
        }

        // runs shared macro in 3rd slot
        raptureShellModulePtr->ExecuteMacro(raptureMacroModulePtr->Shared[2]);
    }
}
