using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RenderTargetManager
{
    [StaticAddress("48 8B 0D ?? ?? ?? ?? 48 8B B1", 3, isPointer: true)]
    public static partial RenderTargetManager* Instance();

    [MemberFunction("48 8B 05 ?? ?? ?? ?? 8B CA 48 8B 84 C8")]
    public readonly partial Texture* GetCharaViewTexture(uint id);
}
