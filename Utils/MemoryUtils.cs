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
}
