namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class VTableHookAttribute<T> : Attribute
{
    public VTableHookAttribute(int VTableIndex)
    {
        this.VTableIndex = VTableIndex;
    }

    public int VTableIndex { get; }
}
