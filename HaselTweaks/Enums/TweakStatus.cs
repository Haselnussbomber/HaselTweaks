namespace HaselTweaks.Enums;

public enum TweakStatus
{
    Disabled,
    Enabled,
    Error,
    Outdated,
    Disposed
}

public static class TweakStatusExtensions
{
    extension(TweakStatus status)
    {
        public string GetTranslateKey()
        {
            return "HaselTweaks.Config.TweakStatus." + status.GetName();
        }

        public string GetName()
        {
            return status switch
            {
                TweakStatus.Disabled => nameof(TweakStatus.Disabled),
                TweakStatus.Enabled => nameof(TweakStatus.Enabled),
                TweakStatus.Error => nameof(TweakStatus.Error),
                TweakStatus.Outdated => nameof(TweakStatus.Outdated),
                TweakStatus.Disposed => nameof(TweakStatus.Disposed),
                _ => status.ToString(),
            };
        }

        public Color GetColor()
        {
            return status switch
            {
                TweakStatus.Error or TweakStatus.Outdated => Color.Red,
                TweakStatus.Enabled => Color.Green,
                _ => Color.Text600
            };
        }
    }
}
