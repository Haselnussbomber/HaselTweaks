using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselTweaks.Extensions;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public static class Colors
{
    public static ImColor Transparent { get; } = Vector4.Zero;
    public static ImColor White { get; } = Vector4.One;
    public static ImColor Black { get; } = new(0, 0, 0);
    public static ImColor Orange { get; } = new(1, 0.6f, 0);
    public static ImColor Gold { get; } = new(0.847f, 0.733f, 0.49f);
    public static ImColor Green { get; } = new(0, 1, 0);
    public static ImColor Yellow { get; } = new(1, 1, 0);
    public static ImColor Red { get; } = new(1, 0, 0);
    public static ImColor Grey { get; } = new(0.73f, 0.73f, 0.73f);
    public static ImColor Grey2 { get; } = new(0.87f, 0.87f, 0.87f);
    public static ImColor Grey3 { get; } = new(0.6f, 0.6f, 0.6f);
    public static ImColor Grey4 { get; } = new(0.3f, 0.3f, 0.3f);

    private static readonly Lazy<Dictionary<byte, ImColor>> ItemRarityColors = new(() =>
    {
        var uiColorSheet = Service.DataManager.GetExcelSheet<UIColor>()!;
        return Service.DataManager.GetExcelSheet<Item>()!
            .Where(item => !string.IsNullOrEmpty(item.Name.ToDalamudString().ToString()))
            .Select(item => item.Rarity)
            .Distinct()
            .Select(rarity => (Rarity: rarity, Color: (ImColor)uiColorSheet.GetRow(547u + rarity * 2u)!.UIForeground.Reverse()))
            .ToDictionary(tuple => tuple.Rarity, tuple => tuple.Color);
    });

    public static ImColor GetItemRarityColor(byte rarity) => ItemRarityColors.Value[rarity];

    public static ImColor GetStainColor(uint id)
    {
        var col = (ImColor)(Service.DataManager.GetExcelSheet<Stain>()!.GetRow(id)!.Color.Reverse() >> 8);
        col.A = 1;
        return col;
    }

    public static unsafe ImColor GetItemLevelColor(byte classJob, Item item, params Vector4[] colors)
    {
        if (colors.Length < 2)
            throw new ArgumentException("At least two colors are required for interpolation.");

        var jobIndex = Service.DataManager.GetExcelSheet<ClassJob>()?.GetRow(classJob)?.DohDolJobIndex;
        if (jobIndex == null)
            return White;

        var level = PlayerState.Instance()->ClassJobLevelArray[(short)jobIndex];

        if (!ItemUtils.MaxLevelRanges.Value.TryGetValue(level, out var range))
            return White;

        var itemLevel = item.LevelItem.Row;

        // special case for Fisher's Secondary Tool
        // which has only one item, Spearfishing Gig
        if (item.ItemUICategory.Row == 99)
            return itemLevel == 180 ? Green : Red;

        if (itemLevel < range.Min)
            return Red;

        var value = (itemLevel - range.Min) / (float)(range.Max - range.Min);

        var startIndex = (int)(value * (colors.Length - 1));
        var endIndex = Math.Min(startIndex + 1, colors.Length - 1);
        var t = value * (colors.Length - 1) - startIndex;
        return (ImColor)Vector4.Lerp(colors[startIndex], colors[endIndex], t);
    }
}
