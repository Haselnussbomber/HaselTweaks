using HaselCommon.Caching;
using HaselCommon.Services;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Caches;

public class LevelObjectCache(ExcelService ExcelService) : MemoryCache<uint, Level>
{
    public override Level? CreateEntry(uint objectId)
        => ExcelService.FindRow<Level>(row => row?.Object == objectId);
}
