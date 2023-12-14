namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAttribute(string Command, string HelpMessageKey, string? ConfigFieldName = null) : Attribute
{
    public string Command { get; } = Command;
    public string HelpMessage { get; } = t(HelpMessageKey);
    public string? ConfigFieldName { get; } = ConfigFieldName;
}
