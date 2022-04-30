using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using GearsetArray = HaselTweaks.Structs.RaptureGearsetModule.GearsetArray;
using GearsetFlag = HaselTweaks.Structs.RaptureGearsetModule.GearsetFlag;

namespace HaselTweaks.Tweaks;

public unsafe class CharacterClassSwitcher : Tweak
{
    public override string Name => "Character Class Switcher";
    public override string Description => "Clicking on a class/job in the character window finds the gearset with the highest item level and equips it. Hold shift to open a crafters desynthesis window.";

    [Signature("48 8B C4 48 89 58 10 48 89 70 18 48 89 78 20 55 41 54 41 55 41 56 41 57 48 8D 68 A1 48 81 EC ?? ?? ?? ?? 0F 29 70 C8 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 17 F3 0F 10 35 ?? ?? ?? ?? 45 33 C9 45 33 C0 F3 0F 11 74 24 ?? 0F 57 C9 48 8B F9 E8", DetourName = nameof(OnSetup))]
    private Hook<OnSetupDelegate>? SetupHook { get; init; } = null!;
    private delegate IntPtr OnSetupDelegate(AddonCharacterClass* addon, int a2);

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 4D 8B D1", DetourName = nameof(OnEvent))]
    private Hook<OnEventDelegate>? OnEventHook { get; init; } = null!;
    private delegate IntPtr OnEventDelegate(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, IntPtr a5);

    [Signature("4C 8B DC 53 55 56 57 41 55 41 56", DetourName = nameof(OnUpdate))]
    private Hook<OnUpdateDelegate>? OnUpdateHook { get; init; } = null!;
    private delegate void OnUpdateDelegate(AddonCharacterClass* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    [Signature("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 74 08 48 8B CB E8 ?? ?? ?? ?? 48 8B B4 24 ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
    private IntPtr g_InputManager { get; init; }

    [Signature("E8 ?? ?? ?? ?? 88 44 24 28 44 0F B6 CE", DetourName = nameof(OnEvent))]
    private InputManager_GetInputStatus_Delegate InputManager_GetInputStatus { get; init; } = null!;
    private delegate bool InputManager_GetInputStatus_Delegate(IntPtr inputManager, int a2);

    [Signature("E8 ?? ?? ?? ?? 0F BF 94 1F ?? ?? ?? ??")]
    private PlaySoundEffectDelegate PlaySoundEffect { get; init; } = null!;
    private delegate void PlaySoundEffectDelegate(int id, IntPtr a2, IntPtr a3, byte a4);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
    private const int VK_SHIFT = 0x10;
    private static bool IsShiftDown => (GetKeyState(VK_SHIFT) & 0x8000) == 0x8000;

    public override void Enable()
    {
        SetupHook?.Enable();
        OnEventHook?.Enable();
        OnUpdateHook?.Enable();
    }

    public override void Disable()
    {
        SetupHook?.Disable();
        OnEventHook?.Disable();
        OnUpdateHook?.Disable();
    }

    public override void Dispose()
    {
        SetupHook?.Dispose();
        OnEventHook?.Dispose();
        OnUpdateHook?.Dispose();
    }

    private static bool IsCrafter(int id)
    {
        return id >= 20 && id <= 27;
    }

    private IntPtr OnSetup(AddonCharacterClass* addon, int a2)
    {
        var result = SetupHook!.Original(addon, a2);

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i)) continue;

            var node = addon->BaseComponentNodes[i];
            if (node == null) continue;

            var collisionNode = (AtkCollisionNode*)node->UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i + 2, &addon->AtkUnitBase.AtkEventListener, &collisionNode->AtkResNode, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i + 2, &addon->AtkUnitBase.AtkEventListener, &collisionNode->AtkResNode, false);
        }

