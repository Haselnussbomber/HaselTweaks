namespace HaselTweaks.Utils;

public ref struct DisposableAction(Action action) : IDisposable
{
    public void Dispose()
    {
        action();
    }
}
