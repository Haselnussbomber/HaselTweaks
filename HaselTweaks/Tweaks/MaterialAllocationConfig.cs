namespace HaselTweaks.Tweaks;

public class MaterialAllocationConfiguration
{
    public byte LastSelectedTab = 2;
}

public unsafe partial class MaterialAllocation
{
    public void OnConfigOpen() { }
    public void OnConfigChange(string fieldName) { }
    public void OnConfigClose() { }
    public void DrawConfig() { }
}
