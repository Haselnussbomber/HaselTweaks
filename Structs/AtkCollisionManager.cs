using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// not sure about the name, because i don't know what it actually is
[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkCollisionManager
{
    public static AtkCollisionManager* Instance => *(AtkCollisionManager**)((IntPtr)AtkStage.GetSingleton() + 0x30);

    [FieldOffset(0x08)] public AtkUnitBase* IntersectingAddon;
    [FieldOffset(0x10)] public AtkCollisionNode* IntersectingCollisionNode;
}
