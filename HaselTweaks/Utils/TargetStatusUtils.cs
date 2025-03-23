using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.ReadOnly;

namespace HaselTweaks.Utils;

public static unsafe class TargetStatusUtils
{
    public const int MaxStatusCount = 30;

    public static void AddPermanentStatus(int index, int iconId, int canDispel, int timeRemainingColor, ReadOnlySeString timeRemaining, ReadOnlySeString tooltipText)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud2);

        ref var statusCount = ref numberArray->IntArray[4];
        if (statusCount == MaxStatusCount)
            return;

        // move statuses by 1

        for (var i = statusCount - 1 - index; i >= index; i--)
        {
            numberArray->IntArray[5 + index + i + 1] = numberArray->IntArray[5 + index + i];
            numberArray->IntArray[35 + index + i + 1] = numberArray->IntArray[35 + index + i];
            stringArray->SetValue(3 + index + i + 1, stringArray->StringArray[3 + index + i], readBeforeWrite: false);
            stringArray->SetValue(33 + index + i + 1, stringArray->StringArray[33 + index + i], readBeforeWrite: false);
        }

        statusCount++;

        SetStatus(index, iconId, canDispel, timeRemainingColor, timeRemaining, tooltipText);
        ClearTimeRemainingCache();
    }

    public static void AddStatus(int iconId, int canDispel, int timeRemainingColor, ReadOnlySeString timeRemaining, ReadOnlySeString tooltipText)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud2);

        ref var statusCount = ref numberArray->IntArray[4];
        if (statusCount == MaxStatusCount)
            return;

        SetStatus(statusCount, iconId, canDispel, timeRemainingColor, timeRemaining, tooltipText);
        statusCount++;
    }

    public static void SetStatus(int index, int iconId, int canDispel, int timeRemainingColor, ReadOnlySeString timeRemaining, ReadOnlySeString tooltipText)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud2);

        var statusCount = numberArray->IntArray[4];
        if (index is < 0 or >= 30)
            return;

        numberArray->IntArray[5 + index] = iconId + (timeRemainingColor << 29); // shifted id is UIColor RowId
        numberArray->IntArray[35 + index] = 0; // CanDispel?
        stringArray->SetValue(3 + index, timeRemaining); // time remaining
        stringArray->SetValue(33 + index, tooltipText);
    }

    public static void ClearTimeRemainingCache()
    {
        Unsafe.InitBlock(AgentHUD.Instance()->TargetInfoBuffTimeRemainingCache.GetPointer(0), 0, (8 + 4) * 30);
    }
}
