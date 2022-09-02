namespace HaselTweaks;

public static partial class Resolver
{
    private static bool Initialized;

    public static void Initialize()
    {
        if (Initialized) return;

        InitializeMemberFunctions(Service.SigScanner);
        InitializeStaticAddresses(Service.SigScanner);

        Initialized = true;
    }
}
