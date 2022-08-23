using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Utils;

public unsafe class StringUtils
{
    private readonly Dictionary<uint, string> ENpcResidentNameCache = new();
    private readonly Dictionary<uint, string> EObjNameCache = new();
    private readonly Dictionary<uint, string> QuestCache = new();

    public StringUtils()
    {
        SignatureHelper.Initialise(this);
    }

    [Signature("E9 ?? ?? ?? ?? 48 8D 47 30")]
    private readonly FormatObjectStringDelegate FormatObjectString = null!; // how do you expect me to name things i have no clue about
    private delegate IntPtr FormatObjectStringDelegate(int mode, uint id, uint idConversionMode, uint a4);

    public string GetENpcResidentName(uint npcId)
    {
        if (ENpcResidentNameCache.ContainsKey(npcId))
        {
            return ENpcResidentNameCache[npcId];
        }

        var ret = MemoryHelper.ReadSeStringNullTerminated(FormatObjectString(0, npcId, 3, 1)).ToString();

        ENpcResidentNameCache.Add(npcId, ret);

        return ret;
    }

    public string GetEObjName(uint objId)
    {
        if (EObjNameCache.ContainsKey(objId))
        {
            return EObjNameCache[objId];
        }

        var ret = MemoryHelper.ReadSeStringNullTerminated(FormatObjectString(0, objId, 5, 1)).ToString();

        EObjNameCache.Add(objId, ret);

        return ret;
    }

    public string GetQuestName(uint questId, bool clean)
    {
        if (QuestCache.ContainsKey(questId))
        {
            return QuestCache[questId];
        }

        var quest = Service.Data.GetExcelSheet<Quest>()?.GetRow(questId);
        if (quest == null)
        {
            return string.Empty;
        }

        var ret = SeString.Parse(quest.Name.RawData).ToString();

        if (clean)
        {
            ret = Regex.Replace(ret, @"^[\ue000-\uf8ff]+ ", "");
        }

        QuestCache.Add(questId, ret);

        return ret;
    }
}
