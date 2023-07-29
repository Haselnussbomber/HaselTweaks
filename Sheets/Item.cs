using Dalamud.Utility;
using HaselTweaks.Caches;

namespace HaselTweaks.Sheets;

public class Item : Lumina.Excel.GeneratedSheets.Item
{
    private string? name { get; set; } = null;
    public new string Name
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                name = StringCache.GetItemName(RowId);

            return !string.IsNullOrEmpty(name)
                ? name
                : base.Name.ToDalamudString().ToString() ?? "";
        }
    }

    // see "E8 ?? ?? ?? ?? 85 C0 48 8B 03"
    public bool CanTryOn => !((EquipSlotCategory.Row is 6 or 17) || (EquipSlotCategory.Row is 2 && FilterGroup != 3)); // Waist, SoulCrystal, OffHand Tools?!
}
