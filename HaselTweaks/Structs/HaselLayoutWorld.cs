using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x228)]
public unsafe partial struct HaselLayoutWorld
{
    public static HaselLayoutWorld* Instance() => (HaselLayoutWorld*)LayoutWorld.Instance();

    [MemberFunction("E8 ?? ?? ?? ?? 45 33 F6 44 89 B7")]
    public partial void UnloadPrefetchLayout();

    [MemberFunction("48 89 6C 24 ?? 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 41 20"), GenerateStringOverloads]
    public partial void LoadPrefetchLayout(int type, byte* bgName, byte layerEntryType, uint levelId, uint territoryTypeId, GameMain* gameMain, uint cfcId);
}
