using System;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

public partial struct HaselAtkComponentRadioButton
{
    [MemberFunction("48 83 EC 38 44 8B 89")]
    public partial IntPtr SetActive(bool active);
}
