using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Utils;

// based on FFXIVClientStructs.Interop.Pointer
public unsafe class DisposableCreatable<T> : IDisposable where T : unmanaged, ICreatable
{
    public T* Ptr { get; private set; }

    public DisposableCreatable(IMemorySpace* memorySpace = null)
    {
        if (memorySpace == null)
            memorySpace = IMemorySpace.GetDefaultSpace();

        Ptr = memorySpace->Create<T>();
    }

    public DisposableCreatable(T* value)
    {
        Ptr = value;
    }

    public static implicit operator T*(DisposableCreatable<T> p)
        => p.Ptr;

    public static implicit operator DisposableCreatable<T>(T* p)
        => new(p);

    public void Dispose()
    {
        if (Ptr == null)
            return;

        IMemorySpace.Free(Ptr);
        Ptr = null;

        GC.SuppressFinalize(this);
    }
}
