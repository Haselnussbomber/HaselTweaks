using System.Collections.Generic;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
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

    // TODO(v9): replace with SeString.CreateItemLink
    public static SeString GetItemLink(uint itemId, ItemPayload.ItemKind kind = ItemPayload.ItemKind.Normal)
    {
        string? displayName;
        var rarity = 1; // default: white
        switch (kind)
        {
            case ItemPayload.ItemKind.Normal:
            case ItemPayload.ItemKind.Collectible:
            case ItemPayload.ItemKind.Hq:
                var item = GetRow<Item>(itemId)!;
                displayName = item.Name.ToDalamudString().TextValue;
                rarity = item.Rarity;
                break;
            case ItemPayload.ItemKind.EventItem:
                displayName = GetRow<EventItem>(itemId)?.Name.ToDalamudString().TextValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        if (displayName == null)
        {
            throw new Exception("Invalid item ID specified, could not determine item name.");
        }

        if (kind == ItemPayload.ItemKind.Hq)
        {
            displayName += $" {(char)SeIconChar.HighQuality}";
        }
        else if (kind == ItemPayload.ItemKind.Collectible)
        {
            displayName += $" {(char)SeIconChar.Collectible}";
        }

        var textColor = (ushort)(549 + (rarity - 1) * 2);
        var textGlowColor = (ushort)(textColor + 1);

        var sb = new SeStringBuilder()
            .AddUiForeground(textColor)
            .AddUiGlow(textGlowColor)
            .Add(new ItemPayload(itemId, kind));

        sb.BuiltString.Payloads.AddRange(TextArrowPayloads);

        return sb.AddText(displayName)
            .AddUiGlowOff()
            .AddUiForegroundOff()
            .Add(RawPayload.LinkTerminator)
            .Build();
    }

    // TODO(v9): remove
    private static IEnumerable<Payload> TextArrowPayloads
    {
        get
        {
            var markerSpace = Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => " ",
                ClientLanguage.French => " ",
                _ => string.Empty,
            };
            return new List<Payload>
            {
                new UIForegroundPayload(500),
                new UIGlowPayload(501),
                new TextPayload($"{(char)SeIconChar.LinkMarker}{markerSpace}"),
                UIGlowPayload.UIGlowOff,
                UIForegroundPayload.UIForegroundOff,
            };
        }
    }
}
