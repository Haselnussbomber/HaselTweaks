using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Structs;
using Lumina.Excel;

namespace HaselTweaks.Services;

public unsafe class StringManager : IDisposable
{
    private readonly Dictionary<uint, string> AddonCache = new();
    private readonly Dictionary<(NameFormatter.Placeholder placeholder, NameFormatter.IdConverter idConverter, uint id), string> NameCache = new();
    private readonly Dictionary<(string sheetName, uint rowId, string columnName), string> SheetCache = new();

    public string? FormatName(NameFormatter.Placeholder placeholder, NameFormatter.IdConverter idConverter, uint id)
    {
        var key = (placeholder, idConverter, id);

        if (!NameCache.TryGetValue(key, out var value))
        {
            var ptr = NameFormatter.Format(placeholder, id, idConverter, 1);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return null;

                NameCache.Add(key, value);
            }
        }

        return value;
    }

    public string GetAddonText(uint id)
    {
        if (!AddonCache.TryGetValue(id, out var value))
        {
            var ptr = (nint)RaptureTextModule.Instance()->GetAddonText(id);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return null ?? $"[Addon#{id}]";

                AddonCache.Add(id, value);
            }
        }

        return value ?? $"[Addon#{id}]";
    }

    public string GetSheetText<T>(uint rowId, string columnName) where T : ExcelRow
    {
        var sheetType = typeof(T);
        var sheetName = sheetType.Name;
        var key = (sheetName, rowId, columnName);

        if (!SheetCache.TryGetValue(key, out var value))
        {

            var prop = sheetType.GetProperty(columnName);
            if (prop == null || prop.PropertyType != typeof(Lumina.Text.SeString))
                return string.Empty;

            var sheetRow = GetRow<T>(rowId);
            if (sheetRow == null)
                return string.Empty;

            var seStr = (Lumina.Text.SeString?)prop.GetValue(sheetRow);
            if (seStr == null)
                return string.Empty;

            value = SeString.Parse(seStr.RawData).ToString();

            SheetCache.Add(key, value);
        }

        return value;
    }

    public void Dispose()
    {
        AddonCache.Clear();
        NameCache.Clear();
        SheetCache.Clear();
    }
}
