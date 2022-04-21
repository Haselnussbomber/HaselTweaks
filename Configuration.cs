using Dalamud.Configuration;
using HaselTweaks.Tweaks;
using System;
using System.Collections.Generic;

namespace HaselTweaks
{
    [Serializable]
    internal class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public List<string> EnabledTweaks { get; init; } = new();
        public TweakConfigs Tweaks { get; init; } = new();
    }

    public class TweakConfigs
    {
        public ChatTimestampFixer.Configuration ChatTimestampFixer { get; init; } = new();
    }
}
