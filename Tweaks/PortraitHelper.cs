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

    public override unsafe void OnAddonOpen(string addonName, AtkUnitBase* unitbase)
    {
        if (addonName != "BannerEditor")
            return;

        Window.AddonBannerEditor = (AddonBannerEditor*)unitbase;
        Window.IsOpen = true;
    }

    public override unsafe void OnAddonClose(string addonName, AtkUnitBase* unitbase)
    {
        if (addonName != "BannerEditor")
            return;

        Window.AddonBannerEditor = null;
        Window.IsOpen = false;
    }
}
