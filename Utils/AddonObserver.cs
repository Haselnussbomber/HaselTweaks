using System.Collections.Generic;
using System.Linq;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Utils;

public static unsafe class AddonObserver
{
    private static HashSet<nint> AddedUnits { get; } = new();
    private static HashSet<nint> RemovedUnits { get; } = new();
    public static HashSet<nint> LoadedUnits { get; } = new();
    public static Dictionary<nint, string> NameCache { get; } = new();

    public static void Update()
    {
        AddedUnits.Clear();
        RemovedUnits.Clear();

        var raptureAtkModule = RaptureAtkModule.Instance();
        if (raptureAtkModule == null)
            return;

        // check added units
        var allLoadedList = (HaselAtkUnitList*)&raptureAtkModule->RaptureAtkUnitManager.AtkUnitManager.AllLoadedUnitsList;
        for (var i = 0; i < allLoadedList->Count; i++)
        {
            var address = (nint)allLoadedList->AtkUnitsSpan[i].Value;
            if (address == 0 || LoadedUnits.Contains(address) || !raptureAtkModule->AtkModule.IsAddonReady(((AtkUnitBase*)address)->ID))
                continue;

            AddedUnits.Add(address);
        }

        foreach (var address in AddedUnits)
        {
            var unitBase = (AtkUnitBase*)address;
            var name = MemoryHelper.ReadStringNullTerminated((nint)unitBase->Name);

            if (!NameCache.ContainsKey(address))
            {
                NameCache.Add(address, name);
            }

            foreach (var tweak in Plugin.Tweaks.Where(tweak => tweak.Enabled))
            {
                tweak.OnAddonOpenInternal(name, unitBase);
            }
        }

        // check removed units
        foreach (var address in LoadedUnits)
        {
            var isLoaded = false;

            for (var i = 0; i < allLoadedList->Count; i++)
            {
                if ((nint)allLoadedList->AtkUnitsSpan[i].Value == address)
                {
                    isLoaded = true;
                    break;
                }
            }

            if (!isLoaded)
            {
                RemovedUnits.Add(address);
            }
        }

        foreach (var address in RemovedUnits)
        {
            if (NameCache.TryGetValue(address, out var name))
            {
                NameCache.Remove(address);

                foreach (var tweak in Plugin.Tweaks.Where(tweak => tweak.Enabled))
                {
                    tweak.OnAddonClose(name);
                }
            }
        }

        // update LoadedUnits
        foreach (var address in AddedUnits)
        {
            LoadedUnits.Add(address);
        }

        foreach (var address in RemovedUnits)
        {
            LoadedUnits.Remove(address);
        }
    }
}
