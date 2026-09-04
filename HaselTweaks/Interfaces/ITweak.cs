using System.Threading.Tasks;

namespace HaselTweaks.Interfaces;

public interface ITweak : IAsyncDisposable
{
    string InternalName { get; }
    TweakStatus Status { get; set; }
    bool IsObsolete { get; set; }
    ValueTask OnEnable();
    ValueTask OnDisable();
}
