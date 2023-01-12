using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RenderTargetManager
{
    [StaticAddress("48 8B B8 ?? ?? ?? ?? 41 83 F9 1E", isPointer: true)]
    public static partial RenderTargetManager* Instance();

    [MemberFunction("48 8B 05 ?? ?? ?? ?? 8B CA 48 8B 84 C8")]
    public partial Texture* GetCharaViewTexture(uint id);
}
