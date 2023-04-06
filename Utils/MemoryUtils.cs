using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Memory;

namespace HaselTweaks.Utils;

public static unsafe class MemoryUtils
{
    public static byte[] ReplaceRaw(nint address, byte[] data)
    {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);

        var oldProtection = MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldProtection);

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

    public static int strlen(byte* ptr)
        => strlen((nint)ptr);

    public static int strlen(nint ptr)
    {
        int i;
        for (i = 0; *(bool*)(ptr + i); i++) ;
        return i;
    }

    public static byte* strconcat(params byte*[] ptrs)
    {
        var lengths = new int[ptrs.Length];
        var totalLength = 0;

        for (var i = 0; i < ptrs.Length; i++)
        {
            var len = strlen(ptrs[i]);
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
