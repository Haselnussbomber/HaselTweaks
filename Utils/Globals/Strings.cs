using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using HaselTweaks.Structs;
using Lumina.Excel;

namespace HaselTweaks.Utils.Globals;

public static unsafe class Strings
{
    public static string t(string key)
        => Service.TranslationManager.Translate(key);

    public static string t(string key, params object?[] args)
        => Service.TranslationManager.Translate(key, args);

    public static SeString tSe(string key, params SeString[] args)
        => Service.TranslationManager.TranslateSeString(key, args.Select(s => s.Payloads).ToArray());

    public static string GetAddonText(uint id)
        => Service.StringManager.GetAddonText(id);

    public static string GetSheetText<T>(uint rowId, string columnName) where T : ExcelRow
        => Service.StringManager.GetSheetText<T>(rowId, columnName);

    public static string GetItemName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.Item, NameFormatter.IdConverter.Item, id) ?? $"[Item#{id}]";

    public static string GetBNpcName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_BNpcName, id) ?? $"[BNpcName#{id}]";

    public static string GetENpcResidentName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_ENpcResident, id) ?? $"[ENpcResident#{id}]";

    public static string GetTreasureName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Treasure, id) ?? $"[Treasure#{id}]";

    public static string GetAetheryteName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Aetheryte, id) ?? $"[Aetheryte#{id}]";

    public static string GetGatheringPointName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_GatheringPointName, id) ?? $"[GatheringPointName#{id}]";

    public static string GetEObjName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_EObjName, id) ?? $"[EObjName#{id}]";

    public static string GetCompanionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ObjStr, NameFormatter.IdConverter.ObjStr_Companion, id) ?? $"[Companion#{id}]";

    public static string GetTraitName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Trait, id) ?? $"[Trait#{id}]";

    public static string GetActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Action, id) ?? $"[Action#{id}]";

    public static string GetEventActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_EventAction, id) ?? $"[EventAction#{id}]";

    public static string GetGeneralActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_GeneralAction, id) ?? $"[GeneralAction#{id}]";

    public static string GetBuddyActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_BuddyAction, id) ?? $"[BuddyAction#{id}]";

    public static string GetMainCommandName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_MainCommand, id) ?? $"[MainCommand#{id}]";

    public static string GetCraftActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_CraftAction, id) ?? $"[CraftAction#{id}]";

    public static string GetPetActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_PetAction, id) ?? $"[PetAction#{id}]";

    public static string GetCompanyActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_CompanyAction, id) ?? $"[CompanyAction#{id}]";

    public static string GetMountName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Mount, id) ?? $"[Mount#{id}]";

    public static string GetBgcArmyActionName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_BgcArmyAction, id) ?? $"[BgcArmyAction#{id}]";

    public static string GetOrnamentName(uint id)
        => Service.StringManager.FormatName(NameFormatter.Placeholder.ActStr, NameFormatter.IdConverter.ActStr_Ornament, id) ?? $"[Ornament#{id}]";
}
