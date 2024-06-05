using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public class CharacterClassSwitcherConfiguration
{
    [BoolConfig]
    public bool DisableTooltips = false;

    [BoolConfig]
    public bool AlwaysOpenOnClassesJobsTab = false;

    [EnumConfig(DependsOn = nameof(AlwaysOpenOnClassesJobsTab))]
    public CharacterClassSwitcher.ClassesJobsSubTabs ForceClassesJobsSubTab = CharacterClassSwitcher.ClassesJobsSubTabs.None;
}

[Tweak, IncompatibilityWarning("SimpleTweaksPlugin", "CharacterWindowJobSwitcher")]
public unsafe partial class CharacterClassSwitcher : Tweak<CharacterClassSwitcherConfiguration>
{
    private const int NumClasses = 31;

    public enum ClassesJobsSubTabs
    {
        None,
        DoWDoM,
        DoHDoL,
    }

    private MemoryReplacement? TooltipPatch;
    private MemoryReplacement? PvpTooltipPatch;

    /* Address for AddonCharacterClass Tooltip Patch

        83 FD 14         cmp     ebp, 14h
        48 8B 6C 24 ??   mov     rbp, [rsp+68h+arg_8]
        7D 69            jge     short loc_140EB06A1     <- replacing this with a jmp rel8

       completely skips the whole if () {...} block, by jumping regardless of cmp result
     */
    [Signature("83 FD 14 48 8B 6C 24 ?? 7D 69")]
    private nint TooltipAddress { get; init; }

    /* Address for AddonPvPCharacter Tooltip Patch

        48 8D 4C 24 ??   lea     rcx, [rsp+68h+var_28]   <- replacing this with a jmp rel8
        E8 ?? ?? ?? ??   call    Component::GUI::AtkTooltipArgs_ctor
        ...

        completely skips the tooltip code, by jumping to the end of the function
     */
    [Signature("48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8B 83 ?? ?? ?? ?? 48 8B CF 0F B7 9F")]
    private nint PvPTooltipAddress { get; init; }

    private AddressHook<AddonCharacterClass.Delegates.OnSetup>? AddonCharacterClassOnSetupHook;
    private AddressHook<AddonCharacterClass.Delegates.OnRequestedUpdate>? AddonCharacterClassOnRequestedUpdateHook;
    private AddressHook<AddonCharacterClass.Delegates.ReceiveEvent>? AddonCharacterClassReceiveEventHook;
    private AddressHook<AddonPvPCharacter.Delegates.UpdateClasses>? AddonPvPCharacterUpdateClassesHook;
    private AddressHook<AddonPvPCharacter.Delegates.ReceiveEvent>? AddonPvPCharacterReceiveEventHook;
    private AddressHook<AgentStatus.Delegates.Show>? AgentStatusShowHook;

    public override void SetupHooks()
    {
        AddonCharacterClassOnSetupHook = new(AddonCharacterClass.StaticVirtualTablePointer->OnSetup, AddonCharacterClassOnSetupDetour);
        AddonCharacterClassOnRequestedUpdateHook = new(AddonCharacterClass.StaticVirtualTablePointer->OnRequestedUpdate, AddonCharacterClassOnRequestedUpdateDetour);
        AddonCharacterClassReceiveEventHook = new(AddonCharacterClass.StaticVirtualTablePointer->ReceiveEvent, AddonCharacterClassReceiveEventDetour);
        AddonPvPCharacterUpdateClassesHook = new(AddonPvPCharacter.MemberFunctionPointers.UpdateClasses, AddonPvPCharacterUpdateClassesDetour);
        AddonPvPCharacterReceiveEventHook = new(AddonPvPCharacter.StaticVirtualTablePointer->ReceiveEvent, AddonPvPCharacterReceiveEventDetour);
        AgentStatusShowHook = new(GetAgent<AgentStatus>()->VirtualTable->Show, AgentStatusShowDetour);
    }

    public override void Enable()
    {
        TooltipPatch = new(TooltipAddress + 8, [0xEB]);
        PvpTooltipPatch = new(PvPTooltipAddress, [0xEB, 0x63]);

        if (Config.DisableTooltips)
        {
            TooltipPatch.Enable();
            PvpTooltipPatch.Enable();
        }

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "AddonPvPCharacter", AddonPvPCharacterOnSetup);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "AddonPvPCharacter", AddonPvPCharacterOnSetup);

        TooltipPatch?.Disable();
        PvpTooltipPatch?.Disable();
    }

    public override void OnConfigChange(string fieldName)
    {
        if (Config.DisableTooltips)
        {
            TooltipPatch?.Enable();
            PvpTooltipPatch?.Enable();
        }
        else
        {
            TooltipPatch?.Disable();
            PvpTooltipPatch?.Disable();
        }
    }

    private static bool IsCrafter(int id)
    {
        return id >= 20 && id <= 27;
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

            var collisionNode = (AtkCollisionNode*)node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i + 2, (AtkEventListener*)addon, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i + 2, (AtkEventListener*)addon, null, false);
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

    private void AddonCharacterClassReceiveEventDetour(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint atkEventData)
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
                (eventType == AtkEventType.InputReceived && GamepadUtils.IsPressed(GamepadUtils.GamepadBinding.Accept));

            if (isClick && !Service.KeyState[VirtualKey.SHIFT])
            {
                SwitchClassJob(8 + (uint)eventParam - 22);
                return true;
            }
        }

        return ProcessEvents(node->AtkComponentBase.OwnerNode, imageNode, eventType);
    }


    private void AddonPvPCharacterOnSetup(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonPvPCharacter*)args.Addon;

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
        {
            var entry = addon->ClassEntries.GetPointer(i);
            if (entry->Base == null) continue;

            var collisionNode = (AtkCollisionNode*)entry->Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i | 0x10000, (AtkEventListener*)addon, null, false);
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
            (eventType == AtkEventType.InputReceived && GamepadUtils.IsPressed(GamepadUtils.GamepadBinding.Accept));

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

            SwitchClassJob((uint)classJobId);

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

        UIModule.PlaySound(8, 0, 0, 0);

        if (selectedGearset.Id == -1)
        {
            Service.ChatGui.PrintError(t("CharacterClassSwitcher.NoSuitableGearsetFound"));
            return;
        }

        Log($"Equipping gearset #{selectedGearset.Id}");
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
