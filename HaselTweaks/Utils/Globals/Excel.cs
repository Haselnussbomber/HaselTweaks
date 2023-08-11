using System.Linq;
using Lumina.Excel;

namespace HaselTweaks.Utils.Globals;

public static class Excel
{
    public static ExcelSheet<T> GetSheet<T>() where T : ExcelRow
        => Service.DataManager.GetExcelSheet<T>()!;

    public static uint GetRowCount<T>() where T : ExcelRow
        => GetSheet<T>().RowCount;

    public static T? GetRow<T>(uint rowId, uint subRowId = uint.MaxValue) where T : ExcelRow
        => GetSheet<T>().GetRow(rowId, subRowId);

    public static T? FindRow<T>(Func<T?, bool> predicate) where T : ExcelRow
        => GetSheet<T>().FirstOrDefault(predicate);
}
