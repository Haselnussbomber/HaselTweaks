using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Agents;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = AgentHUD.StructSize)]
public unsafe partial struct HaselAgentHUD
{
    public static HaselAgentHUD* Instance() => (HaselAgentHUD*)AgentHUD.Instance();

    [FieldOffset(0x33B8), FixedSizeArray] internal FixedSizeArray30<TargetInfoTimeRemainingCacheEntry> _targetInfoTimeRemainingCache;

    [MemberFunction("E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 44 39 A7")]
    public partial void UpdateExp(NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray);

    [MemberFunction("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 30 48 8B F9 48 8B 49 10")]
    public partial void UpdateTargetInfo();

    [StructLayout(LayoutKind.Explicit, Size = 8 + 4)]
    public struct TargetInfoTimeRemainingCacheEntry
    {
        [FieldOffset(0x00)] public uint Icon;
        [FieldOffset(0x04)] public uint TimeRemaining;
        [FieldOffset(0x08)] public byte Unk8;
        [FieldOffset(0x09)] public byte HasTimeRemaining;
        [FieldOffset(0x0A)] public byte ShouldClearTimeRemaining; // temporary value for some crafter statuses
    }
}
