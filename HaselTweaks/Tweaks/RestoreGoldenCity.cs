using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class RestoreGoldenCity : ITweak
{
    private readonly ILogger<RestoreGoldenCity> _logger;
    private readonly TextService _textService;
    private readonly IGameInteropProvider _gameInteropProvider;

    private delegate byte GetEnabledRequirementIndexDelegate(ZoneSharedGroup* zoneSharedGroupRow);
    private Hook<GetEnabledRequirementIndexDelegate> _hook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _hook = _gameInteropProvider.HookFromSignature<GetEnabledRequirementIndexDelegate>(
            "E8 ?? ?? ?? ?? 0F B6 53 6C",
            GetEnabledRequirementIndexDetour);
    }

    public void OnEnable()
    {
        _hook.Enable();
        HaselZoneSharedGroupManager.Instance()->Reload();
    }

    public void OnDisable()
    {
        _hook.Disable();
        HaselZoneSharedGroupManager.Instance()->Reload();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _hook.Dispose();

        Status = TweakStatus.Disposed;
    }

    private byte GetEnabledRequirementIndexDetour(ZoneSharedGroup* zoneSharedGroupRow)
    {
        if (zoneSharedGroupRow->LGBSharedGroup is 10591852 or 10591856 or 10613110 or 10613109 or 10596151 or 10109854 && QuestManager.IsQuestComplete(70495))
        {
            _logger.LogInformation("Overwriting enable state of LGBSharedGroup {id}", zoneSharedGroupRow->LGBSharedGroup);
            return 0;
        }

        return _hook.Original(zoneSharedGroupRow);
    }
}

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x4C)]
public unsafe partial struct ZoneSharedGroup
{
    [FieldOffset(0x00)] public uint LGBSharedGroup;
    [FieldOffset(0x04), FixedSizeArray] internal FixedSizeArray6<uint> _quests;
    [FieldOffset(0x1C)] public uint Unknown0;
    [FieldOffset(0x20), FixedSizeArray] internal FixedSizeArray6<uint> _seqs;
    [FieldOffset(0x38)] public uint Unknown1;
    /// <remarks>
    /// 1 = Quest<br/>
    /// 2 = Quest with specific Sequence<br/>
    /// 3 = AetherCurrent<br/>
    /// 4 = EurekaStoryProgress<br/>
    /// 5 = DomaStoryProgress
    /// </remarks>
    [FieldOffset(0x3C), FixedSizeArray] internal FixedSizeArray6<uint> _types;
    [FieldOffset(0x42)] public byte Unknown8;
    [FieldOffset(0x43)] public byte Unknown9;
    [FieldOffset(0x44)] public byte Unknown10;
    [FieldOffset(0x45)] public byte Unknown11;
    [FieldOffset(0x46)] public byte Unknown12;
    [FieldOffset(0x47)] public byte Unknown13;
    [FieldOffset(0x48)] public byte Unknown14;
    [FieldOffset(0x49)] public byte Unknown15;
}