        return result;
    }

    private void OnUpdate(AddonCharacterClass* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        OnUpdateHook!.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i)) continue;

            var node = addon->BaseComponentNodes[i];
            if (node == null) continue;

            var collisionNode = (AtkCollisionNode*)node->UldManager.RootNode;
            if (collisionNode == null) continue;

            var imageNode = (AtkImageNode*)node->UldManager.SearchNodeById(4);
            if (imageNode == null) continue;

            // if job is unlocked, it has full alpha
            var isEnabled = imageNode->AtkResNode.Color.A == 255;

            if (isEnabled)
            {
                collisionNode->AtkResNode.Flags_2 |= 0x100000; // add Cursor Pointer flag
            }
            else
            {
                if ((collisionNode->AtkResNode.Flags_2 & 0x100000) == 0x100000)
                {
                    collisionNode->AtkResNode.Flags_2 ^= 0x100000; // toggle Cursor Pointer flag off
                }
            }
        }
    }

    private IntPtr OnEvent(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, IntPtr a5)
    {
        // skip events for tabs
        if (eventParam < 2)
            return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

        var node = addon->BaseComponentNodes[eventParam - 2];
        if (node == null)
            return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

        var ownerNode = node->OwnerNode;
        if (ownerNode == null)
            return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

        var imageNode = (AtkImageNode*)node->UldManager.SearchNodeById(4);
        if (imageNode == null)
            return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

        // if job is unlocked, it has full alpha
        var isUnlocked = imageNode->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

        var isClick =
            (eventType == AtkEventType.MouseClick || eventType == AtkEventType.ButtonClick) ||
            (eventType == AtkEventType.InputReceived && InputManager_GetInputStatus(g_InputManager, 12)); // A button on a Xbox 360 Controller

        if (IsCrafter(eventParam - 2))
        {
            // as far as i can see, any other controller button than A doesn't send InputReceived/ButtonClick events on button nodes,
            // so i can't move this functionality to the X button. anyway, i don't think it's a big problem, because
            // desynthesis levels are shown at the bottom of the window, too.

            if (isClick && !IsShiftDown)
            {
                SwitchClassJob(8 + (uint)eventParam - 22);
                return IntPtr.Zero;
            }
        }
        else
        {
            if (isClick)
            {
                var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
                if (textureInfo == null || textureInfo->AtkTexture.Resource == null)
                    return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

                var iconId = textureInfo->AtkTexture.Resource->Unk_1;
                if (iconId <= 62100)
                    return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);

                // yes, you see correctly. the iconId is 62100 + ClassJob RowId :)
                var classJobId = iconId - 62100;

                SwitchClassJob(classJobId);

                return IntPtr.Zero; // handled
            }
            else if (eventType == AtkEventType.MouseOver)
            {
                ownerNode->AtkResNode.AddBlue = 16;
                ownerNode->AtkResNode.AddGreen = 16;
                ownerNode->AtkResNode.AddRed = 16;

                // fallthrough for tooltips
            }
            else if (eventType == AtkEventType.MouseOut)
            {
                ownerNode->AtkResNode.AddBlue = 0;
                ownerNode->AtkResNode.AddGreen = 0;
                ownerNode->AtkResNode.AddRed = 0;

                // fallthrough for tooltips
            }
        }

        return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);
    }

    private void SwitchClassJob(uint classJobId)
    {
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null) return;

        // loop through all gearsets and find the one matching classJobId with the highest avg itemlevel
        var selectedGearset = (Index: -1, ItemLevel: -1);
        for (var i = 0; i < GearsetArray.Length; i++)
        {
            var gearset = gearsetModule->Gearsets[i];
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists)) continue;
            if (gearset->ClassJob != classJobId) continue;

            if (selectedGearset.ItemLevel < gearset->ItemLevel)
                selectedGearset = (i + 1, gearset->ItemLevel);
        }

        PlaySoundEffect(8, IntPtr.Zero, IntPtr.Zero, 0);

        if (selectedGearset.Index == -1)
        {
            // TODO: localize
            Service.Chat.PrintError($"Couldn't find a suitable gearset.");
            return;
        }

        Plugin.XivCommon.Functions.Chat.SendMessage("/gs change " + selectedGearset.Index);
    }
}
