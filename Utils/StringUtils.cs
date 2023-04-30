using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselTweaks.Structs;
using Lumina.Excel;

namespace HaselTweaks.Utils;

public sealed unsafe partial class StringUtils
{
    [GeneratedRegex("^[\\ue000-\\uf8ff]+ ")]
    private static partial Regex Utf8PrivateUseAreaRegex();

    private static Dictionary<uint, string> QuestCache = new();
    private static Dictionary<uint, string> AddonCache = new();
    private static Dictionary<FormatterMode, Dictionary<FormatterType, Dictionary<uint, string>>> ObjectCache = new(); // ObjectCache[mode][type][id]
    private static Dictionary<string, Dictionary<uint, Dictionary<string, string>>> SheetCache = new(); // SheetCache[sheetName][rowId][columnName]

    public static string FormatObject(FormatterMode formatter, uint id, FormatterType type)
    {
        if (!ObjectCache.TryGetValue(formatter, out var formatterDict))
        {
            formatterDict = new();
            ObjectCache.Add(formatter, formatterDict);
        }

        if (!formatterDict.TryGetValue(type, out var typeDict))
        {
            typeDict = new();
            formatterDict.Add(type, typeDict);
        }

        if (!typeDict.TryGetValue(id, out var value))
        {
            var ptr = Formatter.FormatObjectName(formatter, id, type, 1);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();
                typeDict.Add(id, value);
            }
        }

        return value ?? $"[{Enum.GetName(typeof(FormatterType), type)}#{id}]";
    }

    public static string GetENpcResidentName(uint npcId) => FormatObject(FormatterMode.ObjStr, npcId, FormatterType.ENpcResident);
    public static string GetEObjName(uint objId) => FormatObject(FormatterMode.ObjStr, objId, FormatterType.EObjName);
    public static string GetItemName(uint itemId) => FormatObject(FormatterMode.Item, itemId, FormatterType.None);

    public static string? GetAddonText(uint rowId)
    {
        if (!AddonCache.TryGetValue(rowId, out var value))
        {
            var ptr = (nint)Framework.Instance()->GetUiModule()->GetRaptureTextModule()->GetAddonText(rowId);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return null;

                AddonCache.Add(rowId, value);
            }
        }

        return value;
    }

    public static string GetSheetText<T>(uint rowId, string columnName, bool noPrivateUseCharacters = false) where T : ExcelRow
    {
        var sheetType = typeof(T);
        var sheetName = sheetType.Name;

        if (!SheetCache.TryGetValue(sheetName, out var sheet))
        {
            sheet = new();
            SheetCache.Add(sheetName, sheet);
        }

        if (!sheet.TryGetValue(rowId, out var row))
        {
            row = new();
            sheet.Add(rowId, row);
        }

        if (!row.TryGetValue(columnName, out var column))
        {
            var prop = sheetType.GetProperty(columnName);
            if (prop == null || prop.PropertyType != typeof(Lumina.Text.SeString))
                return string.Empty;

            var sheetRow = Service.Data.GetExcelSheet<T>()?.GetRow(rowId);
            if (sheetRow == null)
                return string.Empty;

            var value = (Lumina.Text.SeString?)prop.GetValue(sheetRow);
            if (value == null)
                return string.Empty;

            column = SeString.Parse(value.RawData).ToString();

            if (noPrivateUseCharacters)
                column = Utf8PrivateUseAreaRegex().Replace(column, "");

            row.Add(columnName, column);
        }

        return column;
    }

    internal static void DisposeStringUtils()
    {
        QuestCache.Clear();
        QuestCache = null!;

        AddonCache.Clear();
        AddonCache = null!;

        ObjectCache.Clear();
        ObjectCache = null!;

        SheetCache.Clear();
        SheetCache = null!;
    }
}
