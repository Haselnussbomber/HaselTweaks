namespace HaselTweaks.Tweaks;

public class ForcedCutsceneMusicConfiguration
{
    public bool Restore = true;
}

public unsafe partial class ForcedCutsceneMusic
{
    private ForcedCutsceneMusicConfiguration Config => PluginConfig.Tweaks.ForcedCutsceneMusic;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("Restore", ref Config.Restore);
    }
}
