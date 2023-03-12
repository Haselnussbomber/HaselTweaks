using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

public unsafe class AddonObserver
{
    public delegate AtkUnitBase* GetAddonDelegate();
    public delegate void OnOpenDelegate(AddonObserver sender, string addonName, AtkUnitBase* unitBase);
    public delegate void OnCloseDelegate(AddonObserver sender, string addonName, AtkUnitBase* unitBase);

    public event OnOpenDelegate? OnOpen;
    public event OnCloseDelegate? OnClose;

    public Dictionary<string, AddonState> AddonStates = new();

    public void Register(string addonName, GetAddonDelegate getAddonFn)
    {
        if (!AddonStates.TryGetValue(addonName, out var state))
        {
            state = new(getAddonFn);
            AddonStates.Add(addonName, state);
        }

        state.NumRegistrations += 1;
    }

    public void Register(string addonName)
        => Register(addonName, () => GetAddon(addonName));

    public void Register(params string[] addonNames)
    {
        foreach (var addonName in addonNames)
            Register(addonName, () => GetAddon(addonName));
    }

    public void Unregister(string addonName)
    {
        if (AddonStates.TryGetValue(addonName, out var state))
        {
            state.NumRegistrations -= 1;

            if (state.NumRegistrations == 0)
            {
                AddonStates.Remove(addonName);
            }
        }
    }

    public void Unregister(params string[] addonNames)
    {
        foreach (var addonName in addonNames)
            Unregister(addonName);
    }

    public void Update()
    {
        foreach (var (addonName, state) in AddonStates)
        {
            var unitBase = state.GetAddonFn();
            var isOpen = unitBase != null && unitBase->IsVisible;

            if (state.Open != isOpen)
            {
                if (isOpen)
                {
                    OnOpen?.Invoke(this, addonName, unitBase);
                }
                else
                {
                    OnClose?.Invoke(this, addonName, unitBase);
                }

                state.Open = isOpen;
            }
        }
    }

    public bool IsOpen(string addonName)
        => AddonStates.TryGetValue(addonName, out var state) && state.Open;

    public record AddonState
    {
        public GetAddonDelegate GetAddonFn { get; }
        public int NumRegistrations = 0;
        public bool Open = false;

        public AddonState(GetAddonDelegate GetAddonFn)
        {
            this.GetAddonFn = GetAddonFn;
        }
    }
}
