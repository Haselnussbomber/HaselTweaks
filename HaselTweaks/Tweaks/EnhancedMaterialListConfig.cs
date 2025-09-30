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
    public override void OnConfigChange(string fieldName)
    {
        if (Status != TweakStatus.Enabled)
            return;

        if (fieldName is nameof(_config.EnableZoneNames)
                      or nameof(_config.DisableZoneNameForCrystals)
                      or nameof(_config.DisableClickToOpenMapForCrystals))
        {
            _pendingMaterialListRefresh = true;
            _timeOfMaterialListRefresh = DateTime.UtcNow;
        }

        if (fieldName is nameof(_config.RestoreMaterialList))
        {
            SaveRestoreMaterialList(AgentRecipeMaterialList.Instance());
        }
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();

        _configGui.DrawBool("EnableZoneNames", ref _config.EnableZoneNames, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.EnableZoneNames))
                _configGui.DrawBool("DisableZoneNameForCrystals", ref _config.DisableZoneNameForCrystals, noFixSpaceAfter: true);
        });

        _configGui.DrawBool("ClickToOpenMap", ref _config.ClickToOpenMap, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.ClickToOpenMap))
                _configGui.DrawBool("DisableClickToOpenMapForCrystals", ref _config.DisableClickToOpenMapForCrystals, noFixSpaceAfter: true);
        });

        _configGui.DrawBool("AutoRefreshMaterialList", ref _config.AutoRefreshMaterialList);
        _configGui.DrawBool("AutoRefreshRecipeTree", ref _config.AutoRefreshRecipeTree);
        _configGui.DrawBool("RestoreMaterialList", ref _config.RestoreMaterialList);
        _configGui.DrawBool("AddSearchForItemByCraftingMethodContextMenuEntry", ref _config.AddSearchForItemByCraftingMethodContextMenuEntry, noFixSpaceAfter: true);
    }
}
