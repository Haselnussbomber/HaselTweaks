using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Game;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharacterClassSwitcher : IConfigurableTweak
{
    private const int NumClasses = 33; // includes blue mage, crafters and gatherers
    private const int NumPvPClasses = 21;

    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly ILogger<CharacterClassSwitcher> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IAddonLifecycle _addonLifecycle;

    private Hook<AtkTooltipManager.Delegates.ShowTooltip>? _atkTooltipManagerShowTooltipHook;
    private Hook<AddonCharacterClass.Delegates.OnSetup>? _addonCharacterClassOnSetupHook;
    private Hook<AddonCharacterClass.Delegates.OnRequestedUpdate>? _addonCharacterClassOnRequestedUpdateHook;
    private Hook<AddonCharacterClass.Delegates.ReceiveEvent>? _addonCharacterClassReceiveEventHook;
    private Hook<AddonPvPCharacter.Delegates.UpdateClasses>? _addonPvPCharacterUpdateClassesHook;
    private Hook<AddonPvPCharacter.Delegates.ReceiveEvent>? _addonPvPCharacterReceiveEventHook;
    private Hook<AgentStatus.Delegates.Show>? _agentStatusShowHook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _gameInteropProvider.InitializeFromAttributes(this);

        _atkTooltipManagerShowTooltipHook = _gameInteropProvider.HookFromAddress<AtkTooltipManager.Delegates.ShowTooltip>(
            AtkTooltipManager.Addresses.ShowTooltip.Value,
            AtkTooltipManagerShowTooltipDetour);

        _addonCharacterClassOnSetupHook = _gameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.OnSetup>(
            AddonCharacterClass.StaticVirtualTablePointer->OnSetup,
            AddonCharacterClassOnSetupDetour);

        _addonCharacterClassOnRequestedUpdateHook = _gameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.OnRequestedUpdate>(
            AddonCharacterClass.StaticVirtualTablePointer->OnRequestedUpdate,
            AddonCharacterClassOnRequestedUpdateDetour);

        _addonCharacterClassReceiveEventHook = _gameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.ReceiveEvent>(
            AddonCharacterClass.StaticVirtualTablePointer->ReceiveEvent,
            AddonCharacterClassReceiveEventDetour);

        _addonPvPCharacterUpdateClassesHook = _gameInteropProvider.HookFromAddress<AddonPvPCharacter.Delegates.UpdateClasses>(
            AddonPvPCharacter.MemberFunctionPointers.UpdateClasses,
            AddonPvPCharacterUpdateClassesDetour);

        _addonPvPCharacterReceiveEventHook = _gameInteropProvider.HookFromAddress<AddonPvPCharacter.Delegates.ReceiveEvent>(
            AddonPvPCharacter.StaticVirtualTablePointer->ReceiveEvent,
            AddonPvPCharacterReceiveEventDetour);

        _agentStatusShowHook = _gameInteropProvider.HookFromAddress<AgentStatus.Delegates.Show>(
            AgentStatus.Instance()->VirtualTable->Show,
            AgentStatusShowDetour);
    }

    public void OnEnable()
    {
        _atkTooltipManagerShowTooltipHook?.Enable();
        _addonCharacterClassOnSetupHook?.Enable();
        _addonCharacterClassOnRequestedUpdateHook?.Enable();
        _addonCharacterClassReceiveEventHook?.Enable();
        _addonPvPCharacterUpdateClassesHook?.Enable();
        _addonPvPCharacterReceiveEventHook?.Enable();
        _agentStatusShowHook?.Enable();

        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvPCharacter", PvPCharacterOnSetup);
    }

    public void OnDisable()
    {
        _atkTooltipManagerShowTooltipHook?.Disable();
        _addonCharacterClassOnSetupHook?.Disable();
        _addonCharacterClassOnRequestedUpdateHook?.Disable();
        _addonCharacterClassReceiveEventHook?.Disable();
        _addonPvPCharacterUpdateClassesHook?.Disable();
        _addonPvPCharacterReceiveEventHook?.Disable();
        _agentStatusShowHook?.Disable();

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "PvPCharacter", PvPCharacterOnSetup);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _atkTooltipManagerShowTooltipHook?.Dispose();
        _addonCharacterClassOnSetupHook?.Dispose();
        _addonCharacterClassOnRequestedUpdateHook?.Dispose();
        _addonCharacterClassReceiveEventHook?.Dispose();
        _addonPvPCharacterUpdateClassesHook?.Dispose();
        _addonPvPCharacterReceiveEventHook?.Dispose();
        _agentStatusShowHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private static bool IsCrafter(int id)
    {
        return id >= 22 && id <= 29;
    }

    private void AtkTooltipManagerShowTooltipDetour(
        AtkTooltipManager* thisPtr,
        AtkTooltipManager.AtkTooltipType type,
        ushort parentId,
        AtkResNode* targetNode,
        AtkTooltipManager.AtkTooltipArgs* tooltipArgs,
        delegate* unmanaged[Stdcall]<float*, float*, void*> unkDelegate,
        bool unk7,
        bool unk8)
    {
        if (Config.DisableTooltips && (
            (TryGetAddon<AtkUnitBase>("CharacterClass", out var unitBase) && unitBase->Id == parentId) ||
            (TryGetAddon("PvPCharacter", out unitBase) && unitBase->Id == parentId)))
        {
            return;
        }

        _atkTooltipManagerShowTooltipHook!.Original(thisPtr, type, parentId, targetNode, tooltipArgs, unkDelegate, unk7, unk8);
    }

    private void AddonCharacterClassOnSetupDetour(AddonCharacterClass* addon, uint numAtkValues, AtkValue* atkValues)
    {
        _addonCharacterClassOnSetupHook!.Original(addon, numAtkValues, atkValues);

        for (var i = 0; i < NumClasses; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i)) continue;

            var node = addon->ButtonNodes.GetPointer(i)->Value;
            if (node == null) continue;

            var collisionNode = node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AddEvent(AtkEventType.MouseClick, (uint)i + 2, (AtkEventListener*)addon, null, false);
            collisionNode->AddEvent(AtkEventType.InputReceived, (uint)i + 2, (AtkEventListener*)addon, null, false);
        }

        if (Config.AlwaysOpenOnClassesJobsTab && Config.ForceClassesJobsSubTab != ClassesJobsSubTabs.None)
        {
            addon->SetTab(Config.ForceClassesJobsSubTab switch
            {
                ClassesJobsSubTabs.DoWDoM => 0,
                ClassesJobsSubTabs.DoHDoL => 1,
                _ => 0
            });
        }
    }

    private void AddonCharacterClassOnRequestedUpdateDetour(AddonCharacterClass* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        _addonCharacterClassOnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < NumClasses; i++)
        {
            var node = addon->ButtonNodes.GetPointer(i)->Value;
            if (node == null)
                continue;

            // skip crafters as they already have Cursor Pointer flags
            if (IsCrafter(i))
            {
                // but ensure the button is enabled, even though the player might not have desynthesis unlocked
                node->AtkComponentBase.SetEnabledState(true);
                continue;
            }

            var collisionNode = (AtkCollisionNode*)node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null)
                continue;

            var imageNode = GetNode<AtkImageNode>(&node->AtkComponentBase, 4);
            if (imageNode == null)
                continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = imageNode->AtkResNode.Color.A == 255;
            if (isUnlocked)
                collisionNode->AtkResNode.DrawFlags |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.DrawFlags &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    private void AddonCharacterClassReceiveEventDetour(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (HandleAddonCharacterClassEvent(addon, eventType, eventParam, atkEventData))
            return;

        _addonCharacterClassReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }

    private bool HandleAddonCharacterClassEvent(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEventData* atkEventData)
    {
        // skip events for tabs
        if (eventParam < 2)
            return false;

        var node = addon->ButtonNodes.GetPointer(eventParam - 2)->Value;
        if (node == null || node->AtkComponentBase.OwnerNode == null)
            return false;

        var imageNode = GetNode<AtkImageNode>(&node->AtkComponentBase, 4);
        if (imageNode == null)
            return false;

        // if job is unlocked, it has full alpha
        var isUnlocked = imageNode->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            return false;

        // special handling for crafters
        if (IsCrafter(eventParam - 2))
        {
            var isClick =
                eventType == AtkEventType.MouseClick || eventType == AtkEventType.ButtonClick ||
                (eventType == AtkEventType.InputReceived && atkEventData->InputData.InputId == 1);
             
            if (isClick && !UIInputData.Instance()->IsKeyDown(SeVirtualKey.SHIFT))
            {
                SwitchClassJob(8 + (uint)eventParam - 24);
                return true;
            }
        }

        return ProcessEvents(node->AtkComponentBase.OwnerNode, imageNode, eventType, atkEventData);
    }

    private void PvPCharacterOnSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonPvPCharacter*)args.Addon;

        for (var i = 0; i < NumPvPClasses; i++)
        {
            var entry = addon->ClassEntries.GetPointer(i);
            if (entry->Base == null) continue;

            var collisionNode = entry->Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AddEvent(AtkEventType.MouseClick, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
            collisionNode->AddEvent(AtkEventType.InputReceived, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
        }
    }

    private void AddonPvPCharacterUpdateClassesDetour(AddonPvPCharacter* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        _addonPvPCharacterUpdateClassesHook!.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < NumPvPClasses; i++)
        {
            var entry = addon->ClassEntries.GetPointer(i);
            if (entry->Base == null || entry->Icon == null) continue;

            var collisionNode = (AtkCollisionNode*)entry->Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = entry->Icon->AtkResNode.Color.A == 255;

            if (isUnlocked)
                collisionNode->AtkResNode.DrawFlags |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.DrawFlags &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    private void AddonPvPCharacterReceiveEventDetour(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (HandleAddonPvPCharacterEvent(addon, eventType, eventParam, atkEventData))
            return;

        _addonPvPCharacterReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }

    private bool HandleAddonPvPCharacterEvent(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam, AtkEventData* atkEventData)
    {
        if ((eventParam & 0xFFFF0000) != 0x10000)
            return false;

        var entryId = eventParam & 0x0000FFFF;
        if (entryId is < 0 or > NumPvPClasses)
            return false;

        var entry = addon->ClassEntries.GetPointer(entryId);
        if (entry->Base == null || entry->Base->OwnerNode == null || entry->Icon == null)
            return false;

        // if job is unlocked, it has full alpha
        var isUnlocked = entry->Icon->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            return false;

        return ProcessEvents(entry->Base->OwnerNode, entry->Icon, eventType, atkEventData);
    }

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
            componentNode->AtkResNode.AddBlue = 16;
            componentNode->AtkResNode.AddGreen = 16;
            componentNode->AtkResNode.AddRed = 16;
        }
        else if (eventType == AtkEventType.MouseOut)
        {
            componentNode->AtkResNode.AddBlue = 0;
            componentNode->AtkResNode.AddGreen = 0;
            componentNode->AtkResNode.AddRed = 0;
        }

        return false;
    }

    private void SwitchClassJob(uint classJobId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
            return;

        // loop through all gearsets and find the one matching classJobId with the highest avg itemlevel
        var selectedGearset = (Id: -1, ItemLevel: -1);
        for (var id = 0; id < 100; id++)
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

    private void AgentStatusShowDetour(AgentStatus* agent)
    {
        if (Config.AlwaysOpenOnClassesJobsTab)
        {
            agent->TabIndex = 2;
        }

        _agentStatusShowHook!.Original(agent);
    }
}
