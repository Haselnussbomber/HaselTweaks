using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

public class EnhancedMaterialListConfiguration
{
    public bool EnableZoneNames = true;
    public bool DisableZoneNameForCrystals = true;
    public bool ClickToOpenMap = true;
    public bool DisableClickToOpenMapForCrystals = true;
    public bool AutoRefreshMaterialList = true;
    public bool AutoRefreshRecipeTree = true;
    public bool RestoreMaterialList = true;
    public uint RestoreMaterialListRecipeId = 0;
    public uint RestoreMaterialListAmount = 0;
    public bool AddSearchForItemByCraftingMethodContextMenuEntry = true; // yep, i spelled it out
}

public unsafe partial class EnhancedMaterialList
{
    private EnhancedMaterialListConfiguration Config => PluginConfig.Tweaks.EnhancedMaterialList;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Config.EnableZoneNames)
                      or nameof(Config.DisableZoneNameForCrystals)
                      or nameof(Config.DisableClickToOpenMapForCrystals))
        {
            _pendingMaterialListRefresh = true;
            _timeOfMaterialListRefresh = DateTime.UtcNow;
        }

        if (fieldName is nameof(Config.RestoreMaterialList))
        {
            SaveRestoreMaterialList(AgentRecipeMaterialList.Instance());
        }
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();

        ConfigGui.DrawBool("EnableZoneNames", ref Config.EnableZoneNames, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.EnableZoneNames))
                ConfigGui.DrawBool("DisableZoneNameForCrystals", ref Config.DisableZoneNameForCrystals, noFixSpaceAfter: true);
        });

        ConfigGui.DrawBool("ClickToOpenMap", ref Config.ClickToOpenMap, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.ClickToOpenMap))
                ConfigGui.DrawBool("DisableClickToOpenMapForCrystals", ref Config.DisableClickToOpenMapForCrystals, noFixSpaceAfter: true);
        });

        ConfigGui.DrawBool("AutoRefreshMaterialList", ref Config.AutoRefreshMaterialList);
        ConfigGui.DrawBool("AutoRefreshRecipeTree", ref Config.AutoRefreshRecipeTree);
        ConfigGui.DrawBool("RestoreMaterialList", ref Config.RestoreMaterialList);
        ConfigGui.DrawBool("AddSearchForItemByCraftingMethodContextMenuEntry", ref Config.AddSearchForItemByCraftingMethodContextMenuEntry, noFixSpaceAfter: true);
    }
}
