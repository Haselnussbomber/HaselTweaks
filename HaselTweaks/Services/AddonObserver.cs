using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Services;

public unsafe class AddonObserver : IDisposable
{
    public delegate void CallbackDelegate(string addonName);
    public event CallbackDelegate? AddonOpen;
    public event CallbackDelegate? AddonClose;

    public AddonObserver()
    {
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        Update();
    }

    private HashSet<nint> _addedUnits { get; } = new();
    private HashSet<nint> _removedUnits { get; } = new();
    private HashSet<nint> _loadedUnits { get; } = new();
    private Dictionary<nint, string> _nameCache { get; } = new();

    private void Update()
    {
        _addedUnits.Clear();
        _removedUnits.Clear();

        var raptureAtkModule = RaptureAtkModule.Instance();
        if (raptureAtkModule == null)
            return;

        // check added units
        var allLoadedList = &raptureAtkModule->RaptureAtkUnitManager.AtkUnitManager.AllLoadedUnitsList;
        // TODO: update when https://github.com/aers/FFXIVClientStructs/pull/426 is merged
        var count = (ushort)(allLoadedList->Count & 0xFFFF);
        var entries = new Span<Pointer<AtkUnitBase>>(&allLoadedList->AtkUnitEntries, count);
        for (var i = 0; i < count; i++)
        {
            var address = (nint)entries[i].Value;
            if (address == 0 || _loadedUnits.Contains(address) || !raptureAtkModule->AtkModule.IsAddonReady(((AtkUnitBase*)address)->ID))
                continue;

            _addedUnits.Add(address);
        }

        foreach (var address in _addedUnits)
        {
            var unitBase = (AtkUnitBase*)address;
            var name = MemoryHelper.ReadStringNullTerminated((nint)unitBase->Name);

            if (!_nameCache.ContainsKey(address))
                _nameCache.Add(address, name);

            AddonOpen?.Invoke(name);
        }

        // check removed units
        foreach (var address in _loadedUnits)
        {
            var isLoaded = false;

            for (var i = 0; i < count; i++)
            {
                if ((nint)entries[i].Value == address)
                {
                    isLoaded = true;
                    break;
                }
            }

            if (!isLoaded)
                _removedUnits.Add(address);
        }

        foreach (var address in _removedUnits)
        {
            if (_nameCache.TryGetValue(address, out var name))
            {
                _nameCache.Remove(address);

                AddonClose?.Invoke(name);
            }
        }

        // update LoadedUnits
        foreach (var address in _addedUnits)
        {
            _loadedUnits.Add(address);
        }

        foreach (var address in _removedUnits)
        {
            _loadedUnits.Remove(address);
        }
    }
}
