using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public static class ItemUtils
{
    public static readonly Lazy<Dictionary<short, (uint Min, uint Max)>> MaxLevelRanges = new(() =>
    {
        var dict = new Dictionary<short, (uint Min, uint Max)>();

        short level = 50;
        foreach (var exVersion in Service.Data.GetExcelSheet<ExVersion>()!)
        {
            var entry = (Min: uint.MaxValue, Max: 0u);

            foreach (var item in Service.Data.GetExcelSheet<Item>()!)
            {
                if (item.LevelEquip != level || item.LevelItem.Row <= 1)
                    continue;

                if (entry.Min > item.LevelItem.Row)
                    entry.Min = item.LevelItem.Row;

                if (entry.Max < item.LevelItem.Row)
                    entry.Max = item.LevelItem.Row;
            }

            dict.Add(level, entry);
            level += 10;
        }

        return dict;
    });
}
