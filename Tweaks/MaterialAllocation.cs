using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Structs.Agents;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Material Allocation",
    Description: "Enhances the Island Sanctuarys \"Material Allocation\" window."
)]
public unsafe partial class MaterialAllocation : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.MaterialAllocation;

    private uint _nextMJIGatheringNoteBookItemId;

    public class Configuration
    {
        [ConfigField(Label = "Save last selected tab between game sessions")]
        public bool SaveLastSelectedTab = true;

        [ConfigField(Type = ConfigFieldTypes.Ignore)]
        public byte LastSelectedTab = 2;

        [ConfigField(Label = "Open Sanctuary Gathering Log for gatherable items")]
        public bool OpenGatheringLogOnItemClick = true;
    }

    public override void Enable()
    {
        _nextMJIGatheringNoteBookItemId = 0;
    }

    [VTableHook<AddonMJICraftMaterialConfirmation>(48)]
    public nint AddonMJICraftMaterialConfirmation_vf48(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues)
    {
        if (Config.SaveLastSelectedTab && GetAgent<AgentMJICraftSchedule>(AgentId.MJICraftSchedule, out var agentMJICraftSchedule))
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

        return AddonMJICraftMaterialConfirmation_vf48Hook.OriginalDisposeSafe(addon, numAtkValues, atkValues);
    }

    public override unsafe void OnAddonOpen(string addonName, AtkUnitBase* unitbase)
    {
        if (addonName != "MJICraftMaterialConfirmation")
            return;

        var addon = (AddonMJICraftMaterialConfirmation*)unitbase;
        if (addon->ItemList != null && addon->ItemList->AtkComponentBase.OwnerNode != null)
        {
            //addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(31, 9901, (AtkEventListener*)addon, null, false); // MouseDown
            addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(32, 9902, (AtkEventListener*)addon, null, false); // MouseUp
        }
    }

    [VTableHook<AgentMJIGatheringNoteBook>((int)AgentInterfaceVfs.Update)]
    public void AgentMJIGatheringNoteBook_Update(AgentMJIGatheringNoteBook* agent)
    {
        var handleUpdate = Config.OpenGatheringLogOnItemClick
            && _nextMJIGatheringNoteBookItemId != 0
            && agent->Data != null
            && agent->Data->Status == 3
            && (agent->Data->Flags & 2) != 2 // refresh pending
            && agent->Data->GatherItemPtrs != null;

        AgentMJIGatheringNoteBook_UpdateHook.OriginalDisposeSafe(agent);

        if (handleUpdate)
        {
            UpdateGatheringNoteBookItem(agent, _nextMJIGatheringNoteBookItemId);
            _nextMJIGatheringNoteBookItemId = 0;
        }
    }

    [VTableHook<AddonMJICraftMaterialConfirmation>((int)AtkResNodeVfs.ReceiveEvent)]
    public void AddonMJICraftMaterialConfirmation_ReceiveEvent(AddonMJICraftMaterialConfirmation* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if (eventParam is > 0 and < 4 && Config.SaveLastSelectedTab)
        {
            Config.LastSelectedTab = (byte)(eventParam - 1);
        }

        if (eventParam == 9902 && GetAgent<AgentMJIGatheringNoteBook>(AgentId.MJIGatheringNoteBook, out var agentMJIGatheringNoteBook))
        {
            var sheetMJIGatheringItem = Service.Data.GetExcelSheet<MJIGatheringItem>();
            var sheetMJIItemPouch = Service.Data.GetExcelSheet<MJIItemPouch>();
            if (!Config.OpenGatheringLogOnItemClick || sheetMJIItemPouch == null || sheetMJIGatheringItem == null)
                goto handled;

            //var itemRenderer = *(AtkComponentListItemRenderer**)a5;
            var index = *(int*)(a5 + 0x10);
            //var list = *(HaselTweaks.Structs.AtkComponentList**)(a5 + 0x150);
            var id = addon->AtkUnitBase.AtkValues[index + 4].UInt; // MJIItemPouch RowId

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

            if (GetAddon<AddonAetherCurrent>(AgentId.MJIGatheringNoteBook, out var gatheringNoteBookAddon))
            {
                // just switch item
                UpdateGatheringNoteBookItem(agentMJIGatheringNoteBook, itemId);
                _nextMJIGatheringNoteBookItemId = 0;
            }
            else
            {
                // open with item
                _nextMJIGatheringNoteBookItemId = itemId;
                agentMJIGatheringNoteBook->AgentInterface.Show();
            }

handled:
            atkEvent->SetEventIsHandled();
            return;
        }

        AddonMJICraftMaterialConfirmation_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
    }

    private void UpdateGatheringNoteBookItem(AgentMJIGatheringNoteBook* agent, uint itemId)
    {
        for (var index = 0u; index < agent->Data->ItemCount; index++)
        {
            var gatherItem = agent->Data->GatherItemPtrs[index];
            if (gatherItem == null || gatherItem->ItemId != itemId)
                continue;

            agent->Data->SelectedItemIndex = index;
            agent->Data->Flags |= 2;
            break;
        }
    }
}
