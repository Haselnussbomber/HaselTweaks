using HaselCommon.Gui;

namespace HaselTweaks.Tweaks;

public class ForcedCutsceneMusicConfiguration
{
    public bool Restore = true;
    public bool HandleMaster = true;
    public bool HandleBgm = true;
    public bool HandleSe = true;
    public bool HandleVoice = true;
    public bool HandleEnv = true;
    public bool HandleSystem = false;
    public bool HandlePerform = false;
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
        ImGuiUtils.DrawPaddedSeparator();
        ConfigGui.DrawBool("HandleMaster", ref Config.HandleMaster);
        ConfigGui.DrawBool("HandleBgm", ref Config.HandleBgm);
        ConfigGui.DrawBool("HandleSe", ref Config.HandleSe);
        ConfigGui.DrawBool("HandleVoice", ref Config.HandleVoice);
        ConfigGui.DrawBool("HandleEnv", ref Config.HandleEnv);
        ConfigGui.DrawBool("HandleSystem", ref Config.HandleSystem);
        ConfigGui.DrawBool("HandlePerform", ref Config.HandlePerform);
    }
}
