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
    private EnhancedMaterialListConfiguration Config => _pluginConfig.Tweaks.EnhancedMaterialList;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (Status != TweakStatus.Enabled)
            return;

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
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();

        _configGui.DrawBool("EnableZoneNames", ref Config.EnableZoneNames, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.EnableZoneNames))
                _configGui.DrawBool("DisableZoneNameForCrystals", ref Config.DisableZoneNameForCrystals, noFixSpaceAfter: true);
        });

        _configGui.DrawBool("ClickToOpenMap", ref Config.ClickToOpenMap, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.ClickToOpenMap))
                _configGui.DrawBool("DisableClickToOpenMapForCrystals", ref Config.DisableClickToOpenMapForCrystals, noFixSpaceAfter: true);
        });

        _configGui.DrawBool("AutoRefreshMaterialList", ref Config.AutoRefreshMaterialList);
        _configGui.DrawBool("AutoRefreshRecipeTree", ref Config.AutoRefreshRecipeTree);
        _configGui.DrawBool("RestoreMaterialList", ref Config.RestoreMaterialList);
        _configGui.DrawBool("AddSearchForItemByCraftingMethodContextMenuEntry", ref Config.AddSearchForItemByCraftingMethodContextMenuEntry, noFixSpaceAfter: true);
    }
}
