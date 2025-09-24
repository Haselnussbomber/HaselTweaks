using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

public static unsafe class TargetStatusUtils
{
    public static void AddPermanentStatus(int index, int iconId, int canDispel, int timeRemainingColor, ReadOnlySeString timeRemaining, ReadOnlySeString tooltipText)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud2);

        if (numberArray == null || stringArray == null)
            return;

        if (numberArray->SubscribedAddonsCount == 0 || stringArray->SubscribedAddonsCount == 0)
            return;

        var typedNumberArray = (Hud2NumberArray*)numberArray->IntArray;

        ref var statusCount = ref typedNumberArray->TargetStatusCount;
        if (statusCount == typedNumberArray->TargetStatusIconIds.Length)
            return;

        // move statuses by 1

        for (var i = statusCount - 1 - index; i >= index; i--)
        {
            typedNumberArray->TargetStatusIconIds[i + 1] = typedNumberArray->TargetStatusIconIds[i];
            typedNumberArray->TargetStatusDispellable[i + 1] = typedNumberArray->TargetStatusDispellable[i];

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

        if (numberArray == null || stringArray == null)
            return;

        if (numberArray->SubscribedAddonsCount == 0 || stringArray->SubscribedAddonsCount == 0)
            return;

        var typedNumberArray = (Hud2NumberArray*)numberArray->IntArray;

        ref var statusCount = ref typedNumberArray->TargetStatusCount;
        if (statusCount == typedNumberArray->TargetStatusIconIds.Length)
            return;

        SetStatus(statusCount, iconId, canDispel, timeRemainingColor, timeRemaining, tooltipText);
        statusCount++;
    }

    public static void SetStatus(int index, int iconId, int canDispel, int timeRemainingColor, ReadOnlySeString timeRemaining, ReadOnlySeString tooltipText)
    {
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var stringArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.Hud2);

        if (numberArray == null || stringArray == null)
            return;

        if (numberArray->SubscribedAddonsCount == 0 || stringArray->SubscribedAddonsCount == 0)
            return;

        var typedNumberArray = (Hud2NumberArray*)numberArray->IntArray;

        ref var statusCount = ref typedNumberArray->TargetStatusCount;
        if (index < 0 || index >= typedNumberArray->TargetStatusIconIds.Length)
            return;

        typedNumberArray->TargetStatusIconIds[index] = (uint)(iconId + (timeRemainingColor << 29)); // shifted id is UIColor RowId
        typedNumberArray->TargetStatusDispellable[index] = false;

        stringArray->SetValue(3 + index, timeRemaining); // time remaining
        stringArray->SetValue(33 + index, tooltipText);
    }

    public static void ClearTimeRemainingCache()
    {
        Unsafe.InitBlockUnaligned(AgentHUD.Instance()->TargetInfoBuffTimeRemainingCache.GetPointer(0), 0, (8 + 4) * 30);
    }
}
