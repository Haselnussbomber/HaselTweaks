namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselSchedulerActionTimelineManager
{
    [StaticAddress("4C 8B 43 48 48 8B 0D ?? ?? ?? ??", 7)]
    public static partial HaselSchedulerActionTimelineManager* Instance();

    [MemberFunction("48 83 EC 38 48 8B 02 C7 44 24")]
    public readonly partial void PreloadActionTmbByKey(byte** key);
}
