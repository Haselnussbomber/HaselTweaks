using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Memory;

namespace HaselTweaks.Utils;

public static unsafe class MemoryUtils
{
    public static byte[] ReplaceRaw(nint address, byte[] data)
    {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);

        MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite, out var oldPermissions);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldPermissions);

        return originalBytes;
    }

    public static byte* FromByteArray(byte[] data)
    {
        var len = data.Length;
        var ptr = Marshal.AllocHGlobal(len + 1);
        Unsafe.InitBlockUnaligned((byte*)ptr, 0, (uint)len + 1);
        Marshal.Copy(data, 0, ptr, len);
        return (byte*)ptr;
    }

    public static byte* FromString(string str)
    {
        var len = Encoding.UTF8.GetByteCount(str) + 1;
        var ptr = Marshal.AllocHGlobal(len);
        Unsafe.InitBlockUnaligned((byte*)ptr, 0, (uint)len);
        MemoryHelper.WriteString(ptr, str);
        return (byte*)ptr;
    }

    public static int Strlen(byte* ptr)
        => Strlen((nint)ptr);

    public static int Strlen(nint ptr)
    {
        int i;
        for (i = 0; *(bool*)(ptr + i); i++) ;
        return i;
    }

    public static byte* Strconcat(params byte*[] ptrs)
    {
        var lengths = new int[ptrs.Length];
        var totalLength = 0;

        for (var i = 0; i < ptrs.Length; i++)
        {
            var len = Strlen(ptrs[i]);
            lengths[i] = len;
            totalLength += len;
        }

        var outPtr = Marshal.AllocHGlobal(totalLength + 1);
        var offset = 0;
        for (var i = 0; i < ptrs.Length; i++)
        {
            var len = lengths[i];
            Buffer.MemoryCopy(ptrs[i], (void*)(outPtr + offset), len, len);
            offset += len;
        }
        *(byte*)(outPtr + totalLength) = 0;
        return (byte*)outPtr;
    }
}
