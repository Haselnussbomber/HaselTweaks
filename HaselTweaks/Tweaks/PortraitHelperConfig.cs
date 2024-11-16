using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
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
    private PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("EmbedPresetStringInThumbnails", ref Config.EmbedPresetStringInThumbnails);
        ConfigGui.DrawBool("NotifyGearChecksumMismatch", ref Config.NotifyGearChecksumMismatch, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.NotifyGearChecksumMismatch);
            ConfigGui.DrawBool("IgnoreDoHDoL", ref Config.IgnoreDoHDoL);
        });
        ConfigGui.DrawBool("ReequipGearsetOnUpdate", ref Config.ReequipGearsetOnUpdate, drawAfterLabel: ConfigGui.DrawNetworkWarning);
        //ConfigGui.DrawBool("AutoUpdatePotraitOnGearUpdate", ref Config.AutoUpdatePotraitOnGearUpdate, drawAfterLabel: ConfigGui.DrawNetworkWarning);
    }
}
