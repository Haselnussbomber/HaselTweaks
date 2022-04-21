using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;

namespace HaselTweaks.Tweaks;

public unsafe class ChatTimestampFixer : BaseTweak
{
    public override string Name => "Chat Timestamp Fixer";

    [Signature("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87", DetourName = nameof(Detour))]
    private Hook<DetourDelegate>? Hook { get; init; }
    private delegate byte* DetourDelegate(IntPtr a1, ulong addonRowId, ulong value, IntPtr a4);
    public override bool CanLoad => Hook?.Address != IntPtr.Zero;

    public override void Enable()
    {
        Hook?.Enable();
    }

    public override void Disable()
    {
        Hook?.Disable();
    }

    private byte* Detour(IntPtr a1, ulong addonRowId, ulong value, IntPtr a4)
    {
        if (addonRowId == 7840)
        {
            var str = (Utf8String*)(a1 + 0x9C0);
            var time = DateTime.UnixEpoch.AddSeconds(value).ToLocalTime();

            MemoryHelper.WriteString((IntPtr)str->StringPtr, $"[{time:HH:mm}] \0");
            str->BufUsed = 9;
            str->StringLength = 8;

            return str->StringPtr;
        }

        return Hook!.Original(a1, addonRowId, value, a4);
    }
}
