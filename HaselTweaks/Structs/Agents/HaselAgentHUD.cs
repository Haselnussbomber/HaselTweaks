using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Agents;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = AgentHUD.StructSize)]
public unsafe partial struct HaselAgentHUD
{
    public static HaselAgentHUD* Instance() => (HaselAgentHUD*)AgentHUD.Instance();

    [MemberFunction("E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 44 39 A7")]
    public partial void UpdateExp(NumberArrayData* expNumberArray, StringArrayData* expStringArray, StringArrayData* characterStringArray);
}
