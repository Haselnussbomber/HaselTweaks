using HaselTweaks.Records.PortraitHelper;

namespace HaselTweaks.Tweaks;

public class PortraitHelperConfiguration
{
    public List<SavedPreset> Presets = [];
    public List<SavedPresetTag> PresetTags = [];
    public bool ShowAlignmentTool = false;
    public int AlignmentToolVerticalLines = 2;
    public Vector4 AlignmentToolVerticalColor = new(0, 0, 0, 1f);
    public int AlignmentToolHorizontalLines = 2;
    public Vector4 AlignmentToolHorizontalColor = new(0, 0, 0, 1f);

    public bool EmbedPresetStringInThumbnails = true;
    public bool NotifyGearChecksumMismatch = true;
    public bool ReequipGearsetOnUpdate = false;
    public bool AutoUpdatePotraitOnGearUpdate = false;
    public bool IgnoreDoHDoL = false;
}

public unsafe partial class PortraitHelper
{
    private PortraitHelperConfiguration Config => _pluginConfig.Tweaks.PortraitHelper;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EmbedPresetStringInThumbnails", ref Config.EmbedPresetStringInThumbnails);
        _configGui.DrawBool("NotifyGearChecksumMismatch", ref Config.NotifyGearChecksumMismatch, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.NotifyGearChecksumMismatch);
            _configGui.DrawBool("IgnoreDoHDoL", ref Config.IgnoreDoHDoL);
        });
        _configGui.DrawBool("ReequipGearsetOnUpdate", ref Config.ReequipGearsetOnUpdate, drawAfterLabel: _configGui.DrawNetworkWarning);
        _configGui.DrawBool("AutoUpdatePotraitOnGearUpdate", ref Config.AutoUpdatePotraitOnGearUpdate, drawAfterLabel: _configGui.DrawNetworkWarning);
    }
}
