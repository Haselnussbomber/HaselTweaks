using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselUIModule
{
    public static HaselUIModule* Instance() => (HaselUIModule*)UIModule.Instance();

    [VirtualFunction(35)]
    public readonly partial UIModuleVf35Struct* GetVf35Struct(); // I have absolutely no idea
}
