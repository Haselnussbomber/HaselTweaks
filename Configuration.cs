using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace HaselTweaks
{
    [Serializable]
    internal class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public List<string> EnabledTweaks { get; init; } = new();
        //public Dictionary<string, object> TweakConfigs { get; init; } = new();
    }
}
