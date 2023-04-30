using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselTweaks.Structs;
using Lumina.Excel;
using NameFormatterIdConverter = HaselTweaks.Structs.NameFormatter.IdConverter;
using NameFormatterReplacer = HaselTweaks.Structs.NameFormatter.Replacer;

namespace HaselTweaks.Caches;

public sealed unsafe partial class StringCache
{
    private static Dictionary<uint, string> QuestCache = new();
    private static Dictionary<uint, string> AddonCache = new();
    private static Dictionary<(NameFormatterReplacer, NameFormatterIdConverter, uint), string> NameCache = new(); // NameCache[(replacer, idConverter, id)]
    private static Dictionary<(string, uint, string), string> SheetCache = new(); // SheetCache[(sheetName, rowId, columnName)]

    public static string FormatName(NameFormatterReplacer replacer, NameFormatterIdConverter idConverter, uint id)
    {
        var key = (replacer, idConverter, id);

        if (!NameCache.TryGetValue(key, out var value))
        {
            var ptr = NameFormatter.Format(replacer, id, idConverter, 1);
            if (ptr != 0)
            {
                value = MemoryHelper.ReadSeStringNullTerminated(ptr).ToString();
                NameCache.Add(key, value);
            }
        }

        return value ?? $"[{Enum.GetName(typeof(NameFormatterIdConverter), idConverter)}#{id}]";
    }

    public static string GetItemName(uint id)
        => FormatName(NameFormatterReplacer.Item, NameFormatterIdConverter.None, id);

    public static string GetActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.Action, id);

    public static string GetBNpcName(uint id)
        => FormatName(NameFormatterReplacer.ObjStr, NameFormatterIdConverter.BNpc, id);

    public static string GetENpcResidentName(uint id)
        => FormatName(NameFormatterReplacer.ObjStr, NameFormatterIdConverter.ENpcResident, id);

    public static string GetEventActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.EventAction, id);

    public static string GetEObjName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.EObj, id);

    public static string GetGatheringPointName(uint id)
        => FormatName(NameFormatterReplacer.ObjStr, NameFormatterIdConverter.GatheringPoint, id);

    public static string GetMainCommandName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.MainCommand, id);

    public static string GetCompanionName(uint id)
        => FormatName(NameFormatterReplacer.ObjStr, NameFormatterIdConverter.Companion, id);

    public static string GetCraftActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.CraftAction, id);

    public static string GetPetActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.PetAction, id);

    public static string GetCompanyActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.CompanyAction, id);

    public static string GetMountName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.Mount, id);

    public static string GetBgcArmyActionName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.BgcArmyAction, id);

    public static string GetOrnamentName(uint id)
        => FormatName(NameFormatterReplacer.ActStr, NameFormatterIdConverter.Ornament, id);

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

    public static string GetSheetText<T>(uint rowId, string columnName) where T : ExcelRow
    {
        var sheetType = typeof(T);
        var sheetName = sheetType.Name;
        var key = (sheetName, rowId, columnName);

        if (!SheetCache.TryGetValue(key, out var value))
        {

            var prop = sheetType.GetProperty(columnName);
            if (prop == null || prop.PropertyType != typeof(Lumina.Text.SeString))
                return string.Empty;

            var sheetRow = Service.Data.GetExcelSheet<T>()?.GetRow(rowId);
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

    internal static void Dispose()
    {
        QuestCache.Clear();
        QuestCache = null!;

        AddonCache.Clear();
        AddonCache = null!;

        NameCache.Clear();
        NameCache = null!;

        SheetCache.Clear();
        SheetCache = null!;
    }
}
