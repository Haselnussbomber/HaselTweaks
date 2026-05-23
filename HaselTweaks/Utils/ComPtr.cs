using System.Runtime.CompilerServices;
using Windows.Win32.System.Com;

namespace HaselTweaks.Utils;

[StructLayout(LayoutKind.Sequential, Size = 0x8)]
public unsafe struct ComPtr<T> : IDisposable where T : unmanaged
{
    public T* Pointer { get; set; }

    private ComPtr(T* p)
    {
        Pointer = p;
    }

    public readonly bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Pointer == null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get()
    {
        return Pointer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T** GetAddressOf()
    {
        return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
    }

    public void Dispose()
    {
        Release();
    }

    public void Release()
    {
        if (Pointer != null)
        {
            ((IUnknown*)Pointer)->Release();
            Pointer = null;
        }
    }

    public TNew* Cast<TNew>() where TNew : unmanaged
    {
        return (TNew*)Pointer;
    }

    public static implicit operator T*(ComPtr<T> p) => p.Pointer;
    public static implicit operator ComPtr<T>(T* p) => new(p);
}
