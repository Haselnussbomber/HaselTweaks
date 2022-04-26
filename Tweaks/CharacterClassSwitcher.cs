using System;
using System.Text.RegularExpressions;
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
    public override string Description => "Allow clicking on classes to switch to Gearsets. Same functionality as from SimpleTweaks, but written differently. Also equips the Gearset with the highest ItemLevel.";

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 4D 8B D1 0F B7 CA 41 8B F8 83 E9 06 74 7F", DetourName = nameof(OnEvent))]
    private Hook<OnEventDelegate>? OnEventHook { get; init; }
    private delegate IntPtr OnEventDelegate(AddonCharacterClass* addon, short eventType, int eventParam, AtkEvent* atkEvent, IntPtr a5);

    [Signature("48 8B C4 48 89 58 10 48 89 70 18 48 89 78 20 55 41 54 41 55 41 56 41 57 48 8D 68 A1 48 81 EC ?? ?? ?? ?? 0F 29 70 C8 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 17 F3 0F 10 35 ?? ?? ?? ?? 45 33 C9 45 33 C0 F3 0F 11 74 24 ?? 0F 57 C9 48 8B F9 E8", DetourName = nameof(OnSetup))]
    private Hook<OnSetupDelegate>? SetupHook { get; init; }
    private delegate IntPtr OnSetupDelegate(AddonCharacterClass* addon, int a2);

    public override void Enable()
    {
        OnEventHook?.Enable();
        SetupHook?.Enable();
    }

    public override void Disable()
    {
        OnEventHook?.Disable();
        SetupHook?.Disable();
    }

    public override void Dispose()
    {
        OnEventHook?.Dispose();
        SetupHook?.Dispose();
    }

    private IntPtr OnEvent(AddonCharacterClass* addon, short eventType, int eventParam, AtkEvent* atkEvent, IntPtr a5)
    {
        if (eventType == (short)AtkEventType.MouseClick)
        {
            HandleClick(addon, eventParam);
            return IntPtr.Zero;
        }

        return OnEventHook!.Original(addon, eventType, eventParam, atkEvent, a5);
    }

    private void HandleClick(AddonCharacterClass* addon, int eventParam)
    {
        // this happens in the original event listener too
        var index = eventParam - 2;

        // end where crafters start, because those open desynthesis level window
        if (index < 0 || index > 19) return;

        // this is a super bad way, but i didn't find any ClassJob rowId anywhere, so here we go...
        // thanks to SimpleTweaks for the texture path code

        var baseComponentNode = addon->BaseComponentNodes[index];
        if (baseComponentNode == null) return;

        var imageNode = (AtkImageNode*)baseComponentNode->UldManager.SearchNodeById(4);
        if (imageNode == null || imageNode->PartsList == null) return;

        var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
        if (textureInfo == null) return;

        var texType = textureInfo->AtkTexture.TextureType;
        if (texType != TextureType.Resource) return;

        var texResource = textureInfo->AtkTexture.Resource;
        if (texResource == null || texResource->TexFileResourceHandle == null) return;

        var texFileNamePtr = texResource->TexFileResourceHandle->ResourceHandle.FileName;

        var texString = texFileNamePtr.ToString();
        if (string.IsNullOrWhiteSpace(texString)) return;

        // grab icon id
        var match = Regex.Match(texString, @"^ui\/icon\/\d+\/(\d+)(?:_hr1)?\.tex$");
        if (!match.Success) return;

        // parse icon id into uint
        if (!uint.TryParse(match.Groups[1].Value.TrimStart('0'), out uint iconId)) return;
        if (iconId <= 62100) return;

        // yes, you see correctly. the iconId is 62100 + ClassJob RowId
        var classJobId = iconId - 62100;

        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null) return;

        // loop through all gearsets and find the one matching classJobId with the highest avg itemlevel
        var selectedGearset = (Index: 0, ItemLevel: 0u);
        for (var i = 0; i < GearsetArray.Length; i++)
        {
            var gearset = gearsetModule->Gearsets[i];
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists)) continue;
            if (gearset->ClassJob != classJobId) continue;

            if (selectedGearset.ItemLevel < gearset->ItemLevel)
                selectedGearset = (i + 1, gearset->ItemLevel);
        }

        if (selectedGearset.ItemLevel == 0)
        {
            // TODO: localize
            Service.Chat.PrintError($"Couldn't find a suitable gearset.");
            return;
        }

        Plugin.XivCommon.Functions.Chat.SendMessage("/gs change " + selectedGearset.Index);
    }

    private IntPtr OnSetup(AddonCharacterClass* addon, int a2)
    {
        var result = SetupHook!.Original(addon, a2);

        // end where crafters start, because those open desynthesis level window
        for (var i = 0; i < 20; i++)
        {
            var node = addon->BaseComponentNodes[i];
            if (node == null) continue;

            var rootNode = node->UldManager.RootNode; // seems to be a CollisionNode
            if (rootNode == null) continue;

            rootNode->AddEvent(AtkEventType.MouseClick, (uint)i + 2, &addon->AtkUnitBase.AtkEventListener, rootNode, false);
        }

        return result;
    }
}
