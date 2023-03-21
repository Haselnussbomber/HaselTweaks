using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public sealed unsafe partial class StringUtils : IDisposable
{
    [GeneratedRegex("^[\\ue000-\\uf8ff]+ ")]
    private static partial Regex Utf8PrivateUseAreaRegex();

    private static Dictionary<uint, string> ENpcResidentNameCache = new();
    private static Dictionary<uint, string> EObjNameCache = new();
    private static Dictionary<uint, string> ItemNameCache = new();
    private static Dictionary<uint, string> QuestCache = new();
    private static Dictionary<uint, string> AddonCache = new();
    private static Dictionary<string, Dictionary<uint, Dictionary<string, string>>> SheetCache = new(); // SheetCache[sheetName][rowId][columnName]

    public StringUtils()
    {
        SignatureHelper.Initialise(this);
    }

    [Signature("E9 ?? ?? ?? ?? 48 8D 47 30")]
    private readonly FormatObjectStringDelegate FormatObjectString = null!; // how do you expect me to name things i have no clue about
    private delegate nint FormatObjectStringDelegate(int mode, uint id, uint idConversionMode, uint a4);

    public string GetENpcResidentName(uint npcId)
    {
        if (!ENpcResidentNameCache.TryGetValue(npcId, out var value))
        {
            var ptr = FormatObjectString(0, npcId, 3, 1);
            value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();
            ENpcResidentNameCache.Add(npcId, value);
        }

        return value;
    }

    public string GetEObjName(uint objId)
    {
        if (!EObjNameCache.TryGetValue(objId, out var value))
        {
            var ptr = FormatObjectString(0, objId, 5, 1);
            value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();
            EObjNameCache.Add(objId, value);
        }

        return value;
    }

    public string GetItemName(uint itemId)
    {
        if (!ItemNameCache.TryGetValue(itemId, out var value))
        {
            var ptr = FormatObjectString(1, itemId, 5, 1);
            value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();
            ItemNameCache.Add(itemId, value);
        }

        return value;
    }

    public static string GetQuestName(uint questId, bool clean)
    {
        if (!QuestCache.TryGetValue(questId, out var value))
        {
            var quest = Service.Data.GetExcelSheet<Quest>()?.GetRow(questId);
            if (quest == null)
                return string.Empty;

            value = SeString.Parse(quest.Name.RawData).ToString();

            if (clean)
                value = Utf8PrivateUseAreaRegex().Replace(value, "");

            QuestCache.Add(questId, value);
        }

        return value;
    }

    public static string? GetAddonText(uint rowId)
    {
        if (!AddonCache.TryGetValue(rowId, out var value))
        {
            var ptr = (nint)Framework.Instance()->GetUiModule()->GetRaptureTextModule()->GetAddonText(rowId);
            value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();

            if (string.IsNullOrWhiteSpace(value))
                return null;

            AddonCache.Add(rowId, value);
        }

        return value;
    }

    public static string GetSheetText<T>(uint rowId, string columnName) where T : ExcelRow
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
            row.Add(columnName, column);
        }

        return column;
    }

    void IDisposable.Dispose()
    {
        ENpcResidentNameCache = null!;
        EObjNameCache = null!;
        ItemNameCache = null!;
        QuestCache = null!;
        AddonCache = null!;
        SheetCache = null!;
    }
}
