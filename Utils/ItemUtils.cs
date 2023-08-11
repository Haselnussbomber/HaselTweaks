using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public static class ItemUtils
{
    public static readonly Lazy<Dictionary<short, (uint Min, uint Max)>> MaxLevelRanges = new(() =>
    {
        var dict = new Dictionary<short, (uint Min, uint Max)>();

        short level = 50;
        foreach (var exVersion in GetSheet<ExVersion>())
        {
            var entry = (Min: uint.MaxValue, Max: 0u);

            foreach (var item in GetSheet<Item>())
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

    public static bool CanTryOn(uint rowId)
    {
        var item = GetRow<Item>(rowId)!;

        // see "E8 ?? ?? ?? ?? 85 C0 48 8B 03"
        return item.EquipSlotCategory.Row switch
        {
            2 when item.FilterGroup != 3 => false, // any OffHand that's not a Shield
            6 => false, // Waist
            17 => false, // SoulCrystal

            _ => true
        };
    }

    public static SeString GetItemLink(uint id)
    {
        var item = GetRow<Item>(id);
        if (item == null)
            return new SeString(new TextPayload($"Item#{id}"));


        var link = SeString.CreateItemLink(item, false);
        // TODO: remove in Dalamud v9
        link.Payloads.Add(UIGlowPayload.UIGlowOff);
        link.Payloads.Add(UIForegroundPayload.UIForegroundOff);
        return link;
    }
}
