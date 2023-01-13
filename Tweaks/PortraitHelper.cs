using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public unsafe class PortraitHelper : Tweak
{
    public override string Name => "Portrait Helper";
    public override string Description => @"Adds Copy/Paste buttons to the ""Edit Portrait"" window, which allow settings to be copied to other portraits.

The Advanced Mode allows to specify which settings should be pasted.";

    private readonly PortraitHelperWindow Window = new();

    internal static AgentBannerEditor* AgentBannerEditor => (AgentBannerEditor*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.BannerEditor);
    internal static AddonBannerEditor* AddonBannerEditor => (AddonBannerEditor*)AtkUtils.GetUnitBase("BannerEditor");

    public PortraitHelper() : base()
    {
        Window.SetTweak(this);
    }

    public override void Enable()
    {
        Plugin.WindowSystem.AddWindow(Window);
    }

    public override void Disable()
    {
        Plugin.WindowSystem.RemoveWindow(Window);
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        var addon = AtkUtils.GetUnitBase("BannerEditor");
        if (addon == null)
        {
            if (Window.IsOpen)
                Window.Toggle();
            return;
        }

        if (!Window.IsOpen)
            Window.Toggle();
    }
}
