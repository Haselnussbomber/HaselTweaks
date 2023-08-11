namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselUIModule
{
    [VirtualFunction(58)]
    public readonly partial BannerModule* GetBannerModule();
}
