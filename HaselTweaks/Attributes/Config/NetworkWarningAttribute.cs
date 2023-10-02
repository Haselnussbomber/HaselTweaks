using Dalamud.Interface;
using HaselCommon.Utils;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class NetworkWarningAttribute : ConfigInfoAttribute
{
    public NetworkWarningAttribute() : base("HaselTweaks.Config.NetworkRequestWarning")
    {
        Icon = FontAwesomeIcon.Bolt;
        Color = Colors.Yellow;
    }
}
