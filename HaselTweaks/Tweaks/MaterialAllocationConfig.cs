using HaselTweaks.Config;

namespace HaselTweaks.Tweaks;

public class MaterialAllocationConfiguration
{
    public bool SaveLastSelectedTab = true;
    public byte LastSelectedTab = 2;
    public bool OpenGatheringLogOnItemClick = true;
}

public unsafe partial class MaterialAllocation
{
    public void OnConfigOpen() { }

    public void OnConfigChange(string fieldName) { }

    public void OnConfigClose() { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("SaveLastSelectedTab", ref Config.SaveLastSelectedTab);
        ConfigGui.DrawBool("OpenGatheringLogOnItemClick", ref Config.OpenGatheringLogOnItemClick, noFixSpaceAfter: true);
    }
}
