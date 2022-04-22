using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using HaselTweaks.Tweaks;

namespace HaselTweaks;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public List<string> EnabledTweaks { get; init; } = new();
    public TweakConfigs Tweaks { get; init; } = new();
}

public class TweakConfigs
{
    public AutoSortArmouryChest.Configuration AutoSortArmouryChest { get; init; } = new();
    public CustomChatTimestamp.Configuration CustomChatTimestamp { get; init; } = new();
}
