namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Method)]
public class SlashCommandAttribute : Attribute
{
    public string Command { get; }
    public string HelpMessage { get; }

    public SlashCommandAttribute(string Command, string HelpMessage = "")
    {
        this.Command = Command;
        this.HelpMessage = HelpMessage;
    }
}
