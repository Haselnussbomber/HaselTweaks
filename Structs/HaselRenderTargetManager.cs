using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselRenderTargetManager
{
    [StaticAddress("48 8B 05 ?? ?? ?? ?? 48 8B 90 ?? ?? ?? ?? EB 03 49 8B D0", isPointer: true)]
    public static partial HaselRenderTargetManager* Instance();

    [MemberFunction("48 8B 05 ?? ?? ?? ?? 8B CA 48 8B 84 C8 ?? ?? ?? ??")]
    public partial Texture* GetCharaViewTexture(uint id);
}
