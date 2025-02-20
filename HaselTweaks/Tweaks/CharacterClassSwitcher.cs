using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class CharacterClassSwitcher(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ILogger<CharacterClassSwitcher> Logger,
    IGameInteropProvider GameInteropProvider,
    IAddonLifecycle AddonLifecycle,
    IKeyState KeyState,
    IChatGui ChatGui,
    GamepadService GamepadService)
    : IConfigurableTweak
{
    public string InternalName => nameof(CharacterClassSwitcher);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private const int NumClasses = 33;

    private Hook<AtkTooltipManager.Delegates.ShowTooltip>? AtkTooltipManagerShowTooltipHook;
    private Hook<AddonCharacterClass.Delegates.OnSetup>? AddonCharacterClassOnSetupHook;
    private Hook<AddonCharacterClass.Delegates.OnRequestedUpdate>? AddonCharacterClassOnRequestedUpdateHook;
    private Hook<AddonCharacterClass.Delegates.ReceiveEvent>? AddonCharacterClassReceiveEventHook;
    private Hook<AddonPvPCharacter.Delegates.UpdateClasses>? AddonPvPCharacterUpdateClassesHook;
    private Hook<AddonPvPCharacter.Delegates.ReceiveEvent>? AddonPvPCharacterReceiveEventHook;
    private Hook<AgentStatus.Delegates.Show>? AgentStatusShowHook;

    public void OnInitialize()
    {
        GameInteropProvider.InitializeFromAttributes(this);

        AtkTooltipManagerShowTooltipHook = GameInteropProvider.HookFromAddress<AtkTooltipManager.Delegates.ShowTooltip>(
            AtkTooltipManager.Addresses.ShowTooltip.Value,
            AtkTooltipManagerShowTooltipDetour);

        AddonCharacterClassOnSetupHook = GameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.OnSetup>(
            AddonCharacterClass.StaticVirtualTablePointer->OnSetup,
            AddonCharacterClassOnSetupDetour);

        AddonCharacterClassOnRequestedUpdateHook = GameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.OnRequestedUpdate>(
            AddonCharacterClass.StaticVirtualTablePointer->OnRequestedUpdate,
            AddonCharacterClassOnRequestedUpdateDetour);

        AddonCharacterClassReceiveEventHook = GameInteropProvider.HookFromAddress<AddonCharacterClass.Delegates.ReceiveEvent>(
            AddonCharacterClass.StaticVirtualTablePointer->ReceiveEvent,
            AddonCharacterClassReceiveEventDetour);

        AddonPvPCharacterUpdateClassesHook = GameInteropProvider.HookFromAddress<AddonPvPCharacter.Delegates.UpdateClasses>(
            AddonPvPCharacter.MemberFunctionPointers.UpdateClasses,
            AddonPvPCharacterUpdateClassesDetour);

        AddonPvPCharacterReceiveEventHook = GameInteropProvider.HookFromAddress<AddonPvPCharacter.Delegates.ReceiveEvent>(
            AddonPvPCharacter.StaticVirtualTablePointer->ReceiveEvent,
            AddonPvPCharacterReceiveEventDetour);

        AgentStatusShowHook = GameInteropProvider.HookFromAddress<AgentStatus.Delegates.Show>(
            AgentStatus.Instance()->VirtualTable->Show,
            AgentStatusShowDetour);
    }

    public void OnEnable()
    {
        AtkTooltipManagerShowTooltipHook?.Enable();
        AddonCharacterClassOnSetupHook?.Enable();
        AddonCharacterClassOnRequestedUpdateHook?.Enable();
        AddonCharacterClassReceiveEventHook?.Enable();
        AddonPvPCharacterUpdateClassesHook?.Enable();
        AddonPvPCharacterReceiveEventHook?.Enable();
        AgentStatusShowHook?.Enable();

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "PvPCharacter", PvPCharacterOnSetup);
    }

    public void OnDisable()
    {
        AtkTooltipManagerShowTooltipHook?.Disable();
        AddonCharacterClassOnSetupHook?.Disable();
        AddonCharacterClassOnRequestedUpdateHook?.Disable();
        AddonCharacterClassReceiveEventHook?.Disable();
        AddonPvPCharacterUpdateClassesHook?.Disable();
        AddonPvPCharacterReceiveEventHook?.Disable();
        AgentStatusShowHook?.Disable();

        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "PvPCharacter", PvPCharacterOnSetup);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        AtkTooltipManagerShowTooltipHook?.Dispose();
        AddonCharacterClassOnSetupHook?.Dispose();
        AddonCharacterClassOnRequestedUpdateHook?.Dispose();
        AddonCharacterClassReceiveEventHook?.Dispose();
        AddonPvPCharacterUpdateClassesHook?.Dispose();
        AddonPvPCharacterReceiveEventHook?.Dispose();
        AgentStatusShowHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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

        AtkTooltipManagerShowTooltipHook!.Original(thisPtr, type, parentId, targetNode, tooltipArgs, unkDelegate, unk7, unk8);
    }

    private void AddonCharacterClassOnSetupDetour(AddonCharacterClass* addon, uint numAtkValues, AtkValue* atkValues)
    {
        AddonCharacterClassOnSetupHook!.Original(addon, numAtkValues, atkValues);

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
        AddonCharacterClassOnRequestedUpdateHook!.Original(addon, numberArrayData, stringArrayData);

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
        if (HandleAddonCharacterClassEvent(addon, eventType, eventParam))
            return;

        AddonCharacterClassReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }

    private bool HandleAddonCharacterClassEvent(AddonCharacterClass* addon, AtkEventType eventType, int eventParam)
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
                (eventType == AtkEventType.InputReceived && GamepadService.IsPressed(GamepadBinding.Accept));

            if (isClick && !KeyState[VirtualKey.SHIFT])
            {
                SwitchClassJob(8 + (uint)eventParam - 24);
                return true;
            }
        }

        return ProcessEvents(node->AtkComponentBase.OwnerNode, imageNode, eventType);
    }

    private void PvPCharacterOnSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonPvPCharacter*)args.Addon;

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
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
        AddonPvPCharacterUpdateClassesHook!.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
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

    private void AddonPvPCharacterReceiveEventDetour(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint atkEventData)
    {
        if (HandleAddonPvPCharacterEvent(addon, eventType, eventParam))
            return;

        AddonPvPCharacterReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }

    private bool HandleAddonPvPCharacterEvent(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam)
    {
        if ((eventParam & 0xFFFF0000) != 0x10000)
            return false;

        var entryId = eventParam & 0x0000FFFF;
        if (entryId is < 0 or > AddonPvPCharacter.NUM_CLASSES)
            return false;

        var entry = addon->ClassEntries.GetPointer(entryId);
        if (entry->Base == null || entry->Base->OwnerNode == null || entry->Icon == null)
            return false;

        // if job is unlocked, it has full alpha
        var isUnlocked = entry->Icon->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            return false;

        return ProcessEvents(entry->Base->OwnerNode, entry->Icon, eventType);
    }

    /// <returns>Boolean whether original code should be skipped (true) or not (false)</returns>
    private bool ProcessEvents(AtkComponentNode* componentNode, AtkImageNode* imageNode, AtkEventType eventType)
    {
        var isClick =
            eventType == AtkEventType.MouseClick ||
            (eventType == AtkEventType.InputReceived && GamepadService.IsPressed(GamepadBinding.Accept));

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
            ChatGui.PrintError(TextService.Translate("CharacterClassSwitcher.NoSuitableGearsetFound"));
            return;
        }

        Logger.LogInformation("Equipping gearset #{selectedGearsetId}", selectedGearset.Id);
        gearsetModule->EquipGearset(selectedGearset.Id - 1);
    }

    private void AgentStatusShowDetour(AgentStatus* agent)
    {
        if (Config.AlwaysOpenOnClassesJobsTab)
        {
            agent->TabIndex = 2;
        }

        AgentStatusShowHook!.Original(agent);
    }
}
