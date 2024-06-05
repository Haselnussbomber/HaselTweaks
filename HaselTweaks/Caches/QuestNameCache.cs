using System.Text.RegularExpressions;
using HaselCommon.Interfaces;
using HaselCommon.Utils;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Caches;

public partial class QuestNameCache : Cache<uint, string>, ILocalizedCache
{
    [GeneratedRegex("^[\\ue000-\\uf8ff]+ ")]
    private static partial Regex Utf8PrivateUseAreaRegex();

    protected override string? CreateValue(uint questId)
        => Utf8PrivateUseAreaRegex().Replace(GetSheetText<Quest>(questId, "Name"), "");
}
