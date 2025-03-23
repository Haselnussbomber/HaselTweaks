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
    private ForcedCutsceneMusicConfiguration Config => _pluginConfig.Tweaks.ForcedCutsceneMusic;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Restore", ref Config.Restore);
        ImGuiUtils.DrawPaddedSeparator();
        _configGui.DrawBool("HandleMaster", ref Config.HandleMaster);
        _configGui.DrawBool("HandleBgm", ref Config.HandleBgm);
        _configGui.DrawBool("HandleSe", ref Config.HandleSe);
        _configGui.DrawBool("HandleVoice", ref Config.HandleVoice);
        _configGui.DrawBool("HandleEnv", ref Config.HandleEnv);
        _configGui.DrawBool("HandleSystem", ref Config.HandleSystem);
        _configGui.DrawBool("HandlePerform", ref Config.HandlePerform);
    }
}
