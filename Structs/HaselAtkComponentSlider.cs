using System;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

public partial struct HaselAtkComponentSlider
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 43 3C")]
    public partial IntPtr SetValue(int value, bool triggerEvent = false);
}
