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

    [Signature("66 83 FA 19 75 25 53 48 83 EC 20 49 8B D9 45 85 C0 75 13 E8", DetourName = nameof(DutySlot_ReceiveEvent))]
    private Hook<DutySlotReceiveEventDelegate>? Hook { get; init; }
    private delegate void DutySlotReceiveEventDelegate(void* a1, short a2, int a3, void* a4);

    public override void Enable()
    {
        Hook?.Enable();
    }

    public override void Disable()
    {
        Hook?.Disable();
    }

    public override void Dispose()
    {
        Hook?.Dispose();
    }

    private void DutySlot_ReceiveEvent(void* a1, short a2, int a3, void* a4)
    {
        Hook!.Original(a1, a2, a3, a4);

        if ((AtkEventType)a2 == AtkEventType.ButtonClick)
        {
            var slot = (DutySlot*)a1;
            var addon = (AddonWeeklyBingo*)slot->Addon;
            if (!addon->InDutySlotResetMode && slot->Status != DutySlotStatus.Claimable)
            {
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
                var expertRoulette = Service.Data.GetExcelSheet<ContentRoulette>()?.GetRow(5);

                if (expertRoulette != null &&
                    Service.ClientState.LocalPlayer != null &&
                    expertRoulette.RequiredLevel == entry.Data &&
                    expertRoulette.RequiredLevel == Service.ClientState.LocalPlayer?.Level)
                {
                    Plugin.XivCommon.Functions.DutyFinder.OpenRoulette(5); // Duty Roulette: Expert
                    return;
                }

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
                    case 5: // Crystalline Conflict
                        Plugin.XivCommon.Functions.DutyFinder.OpenRoulette(40); // Crystalline Conflict (Casual Match)
                        break;

                    case 6: // Frontline
                        Plugin.XivCommon.Functions.DutyFinder.OpenRoulette(7); // Daily Challenge: Frontline
                        break;

                        //case 11: unsupported // Rival Wings - 277: Astragalos, 599 Hidden Gorge
                }
                break;

            case 4: // Raids
                // TODO: not sure how to resolve this via Excel
                switch (entry.Data)
                {
                    case 2: // Binding Coil of Bahamut
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(93); // the Binding Coil of Bahamut - Turn 1
                        break;
                    case 3: // Second Coil of Bahamut
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(98); // the Second Coil of Bahamut - Turn 1
                        break;
                    case 4: // Final Coil of Bahamut
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(107); // the Final Coil of Bahamut - Turn 1
                        break;
                    case 5: // Alexander: Gordias
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(112); // Alexander - The Fist of the Father
                        break;
                    case 6: // Alexander: Midas
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(136); // Alexander - The Fist of the Son
                        break;
                    case 7: // Alexander: The Creator
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(186); // Alexander - The Eyes of the Creator
                        break;
                    case 8: // Omega: Deltascape
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(252); // Deltascape V1.0
                        break;
                    case 9: // Omega: Sigmascape
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(286); // Sigmascape V1.0
                        break;
                    case 10: // Omega: Alphascape
                        Plugin.XivCommon.Functions.DutyFinder.OpenDuty(587); // Alphascape V1.0
                        break;
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
