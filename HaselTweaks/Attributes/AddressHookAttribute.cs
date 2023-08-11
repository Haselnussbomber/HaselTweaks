namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class AddressHookAttribute<T> : Attribute
{
    public AddressHookAttribute(string AddressName)
    {
        this.AddressName = AddressName;
    }

    public string AddressName { get; }
}
