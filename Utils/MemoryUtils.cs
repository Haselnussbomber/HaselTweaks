using Dalamud.Memory;

namespace HaselTweaks.Utils;

public static class MemoryUtils
{
    public static byte[] ReplaceRaw(IntPtr address, byte[] data)
    {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);

        var oldProtection = MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldProtection);

        return originalBytes;
    }
}
