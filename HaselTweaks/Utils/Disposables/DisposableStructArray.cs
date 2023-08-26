using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Utils;

// based on FFXIVClientStructs.Interop.Pointer
public unsafe class DisposableStructArray<T> : IDisposable where T : unmanaged
{
    public T* Ptr { get; private set; }
    public int Count { get; }

    public DisposableStructArray(int count = 1, IMemorySpace* memorySpace = null)
    {
        Count = count;

        if (memorySpace == null)
            memorySpace = IMemorySpace.GetDefaultSpace();

        Ptr = (T*)memorySpace->Malloc((ulong)sizeof(T) * (uint)count, 8);
    }

    public static implicit operator T*(DisposableStructArray<T> p)
        => p.Ptr;

    public void Dispose()
    {
        if (Ptr == null)
            return;

        IMemorySpace.Free(Ptr);
        Ptr = null;

        GC.SuppressFinalize(this);
    }

    public T* this[int index]
        => index < 0 || index >= Count
            ? throw new IndexOutOfRangeException()
            : &Ptr[index];
}
