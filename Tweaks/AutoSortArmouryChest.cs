using Dalamud;
using Dalamud.Game;

namespace HaselTweaks.Tweaks;

public unsafe class AutoSortArmouryChest : Tweak
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
        var definition = "/isort condition armoury ilv des";
        var execute = "/isort execute armoury";

        if (Service.ClientState.ClientLanguage == ClientLanguage.German)
        {
            definition = "/sort def arsenal ggstufe abs";
            execute = "/sort los arsenal";
        }
        else if (Service.ClientState.ClientLanguage == ClientLanguage.French)
        {
            definition = "/triobjet condition arsenal niveauobjet décroissant";
            execute = "/triobjet exécuter arsenal";
        }
        else if (Service.ClientState.ClientLanguage == ClientLanguage.Japanese)
        {
            // i'm sorry, but i can't decipher the description
            return;
        }

        Plugin.XivCommon.Functions.Chat.SendMessage(definition);
        Plugin.XivCommon.Functions.Chat.SendMessage(execute);
    }
}
