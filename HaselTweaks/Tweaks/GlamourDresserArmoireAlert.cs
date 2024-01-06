using HaselCommon.Services;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class GlamourDresserArmoireAlert : Tweak
{
    public override void Enable()
    {
        if (IsAddonOpen("MiragePrismPrismBox"))
            Service.WindowManager.OpenWindow<GlamourDresserArmoireAlertWindow>();
    }

    public override void Disable()
    {
        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<GlamourDresserArmoireAlertWindow>();
    }

    public override void OnAddonOpen(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
            Service.WindowManager.OpenWindow<GlamourDresserArmoireAlertWindow>();
    }

    public override void OnAddonClose(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
            Service.WindowManager.CloseWindow<GlamourDresserArmoireAlertWindow>();
    }
}
