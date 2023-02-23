using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Structs.Agents;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class MaterialAllocation : Tweak
{
    public override string Name => "Material Allocation";
    public override string Description => "Enhances the Island Sanctuarys \"Material Allocation\" window.";
    public static Configuration Config => Plugin.Config.Tweaks.MaterialAllocation;

    private AgentMJICraftSchedule* agentMJICraftSchedule;
    private AgentMJIGatheringNoteBook* agentMJIGatheringNoteBook;
    private ExcelSheet<MJIGatheringItem>? sheetMJIGatheringItem;
    private ExcelSheet<MJIItemPouch>? sheetMJIItemPouch;
    private uint nextMJIGatheringNoteBookItemId;

    public class Configuration
    {
        [ConfigField(Label = "Save last selected tab between game sessions")]
        public bool SaveLastSelectedTab = true;

        [ConfigField(Type = ConfigFieldTypes.Ignore)]
        public sbyte LastSelectedTab = 2;

        [ConfigField(Label = "Open Sanctuary Gathering Log for gatherable items")]
        public bool OpenGatheringLogOnItemClick = true;
    }

    public override void Setup()
    {
        agentMJICraftSchedule = GetAgent<AgentMJICraftSchedule>(AgentId.MJICraftSchedule);
        agentMJIGatheringNoteBook = GetAgent<AgentMJIGatheringNoteBook>(AgentId.MJIGatheringNoteBook);
        sheetMJIGatheringItem = Service.Data.GetExcelSheet<MJIGatheringItem>();
        sheetMJIItemPouch = Service.Data.GetExcelSheet<MJIItemPouch>();
    }

    public override void Enable()
    {
        nextMJIGatheringNoteBookItemId = 0;
    }

    // vf48 = OnOpen?
    [AutoHook, Signature("BA ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC 40 57 48 83 EC 20 48 8B F9 85 D2 7E 51", DetourName = nameof(AddonMJICraftMaterialConfirmation_vf48Detour))]
    private Hook<AddonMJICraftMaterialConfirmation_vf48Delegate> AddonMJICraftMaterialConfirmation_vf48Hook { get; init; } = null!;
    private delegate nint AddonMJICraftMaterialConfirmation_vf48Delegate(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues);
    public nint AddonMJICraftMaterialConfirmation_vf48Detour(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues)
    {
        if (Config.SaveLastSelectedTab)
        {
            if (Config.LastSelectedTab > 2)
                Config.LastSelectedTab = 2;

            agentMJICraftSchedule->TabIndex = Config.LastSelectedTab;

            for (var i = 0; i < 3; i++)
            {
                var button = addon->RadioButtonsSpan[i];
                if (button.Value != null)
                {
                    button.Value->SetSelected(i == Config.LastSelectedTab);
                }
            }
        }

        return AddonMJICraftMaterialConfirmation_vf48Hook.Original(addon, numAtkValues, atkValues);
    }

    // not really OnUpdate, but the function it calls after checking agent->Data exists
    [AutoHook, Signature("40 53 48 83 EC 20 48 8B 51 28 48 8B D9 8B 0A 83 E9 01 74 39", DetourName = nameof(AgentMJIGatheringNoteBook_OnUpdateDetour))]
    private Hook<AgentMJIGatheringNoteBook_OnUpdateDelegate> AgentMJIGatheringNoteBook_OnUpdateHook { get; init; } = null!;
    private delegate bool AgentMJIGatheringNoteBook_OnUpdateDelegate(AgentMJIGatheringNoteBook* agent);
    public bool AgentMJIGatheringNoteBook_OnUpdateDetour(AgentMJIGatheringNoteBook* agent)
    {
        var handleUpdate = Config.OpenGatheringLogOnItemClick
            && nextMJIGatheringNoteBookItemId != 0
            && agent->Data->Status == 3
            && (agent->Data->Flags & 2) != 0 // refresh pending
            && agent->Data->GatherItemPtrs != null;

        var ret = AgentMJIGatheringNoteBook_OnUpdateHook.Original(agent);

        if (handleUpdate)
        {
            UpdateGatheringNoteBookItem(agent, nextMJIGatheringNoteBookItemId);
            nextMJIGatheringNoteBookItemId = 0;
        }

        return ret;
    }

    private void UpdateGatheringNoteBookItem(AgentMJIGatheringNoteBook* agent, uint itemId)
    {
        if (sheetMJIItemPouch == null)
            return;

        var index = 0u;
        for (; index < sheetMJIItemPouch.RowCount; index++)
        {
            var gatherItem = agent->Data->GatherItemPtrs[index];
            if (gatherItem != null && gatherItem->ItemId == itemId)
                break; // found
        }

        if (index > 0)
        {
            agent->Data->SelectedItemIndex = index;
            agent->Data->Flags |= 2;
        }
    }

    [AutoHook, Signature("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 17 BA ?? ?? ?? ?? 49 8B D8", DetourName = nameof(AddonMJICraftMaterialConfirmation_OnSetupDetour))]
    private Hook<AddonMJICraftMaterialConfirmation_OnSetupDelegate> AddonMJICraftMaterialConfirmation_OnSetupHook { get; init; } = null!;
    private delegate void AddonMJICraftMaterialConfirmation_OnSetupDelegate(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, AtkValue* atkValues);
    public void AddonMJICraftMaterialConfirmation_OnSetupDetour(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, AtkValue* atkValues)
    {
        AddonMJICraftMaterialConfirmation_OnSetupHook.Original(addon, numAtkValues, atkValues);
        if (addon->ItemList != null && addon->ItemList->AtkComponentBase.OwnerNode != null)
        {
            //addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(31, 9901, (AtkEventListener*)addon, null, false); // MouseDown
            addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(32, 9902, (AtkEventListener*)addon, null, false); // MouseUp
        }
    }

    [AutoHook, Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 60 41 8D 40 FF", DetourName = nameof(AddonMJICraftMaterialConfirmation_ReceiveEventDetour))]
    private Hook<AddonMJICraftMaterialConfirmation_ReceiveEventDelegate> AddonMJICraftMaterialConfirmation_ReceiveEventHook { get; init; } = null!;
    private delegate void AddonMJICraftMaterialConfirmation_ReceiveEventDelegate(AddonMJICraftMaterialConfirmation* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5);
    public void AddonMJICraftMaterialConfirmation_ReceiveEventDetour(AddonMJICraftMaterialConfirmation* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if (eventParam is > 0 and < 4 && Config.SaveLastSelectedTab)
        {
            Config.LastSelectedTab = (sbyte)(eventParam - 1);
        }

        if (eventParam == 9902)
        {
            if (!Config.OpenGatheringLogOnItemClick || sheetMJIItemPouch == null || sheetMJIGatheringItem == null)
                goto handled;

            //var itemRenderer = *(AtkComponentListItemRenderer**)a5;
            var index = *(int*)(a5 + 0x10);
            //var list = *(HaselTweaks.Structs.AtkComponentList**)(a5 + 0x150);
            var id = addon->AtkUnitBase.AtkValues[index + 2].UInt; // MJIItemPouch RowId

            var pouchRow = sheetMJIItemPouch.GetRow(id);
            if (pouchRow == null || pouchRow.Item.Row == 0)
                goto handled;

            var itemId = pouchRow.Item.Row;

            var mjiGatheringItemRow = sheetMJIGatheringItem.FirstOrDefault(row => row.Item.Row == itemId);
            if (mjiGatheringItemRow == null)
            {
                var item = pouchRow.Item.Value;
                if (itemId != 0 && item != null)
                {
                    var fgColor = (ushort)(549 + (item.Rarity - 1) * 2);
                    var glowColor = (ushort)(fgColor + 1);

                    var sb = new SeStringBuilder()
                        .AddText($"Item ")
                        .AddUiForeground(fgColor)
                        .AddUiGlow(glowColor)
                        .AddItemLink(item.RowId, false)
                        .AddUiForeground(500)
                        .AddUiGlow(501)
                        .AddText(SeIconChar.LinkMarker.ToIconString() + " ")
                        .AddUiForegroundOff()
                        .AddUiGlowOff()
                        .AddText(item.Name)
                        .Add(new RawPayload(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 })) // LinkTerminator
                        .Add(new RawPayload(new byte[] { 0x02, 0x13, 0x02, 0xEC, 0x03 })) // ?
                        .AddText(" is not gatherable.");

                    Service.Chat.PrintChat(new XivChatEntry
                    {
                        Message = sb.BuiltString,
                        Type = XivChatType.Echo
                    });
                }
                goto handled;
            }

            var gatheringNoteBookAddon = agentMJIGatheringNoteBook->GetAddon();
            if (gatheringNoteBookAddon != null)
            {
                // just switch item
                UpdateGatheringNoteBookItem(agentMJIGatheringNoteBook, itemId);
                nextMJIGatheringNoteBookItemId = 0;
            }
            else
            {
                // open with item
                nextMJIGatheringNoteBookItemId = itemId;
                agentMJIGatheringNoteBook->AgentInterface.Show();
            }

            handled:
            ((HAtkEvent*)atkEvent)->SetEventHandled(false);
            return;
        }

        AddonMJICraftMaterialConfirmation_ReceiveEventHook.Original(addon, eventType, eventParam, atkEvent, a5);
    }
}
