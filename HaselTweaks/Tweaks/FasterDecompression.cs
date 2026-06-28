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
        if (_nativeService.InflateAddr == 0)
        {
            _logger.LogError("Could not find function Inflate");
            Status = TweakStatus.Error;
            return;
        }

        var zlibUncompressAddr = _sigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5C 24 ?? 44 8B C3");
        if (zlibUncompressAddr == 0)
        {
            _logger.LogError("Could not find function zlib.uncompress");
            Status = TweakStatus.Error;
            return;
        }

        var assembler = new Assembler(64);

        assembler.mov(rax, (ulong)_nativeService.InflateAddr);
        assembler.jmp(rax);

        using var stream = new MemoryStream();
        var writer = new StreamCodeWriter(stream);
        assembler.Assemble(writer, 0);

        _patch ??= new MemoryReplacement(zlibUncompressAddr, stream.ToArray());
        _patch.Enable();
    }

    public override void OnDisable()
    {
        _patch?.Dispose();
        _patch = null;
    }
}
