using System;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

public partial struct HaselAtkComponentTextInput
{
    [MemberFunction("E8 ?? ?? ?? ?? 48 0F BF 56")]
    public partial IntPtr TriggerRedraw();
}
