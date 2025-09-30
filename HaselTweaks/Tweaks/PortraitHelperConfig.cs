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
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EmbedPresetStringInThumbnails", ref _config.EmbedPresetStringInThumbnails);
        _configGui.DrawBool("NotifyGearChecksumMismatch", ref _config.NotifyGearChecksumMismatch, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!_config.NotifyGearChecksumMismatch);
            _configGui.DrawBool("IgnoreDoHDoL", ref _config.IgnoreDoHDoL);
        });
        _configGui.DrawBool("ReequipGearsetOnUpdate", ref _config.ReequipGearsetOnUpdate, drawAfterLabel: _configGui.DrawNetworkWarning);
        _configGui.DrawBool("AutoUpdatePotraitOnGearUpdate", ref _config.AutoUpdatePotraitOnGearUpdate, drawAfterLabel: _configGui.DrawNetworkWarning);
    }
}
