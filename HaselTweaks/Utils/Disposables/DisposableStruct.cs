using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Utils;

// based on FFXIVClientStructs.Interop.Pointer
public unsafe class DisposableStruct<T> : IDisposable where T : unmanaged
{
    public T* Ptr { get; private set; }

    public DisposableStruct(IMemorySpace* memorySpace = null)
    {
        if (memorySpace == null)
            memorySpace = IMemorySpace.GetDefaultSpace();

        Ptr = (T*)memorySpace->Malloc<T>();
    }

    public DisposableStruct(T* value)
    {
        Ptr = value;
    }

    public static implicit operator T*(DisposableStruct<T> p)
        => p.Ptr;

    public static implicit operator DisposableStruct<T>(T* p)
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
