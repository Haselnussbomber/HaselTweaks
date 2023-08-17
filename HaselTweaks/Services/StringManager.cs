using System.Collections.Generic;
using System.Reflection;
using Dalamud.Memory;
using Dalamud.Utility;
using HaselTweaks.Structs;
using Lumina.Excel;

namespace HaselTweaks.Services;

public unsafe class StringManager
{
    private readonly Dictionary<(NameFormatter.Placeholder placeholder, NameFormatter.IdConverter idConverter, uint id), string> _nameCache = new();
    private readonly Dictionary<(string sheetName, uint rowId, string columnName), string> _sheetCache = new();

    public string? FormatName(NameFormatter.Placeholder placeholder, NameFormatter.IdConverter idConverter, uint id)
    {
        var key = (placeholder, idConverter, id);

        if (!_nameCache.TryGetValue(key, out var value))
        {
            var ptr = NameFormatter.Format(placeholder, id, idConverter, 1);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return null;

                _nameCache.Add(key, value);
            }
        }

        return value;
    }

    public string GetSheetText<T>(uint rowId, string columnName) where T : ExcelRow
    {
        var sheetType = typeof(T);
        var sheetName = sheetType.Name;
        var key = (sheetName, rowId, columnName);

        if (!_sheetCache.TryGetValue(key, out var value))
        {
            var prop = sheetType.GetProperty(columnName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || prop.PropertyType != typeof(Lumina.Text.SeString))
                return string.Empty;

            var sheetRow = GetRow<T>(rowId);
            if (sheetRow == null)
                return string.Empty;

            var seStr = (Lumina.Text.SeString?)prop.GetValue(sheetRow);
            if (seStr == null)
                return string.Empty;

            value = seStr.ToDalamudString().TextValue;

            _sheetCache.Add(key, value);
        }

        return value;
    }

    public void Clear()
    {
        _nameCache.Clear();
        _sheetCache.Clear();
    }
}
