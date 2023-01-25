using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

public unsafe class AddonObserver
{
    public delegate AtkUnitBase* GetAddonDelegate();
    public delegate void OnOpenDelegate(AddonObserver sender, AtkUnitBase* unitBase);
    public delegate void OnCloseDelegate(AddonObserver sender, AtkUnitBase* unitBase);

    public event OnOpenDelegate? OnOpen;
    public event OnCloseDelegate? OnClose;

    public GetAddonDelegate? GetAddonFn;
    public bool IsOpen;

    public AddonObserver(GetAddonDelegate fn)
    {
        GetAddonFn = fn;

        if (GetAddonFn != null)
        {
            var unitBase = GetAddonFn();
            IsOpen = unitBase == null || unitBase->IsVisible;
        }
    }

    public AddonObserver(string addonName) : this(() => GetAddon(addonName)) { }

    internal void Update()
    {
        if (GetAddonFn == null)
            return;

        var unitBase = GetAddonFn();
        if (unitBase == null || !unitBase->IsVisible)
        {
            if (IsOpen)
            {
                IsOpen = false;
                OnClose?.Invoke(this, unitBase);
            }

            return;
        }

        if (!IsOpen)
        {
            IsOpen = true;
            OnOpen?.Invoke(this, unitBase);
        }
    }
}
