namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 1)]
public unsafe partial struct HaselAgentGearSet
{
    [MemberFunction("48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 8B DA 48 8B 49 10 48 8B 01 FF 50 70 4C 8D 44 24")]
    public partial void OpenBannerEditorForGearset(int gearsetId);
}
