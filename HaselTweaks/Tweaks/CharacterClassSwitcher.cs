using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SeVirtualKey = FFXIVClientStructs.FFXIV.Client.System.Input.SeVirtualKey;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharacterClassSwitcher : ConfigurableTweak<CharacterClassSwitcherConfiguration>
{
    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;

    private Hook<AtkTooltipManager.Delegates.ShowTooltip>? _atkTooltipManagerShowTooltipHook;

    public override void OnEnable()
    {
        _atkTooltipManagerShowTooltipHook = _gameInteropProvider.HookFromAddress<AtkTooltipManager.Delegates.ShowTooltip>(
            AtkTooltipManager.Addresses.ShowTooltip.Value,
            AtkTooltipManagerShowTooltipDetour);

        _atkTooltipManagerShowTooltipHook.Enable();

        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "Character", OnCharacterPreSetup);

        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "CharacterClass", OnCharacterClassPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "CharacterClass", OnCharacterClassPostRequestedUpdate);
        _addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "CharacterClass", OnCharacterClassPreReceiveEvent);

        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvPCharacter", OnPvPCharacterPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "PvPCharacter", OnPvPCharacterPostRequestedUpdate);
        _addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "PvPCharacter", OnPvPCharacterPreReceiveEvent);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "Character", OnCharacterPreSetup);

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "CharacterClass", OnCharacterClassPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "CharacterClass", OnCharacterClassPostRequestedUpdate);
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "CharacterClass", OnCharacterClassPreReceiveEvent);

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "PvPCharacter", OnPvPCharacterPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "PvPCharacter", OnPvPCharacterPostRequestedUpdate);
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, "PvPCharacter", OnPvPCharacterPreReceiveEvent);

        _atkTooltipManagerShowTooltipHook?.Dispose();
        _atkTooltipManagerShowTooltipHook = null;
    }

    private void AtkTooltipManagerShowTooltipDetour(
        AtkTooltipManager* thisPtr,
        AtkTooltipManager.AtkTooltipType type,
        ushort parentId,
        AtkResNode* targetNode,
        AtkTooltipManager.AtkTooltipArgs* tooltipArgs,
        delegate* unmanaged[Stdcall]<float*, float*, AtkResNode*, void> unkDelegate,
        bool unk7,
        bool unk8)
    {
        if (_config.DisableTooltips && (
            (TryGetAddon("CharacterClass"u8, out AtkUnitBase* unitBase) && unitBase->Id == parentId) ||
            (TryGetAddon("PvPCharacter"u8, out unitBase) && unitBase->Id == parentId)))
        {
            return;
        }

        _atkTooltipManagerShowTooltipHook!.Original(thisPtr, type, parentId, targetNode, tooltipArgs, unkDelegate, unk7, unk8);
    }

    #region Character

    private void OnCharacterPreSetup(AddonEvent type, AddonArgs addonArgs)
    {
        if (!_config.AlwaysOpenOnClassesJobsTab || addonArgs is not AddonSetupArgs args)
            return;

        var values = args.GetAtkValues();
        if (values.Length < 1)
            return;

        ref var tabIndex = ref values[1];
        if (!tabIndex.IsInt)
            return;

        tabIndex.Int = 2;
    }

    #endregion

    #region CharacterClass

    private void OnCharacterClassPostSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonCharacterClass*)args.Addon.Address;

        for (var i = 0; i < addon->ClassComponents.Length; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i))
                continue;

            var node = addon->ClassComponents.GetPointer(i)->Value;
            if (node == null)
                continue;

            var collisionNode = node->UldManager.RootNode;
            if (collisionNode == null)
                continue;

            collisionNode->AddEvent(AtkEventType.MouseClick, (uint)i + 2, (AtkEventListener*)addon, null, false);
            collisionNode->AddEvent(AtkEventType.InputReceived, (uint)i + 2, (AtkEventListener*)addon, null, false);
        }

        if (_config.AlwaysOpenOnClassesJobsTab && _config.ForceClassesJobsSubTab != ClassesJobsSubTabs.None)
        {
            addon->SetTab(_config.ForceClassesJobsSubTab switch
            {
                ClassesJobsSubTabs.DoWDoM => 0,
                ClassesJobsSubTabs.DoHDoL => 1,
                _ => 0
            });
        }
    }

    private void OnCharacterClassPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonCharacterClass*)args.Addon.Address;

        for (var i = 0; i < addon->ClassComponents.Length; i++)
        {
            var component = addon->ClassComponents.GetPointer(i)->Value;
            if (component == null)
                continue;

            // skip crafters as they already have Cursor Pointer flags
            if (IsCrafter(i))
            {
                // but ensure the button is enabled, even though the player might not have desynthesis unlocked
                component->SetEnabledState(true);
                continue;
            }

            var rootNode = component->UldManager.RootNode;
            if (rootNode == null)
                continue;

            var imageNode = component->GetComponentType() switch
            {
                ComponentType.Button => component->GetImageNodeById(6),
                ComponentType.Base => component->GetImageNodeById(4),
                _ => null
            };
            if (imageNode == null)
                continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = imageNode->Color.A == 255;
            if (isUnlocked)
                rootNode->DrawFlags |= 1 << 20; // add Cursor Pointer flag
            else
                rootNode->DrawFlags &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    private void OnCharacterClassPreReceiveEvent(AddonEvent type, AddonArgs addonArgs)
    {
        if (addonArgs is not AddonReceiveEventArgs args)
            return;

        var addon = (AddonCharacterClass*)args.Addon.Address;
        var eventType = (AtkEventType)args.AtkEventType;
        var eventParam = args.EventParam;
        var eventData = (AtkEventData*)args.AtkEventData;

        if (eventParam < 2) // skip events for tabs
            return;

        var component = addon->ClassComponents.GetPointer(args.EventParam - 2)->Value;
        if (component == null || component->OwnerNode == null)
            return;

        var imageNode = component->GetComponentType() switch
        {
            ComponentType.Button => component->GetImageNodeById(6),
            ComponentType.Base => component->GetImageNodeById(4),
            _ => null
        };
        if (imageNode == null)
            return;

        // if job is unlocked, it has full alpha
        var isUnlocked = imageNode->Color.A == 255;
        if (!isUnlocked)
            return;

        // special handling for crafters
        if (IsCrafter(args.EventParam - 2))
        {
            var isClick =
                eventType is AtkEventType.MouseClick or AtkEventType.ButtonClick ||
                (eventType == AtkEventType.InputReceived && eventData->InputData.InputId == 1);

            if (isClick && !UIInputData.Instance()->IsKeyDown(SeVirtualKey.SHIFT))
            {
                SwitchClassJob(8 + (uint)eventParam - 24);
                args.AtkEventType = 0;
                return;
            }
        }

        if (ProcessEvents(component->OwnerNode, imageNode, eventType, eventData))
            args.AtkEventType = 0;
    }

    #endregion

    #region PvPCharacter

    private void OnPvPCharacterPostSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonPvPCharacter*)args.Addon.Address;

        for (var i = 0; i < addon->ClassEntries.Length; i++)
        {
            var entry = addon->ClassEntries.GetPointer(i);
            if (entry->Base == null)
                continue;

            var rootNode = entry->Base->UldManager.RootNode;
            if (rootNode == null)
                continue;

            rootNode->AddEvent(AtkEventType.MouseClick, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
            rootNode->AddEvent(AtkEventType.InputReceived, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
        }
    }

    private void OnPvPCharacterPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonPvPCharacter*)args.Addon.Address;

        for (var i = 0; i < addon->ClassEntries.Length; i++)
        {
            var entry = addon->ClassEntries.GetPointer(i);
            if (entry->Base == null || entry->Icon == null)
                continue;

            var rootNode = entry->Base->UldManager.RootNode;
            if (rootNode == null)
                continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = entry->Icon->Color.A == 255;

            if (isUnlocked)
                rootNode->DrawFlags |= 1 << 20; // add Cursor Pointer flag
            else
                rootNode->DrawFlags &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    private void OnPvPCharacterPreReceiveEvent(AddonEvent type, AddonArgs addonArgs)
    {
        if (addonArgs is not AddonReceiveEventArgs args)
            return;

        var addon = (AddonPvPCharacter*)args.Addon.Address;
        var eventType = (AtkEventType)args.AtkEventType;
        var eventParam = args.EventParam;
        var eventData = (AtkEventData*)args.AtkEventData;

        if ((eventParam & 0xFFFF0000) != 0x10000)
            return;

        var entryId = eventParam & 0x0000FFFF;
        if (entryId < 0 || entryId > addon->ClassEntries.Length)
            return;

        var entry = addon->ClassEntries.GetPointer(entryId);
        if (entry->Base == null || entry->Base->OwnerNode == null || entry->Icon == null)
            return;

        // if job is unlocked, it has full alpha
        var isUnlocked = entry->Icon->Color.A == 255;
        if (!isUnlocked)
            return;

        if (ProcessEvents(entry->Base->OwnerNode, entry->Icon, eventType, eventData))
            args.AtkEventType = 0;
    }

    #endregion

    /// <returns>Boolean whether original code should be skipped (true) or not (false)</returns>
    private bool ProcessEvents(AtkComponentNode* componentNode, AtkImageNode* imageNode, AtkEventType eventType, AtkEventData* atkEventData)
    {
        var isClick =
            eventType == AtkEventType.MouseClick ||
            (eventType == AtkEventType.InputReceived && atkEventData->InputData.InputId == 1);

        if (isClick)
        {
            var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
            if (textureInfo == null || textureInfo->AtkTexture.Resource == null)
                return false;

            var iconId = textureInfo->AtkTexture.Resource->IconId;
            if (iconId <= 62100)
                return false;

            // yes, you see correctly. the iconId is 62100 + ClassJob RowId :)
            var classJobId = iconId - 62100;

            SwitchClassJob(classJobId);

            return true; // handled
        }

        if (eventType == AtkEventType.MouseOver)
        {
            componentNode->AddBlue = 16;
            componentNode->AddGreen = 16;
            componentNode->AddRed = 16;
        }
        else if (eventType == AtkEventType.MouseOut)
        {
            componentNode->AddBlue = 0;
            componentNode->AddGreen = 0;
            componentNode->AddRed = 0;
        }

        return false;
    }

    private void SwitchClassJob(uint classJobId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
            return;

        var permittedGearsetCount = InventoryManager.Instance()->GetPermittedGearsetCount();

        // loop through all gearsets and find the one matching classJobId with the highest avg itemlevel
        var selectedGearset = (Id: -1, ItemLevel: -1);
        for (var id = 0; id < permittedGearsetCount; id++)
        {
            // skip if invalid
            if (!gearsetModule->IsValidGearset(id))
                continue;

            var gearset = gearsetModule->GetGearset(id);

            // skip wrong job
            if (gearset->ClassJob != classJobId)
                continue;

            // skip if lower itemlevel than previous selected gearset
            if (selectedGearset.ItemLevel >= gearset->ItemLevel)
                continue;

            selectedGearset = (id + 1, gearset->ItemLevel);
        }

        UIGlobals.PlaySoundEffect(8);

        if (selectedGearset.Id == -1)
        {
            Chat.PrintError(_textService.Translate("CharacterClassSwitcher.NoSuitableGearsetFound"));
            return;
        }

        _logger.LogInformation("Equipping gearset #{selectedGearsetId}", selectedGearset.Id);
        gearsetModule->EquipGearset(selectedGearset.Id - 1);
    }

    private static bool IsCrafter(int id)
    {
        return id >= 22 && id <= 29;
    }
}
