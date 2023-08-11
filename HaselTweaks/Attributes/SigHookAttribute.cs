namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class SigHookAttribute : Attribute
{
    public SigHookAttribute(string Signature)
    {
        this.Signature = Signature;
    }

    public string Signature { get; }
}
