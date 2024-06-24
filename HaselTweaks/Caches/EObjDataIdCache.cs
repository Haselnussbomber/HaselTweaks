using HaselCommon.Caching;
using HaselCommon.Services;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Caches;

public class EObjDataIdCache(ExcelService ExcelService) : MemoryCache<uint, EObj>
{
    public override EObj? CreateEntry(uint dataId)
        => ExcelService.FindRow<EObj>(row => row?.Data == dataId);
}
