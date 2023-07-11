using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Structs;
using Lumina.Excel;

namespace HaselTweaks.Caches;

public sealed unsafe partial class StringCache
{
    private static Dictionary<uint, string> AddonCache = new();
    private static Dictionary<(NameFormatter.Placeholder, NameFormatter.IdConverter, uint), string> NameCache = new(); // NameCache[(placeholder, idConverter, id)]
    private static Dictionary<(string, uint, string), string> SheetCache = new(); // SheetCache[(sheetName, rowId, columnName)]

    public static string? FormatName(NameFormatter.Placeholder placeholder, NameFormatter.IdConverter idConverter, uint id)
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

    public static string GetItemName(uint id)
        => FormatName(NameFormatter.Placeholder.Item, NameFormatter.IdConverter.Item, id) ?? $"[Item#{id}]";

    public static string GetBNpcName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_BNpcName, id) ?? $"[BNpcName#{id}]";

    public static string GetENpcResidentName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_ENpcResident, id) ?? $"[ENpcResident#{id}]";

    public static string GetTreasureName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Treasure, id) ?? $"[Treasure#{id}]";

    public static string GetAetheryteName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Aetheryte, id) ?? $"[Aetheryte#{id}]";

    public static string GetGatheringPointName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_GatheringPointName, id) ?? $"[GatheringPointName#{id}]";

    public static string GetEObjName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_EObjName, id) ?? $"[EObjName#{id}]";

    public static string GetCompanionName(uint id)
        => FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Companion, id) ?? $"[Companion#{id}]";

    public static string GetTraitName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Trait, id) ?? $"[Trait#{id}]";

    public static string GetActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Action, id) ?? $"[Action#{id}]";

    public static string GetEventActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_EventAction, id) ?? $"[EventAction#{id}]";

    public static string GetGeneralActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_GeneralAction, id) ?? $"[GeneralAction#{id}]";

    public static string GetBuddyActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_BuddyAction, id) ?? $"[BuddyAction#{id}]";

    public static string GetMainCommandName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_MainCommand, id) ?? $"[MainCommand#{id}]";

    public static string GetCraftActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_CraftAction, id) ?? $"[CraftAction#{id}]";

    public static string GetPetActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_PetAction, id) ?? $"[PetAction#{id}]";

    public static string GetCompanyActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_CompanyAction, id) ?? $"[CompanyAction#{id}]";

    public static string GetMountName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Mount, id) ?? $"[Mount#{id}]";

    public static string GetBgcArmyActionName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_BgcArmyAction, id) ?? $"[BgcArmyAction#{id}]";

    public static string GetOrnamentName(uint id)
        => FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Ornament, id) ?? $"[Ornament#{id}]";

    public static string? GetAddonText(uint rowId)
    {
        if (!AddonCache.TryGetValue(rowId, out var value))
        {
            var ptr = (nint)RaptureTextModule.Instance()->GetAddonText(rowId);
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
        AddonCache.Clear();
        AddonCache = null!;

        NameCache.Clear();
        NameCache = null!;

        SheetCache.Clear();
        SheetCache = null!;
    }
}
