using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Character Class Switcher",
    Description: "Clicking on a class/job in the character window finds the matching gearset and equips it. Hold shift on crafters to open the original desynthesis window."
)]
[IncompatibilityWarning("SimpleTweaksPlugin", "Simple Tweaks", "Character Window Job Switcher")]
public unsafe partial class CharacterClassSwitcher : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.CharacterClassSwitcher;

    public class Configuration
    {
        [ConfigField(Label = "Disable Tooltips", OnChange = nameof(OnTooltipConfigChange))]
        public bool DisableTooltips = false;
    }

    private bool _tooltipPatchApplied;

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

    public override void Enable()
    {
        ApplyTooltipPatch(Config.DisableTooltips);
    }

    public override void Disable()
    {
        ApplyTooltipPatch(false);
    }

    private void OnTooltipConfigChange()
    {
        ApplyTooltipPatch(Config.DisableTooltips);
    }

    private void ApplyTooltipPatch(bool enable)
    {
        if (enable && !_tooltipPatchApplied)
        {
            MemoryUtils.ReplaceRaw(TooltipAddress + 8, new byte[] { 0xEB }); // jmp rel8
            MemoryUtils.ReplaceRaw(PvPTooltipAddress, new byte[] { 0xEB, 0x63 }); // jmp rel8

            _tooltipPatchApplied = true;
        }
        else if (!enable && _tooltipPatchApplied)
        {
            MemoryUtils.ReplaceRaw(TooltipAddress + 8, new byte[] { 0x7D }); // jge rel8
            MemoryUtils.ReplaceRaw(PvPTooltipAddress, new byte[] { 0x48, 0x8D }); // original bytes (see signature)

            _tooltipPatchApplied = false;
        }
    }

    private static bool IsCrafter(int id)
    {
        return id >= 20 && id <= 27;
    }

    [VTableHook<AddonCharacterClass>((int)AtkResNodeVfs.OnSetup)]
    private nint AddonCharacterClass_OnSetup(AddonCharacterClass* addon, uint numAtkValues, AtkValue* atkValues)
    {
        var result = AddonCharacterClass_OnSetupHook!.OriginalDisposeSafe(addon, numAtkValues, atkValues);
        var eventListener = &addon->AtkUnitBase.AtkEventListener;

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i)) continue;

            var node = addon->ButtonNodesSpan[i].Value;
            if (node == null) continue;

            var collisionNode = (AtkCollisionNode*)node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i + 2, eventListener, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i + 2, eventListener, null, false);
        }

        return result;
    }

    [VTableHook<AddonCharacterClass>((int)AtkResNodeVfs.OnUpdate)]
    private void AddonCharacterClass_OnUpdate(AddonCharacterClass* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        AddonCharacterClass_OnUpdateHook.OriginalDisposeSafe(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            var node = addon->ButtonNodesSpan[i].Value;
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
                collisionNode->AtkResNode.Flags_2 |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.Flags_2 &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    [VTableHook<AddonCharacterClass>((int)AtkResNodeVfs.ReceiveEvent)]
    private nint AddonCharacterClass_ReceiveEvent(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        // skip events for tabs
        if (eventParam < 2)
            goto OriginalReceiveEventCode;

        var node = addon->ButtonNodesSpan[eventParam - 2].Value;
        if (node == null || node->AtkComponentBase.OwnerNode == null)
            goto OriginalReceiveEventCode;

        var imageNode = GetNode<AtkImageNode>(&node->AtkComponentBase, 4);
        if (imageNode == null)
            goto OriginalReceiveEventCode;

        // if job is unlocked, it has full alpha
        var isUnlocked = imageNode->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            goto OriginalReceiveEventCode;

        // special handling for crafters
        if (IsCrafter(eventParam - 2))
        {
            var isClick =
                eventType == AtkEventType.MouseClick || eventType == AtkEventType.ButtonClick ||
                (eventType == AtkEventType.InputReceived && GamepadUtils.IsPressed(GamepadUtils.GamepadBinding.Accept));

            if (isClick && !Service.KeyState[VirtualKey.SHIFT])
            {
                SwitchClassJob(8 + (uint)eventParam - 22);
                return 0;
            }
        }
        else if (ProcessEvents(node->AtkComponentBase.OwnerNode, imageNode, eventType))
        {
            return 0;
        }

OriginalReceiveEventCode:
        return AddonCharacterClass_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
    }

    [VTableHook<AddonPvPCharacter>((int)AtkResNodeVfs.OnSetup)]
    private void AddonPvPCharacter_OnSetup(AddonPvPCharacter* addon, uint numAtkValues, AtkValue* atkValues)
    {
        AddonPvPCharacter_OnSetupHook.OriginalDisposeSafe(addon, numAtkValues, atkValues);

        var eventListener = &addon->AtkUnitBase.AtkEventListener;

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
        {
            var entry = addon->ClassEntriesSpan[i];
            if (entry.Base == null) continue;

            var collisionNode = (AtkCollisionNode*)entry.Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i | 0x10000, eventListener, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i | 0x10000, eventListener, null, false);
        }
    }

    [AddressHook<AddonPvPCharacter>(nameof(AddonPvPCharacter.Addresses.UpdateClasses))]
    private void AddonPvPCharacter_UpdateClasses(AddonPvPCharacter* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        AddonPvPCharacter_UpdateClassesHook.OriginalDisposeSafe(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
        {
            var entry = addon->ClassEntriesSpan[i];
            if (entry.Base == null || entry.Icon == null) continue;

            var collisionNode = (AtkCollisionNode*)entry.Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = entry.Icon->AtkResNode.Color.A == 255;

            if (isUnlocked)
                collisionNode->AtkResNode.Flags_2 |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.Flags_2 &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    [VTableHook<AddonPvPCharacter>((int)AtkResNodeVfs.ReceiveEvent)]
    private nint AddonPvPCharacter_ReceiveEvent(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if ((eventParam & 0xFFFF0000) != 0x10000)
            goto OriginalPvPReceiveEventCode;

        var entryId = eventParam & 0x0000FFFF;
        if (entryId is < 0 or > AddonPvPCharacter.NUM_CLASSES)
            goto OriginalPvPReceiveEventCode;

        var entry = addon->ClassEntriesSpan[entryId];

        if (entry.Base == null || entry.Base->OwnerNode == null || entry.Icon == null)
            goto OriginalPvPReceiveEventCode;

        // if job is unlocked, it has full alpha
        var isUnlocked = entry.Icon->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            goto OriginalPvPReceiveEventCode;

        if (ProcessEvents(entry.Base->OwnerNode, entry.Icon, eventType))
            return 0;

OriginalPvPReceiveEventCode:
        return AddonPvPCharacter_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
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

            var iconId = textureInfo->AtkTexture.Resource->IconID;
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
            if (gearsetModule->IsValidGearset(id) == 0)
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
}
