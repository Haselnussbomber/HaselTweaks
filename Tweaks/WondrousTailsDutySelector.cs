using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using static HaselTweaks.Structs.AddonWeeklyBingo;

namespace HaselTweaks.Tweaks;

public unsafe class WondrousTailsDutySelector : BaseTweak
{
    public override string Name => "Wondrous Tails Duty Selector";
    public override bool CanLoad => false; // TODO: fix it first

    [Signature("66 83 FA 19 75 25 53 48 83 EC 20 49 8B D9 45 85 C0 75 13 E8", DetourName = nameof(DutySlot_ReceiveEvent))]
    private readonly Hook<DutySlotReceiveEventDelegate>? Hook = null;
    private delegate void DutySlotReceiveEventDelegate(void* a1, short a2, int a3, void* a4);

    public override void Enable()
    {
        base.Enable();
        Hook?.Enable();
    }

    public override void Disable()
    {
        base.Disable();
        Hook?.Disable();
    }

    public override void Dispose()
    {
        base.Dispose();
        Hook?.Dispose();
    }

    private void DutySlot_ReceiveEvent(void* a1, short a2, int a3, void* a4)
    {
        PluginLog.Log("before original hook");
        Hook!.Original(a1, a2, a3, a4);
        PluginLog.Log("after original hook");

        if ((AtkEventType)a2 == AtkEventType.ButtonClick)
        {
            PluginLog.Log("Button Click");
            var node = (DutySlot*)a1;
            PluginLog.Log("is a DutySlot");
            var addon = (AddonWeeklyBingo*)node->Addon;
            PluginLog.Log("is a AddonWeeklyBingo");
            if (!addon->InDutySlotResetMode && node->Status != DutySlotStatus.Claimable)
            {
                PluginLog.Log("should be handled");
                HandleClick((DutySlot*)a1);
            }
        }
    }

    private void HandleClick(DutySlot* dutySlot)
    {
        PluginLog.Log("HandleClick!");
        var UIState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
        if (UIState == null) return;

        //var id = UIState->PlayerState.WeeklyBonusOrderDataIds[dutySlot->Index];
        var id = ((PlayerState*)&UIState->PlayerState)->WeeklyBonusOrderDataIds[dutySlot->Index];
        if (id <= 0) return;

        var entry = Service.Data.GetExcelSheet<WeeklyBingoOrderData>()!.GetRow(id);
        if (entry == null) return;

        ContentFinderCondition? contentFinderCondition = null;

        switch (entry.Type)
        {
            case 0: // Duty
                contentFinderCondition = GetDutyByContentId((ushort)entry.Data);
                if (contentFinderCondition == null) return;
                Plugin.XivCommon.Functions.DutyFinder.OpenDuty(contentFinderCondition);
                break;

            case 1: // Max Level Dungeons
                contentFinderCondition = GetFirstDungeonByLevel((byte)entry.Data);
                if (contentFinderCondition == null) return;
                Plugin.XivCommon.Functions.DutyFinder.OpenDuty(contentFinderCondition);
                break;

            case 2: // Leveling Dungeons
                Plugin.XivCommon.Functions.DutyFinder.OpenRoulette(1); // Duty Roulette: Leveling
                break;

            case 3: // PvP
                switch (entry.Data)
                {
                    case 5: // The Feast
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(478); // The Feast (Ranked)
                        break;

                    case 6: // Frontline
                        Plugin.XivCommon.Functions.DutyFinder.OpenRoulette(7); // Daily Challenge: Frontline
                        break;

                        //case 11: unsupported // Rival Wings - 277: Astragalos, 599 Hidden Gorge
                }
                break;

            default:
                PluginLog.Verbose($"click registered: id {id}, unknown type {entry.Type}, {entry.Text.Value?.Description}");
                break;
        }
    }

    private static ContentFinderCondition? GetDutyByContentId(ushort contentId)
    {
        var sheet = Service.Data.GetExcelSheet<ContentFinderCondition>();
        if (sheet == null) return null;

        foreach (var row in sheet)
        {
            if (row == null || row.RowId == 0 || row.Content != contentId)
                continue;

            return row;
        }

        return null;
    }

    private static ContentFinderCondition? GetFirstDungeonByLevel(byte level)
    {
        var sheet = Service.Data.GetExcelSheet<ContentFinderCondition>();
        if (sheet == null) return null;

        var duty = (SortKey: ushort.MaxValue, ContentFinderCondition: new ContentFinderCondition());

        foreach (var row in sheet)
        {
            if (row == null
                || row.RowId == 0
                || row.PvP
                || row.ClassJobLevelRequired != level
                || row.AcceptClassJobCategory.Row != 108
                || row.ContentMemberType.Row != 2
            ) continue;

            if (row.SortKey < duty.SortKey)
            {
                duty = (row.SortKey, row);
            }
        }

        if (duty.SortKey == ushort.MaxValue)
            return null;

        return duty.ContentFinderCondition;
    }
}
