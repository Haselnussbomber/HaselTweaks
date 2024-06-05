using HaselCommon.Utils;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Caches;

public class EObjDataIdCache : Cache<uint, EObj>
{
    protected override EObj? CreateValue(uint dataId)
        => FindRow<EObj>(row => row?.Data == dataId);
}
