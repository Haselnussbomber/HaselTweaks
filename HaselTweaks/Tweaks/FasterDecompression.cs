using System.IO;
using HaselTweaks.Services;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FasterDecompression : Tweak
{
    private readonly NativeService _nativeService;
    private readonly ISigScanner _sigScanner;
    private MemoryReplacement? _patch;

    public override void OnEnable()
    {
        if (_nativeService.ReadSqpkChunkAddr == 0)
        {
            _logger.LogError("Could not find function ReadSqpkChunk");
            Status = TweakStatus.Error;
            return;
        }

        var originalReadSqpkChunkAddr = _sigScanner.ScanText("48 89 5C 24 ?? 57 B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 4C 8B 49");
        if (originalReadSqpkChunkAddr == 0)
        {
            _logger.LogError("Could not find function ReadSqpkChunk");
            Status = TweakStatus.Error;
            return;
        }

        var assembler = new Assembler(64);

        assembler.mov(rax, (ulong)_nativeService.ReadSqpkChunkAddr);
        assembler.jmp(rax);

        using var stream = new MemoryStream();
        var writer = new StreamCodeWriter(stream);
        assembler.Assemble(writer, 0);

        _patch ??= new MemoryReplacement(originalReadSqpkChunkAddr, stream.ToArray());
        _patch.Enable();
    }

    public override void OnDisable()
    {
        _patch?.Dispose();
        _patch = null;
    }
}
