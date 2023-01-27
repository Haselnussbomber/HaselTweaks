using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public unsafe class PortraitHelper : Tweak
{
    public override string Name => "Portrait Helper";
    public override string Description => @"Adds Copy/Paste buttons to the ""Edit Portrait"" window, which allow settings to be copied to other portraits.

The Advanced Mode allows to specify which settings should be pasted.";

    private readonly PortraitHelperWindow Window = new();

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
        var agent = GetAgent<AgentBannerEditor>(AgentId.BannerEditor);
        var addon = GetAddon<AddonBannerEditor>((AgentInterface*)agent);

        if (addon == null)
        {
            if (Window.IsOpen)
                Window.IsOpen = false;

            if (Window.AgentBannerEditor != null)
                Window.AgentBannerEditor = null;

            if (Window.AddonBannerEditor != null)
                Window.AddonBannerEditor = null;

            return;
        }

        if (!Window.IsOpen)
        {
            Window.AgentBannerEditor = agent;
            Window.AddonBannerEditor = addon;
            Window.IsOpen = true;
        }
    }
}
