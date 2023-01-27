using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// not sure about the name, because i don't know what it actually is
[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkCollisionManager
{
    private static nint instance;
    public static AtkCollisionManager* Instance
    {
        get
        {
            if (instance == 0)
                instance = *(nint*)((IntPtr)AtkStage.GetSingleton() + 0x30);
            return (AtkCollisionManager*)instance;
        }
    }

    [FieldOffset(0x08)] public AtkUnitBase* IntersectingAddon;
    [FieldOffset(0x10)] public AtkCollisionNode* IntersectingCollisionNode;
}
