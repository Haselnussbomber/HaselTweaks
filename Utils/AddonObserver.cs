using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

public class AddonObserver
{
    public unsafe delegate AtkUnitBase* GetAddonDelegate();
    public delegate void OnOpenDelegate(AddonObserver sender);
    public delegate void OnCloseDelegate(AddonObserver sender);

    public event OnOpenDelegate? OnOpen;
    public event OnCloseDelegate? OnClose;

    public GetAddonDelegate? GetAddonFn;
    public bool IsOpen;

    public unsafe AddonObserver(GetAddonDelegate fn)
    {
        GetAddonFn = fn;

        if (GetAddonFn != null)
        {
            var unitBase = GetAddonFn();
            IsOpen = unitBase == null || unitBase->IsVisible;
        }
    }

    public unsafe AddonObserver(string addonName) : this(() => GetAddon(addonName)) { }

    internal unsafe void Update()
    {
        if (GetAddonFn == null)
            return;

        var unitBase = GetAddonFn();
        if (unitBase == null || !unitBase->IsVisible)
        {
            if (IsOpen)
            {
                IsOpen = false;
                OnClose?.Invoke(this);
            }

            return;
        }

        if (!IsOpen)
        {
            IsOpen = true;
            OnOpen?.Invoke(this);
        }
    }
}
