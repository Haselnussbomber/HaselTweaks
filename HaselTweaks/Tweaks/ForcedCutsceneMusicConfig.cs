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
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Restore", ref _config.Restore);
        ImGuiUtils.DrawPaddedSeparator();
        _configGui.DrawBool("HandleMaster", ref _config.HandleMaster);
        _configGui.DrawBool("HandleBgm", ref _config.HandleBgm);
        _configGui.DrawBool("HandleSe", ref _config.HandleSe);
        _configGui.DrawBool("HandleVoice", ref _config.HandleVoice);
        _configGui.DrawBool("HandleEnv", ref _config.HandleEnv);
        _configGui.DrawBool("HandleSystem", ref _config.HandleSystem);
        _configGui.DrawBool("HandlePerform", ref _config.HandlePerform);
    }
}
