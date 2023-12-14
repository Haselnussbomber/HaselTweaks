namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAttribute(string Command, string? ConfigFieldName = null) : Attribute
{
    public string Command { get; } = Command;
    public string? ConfigFieldName { get; } = ConfigFieldName;
}
