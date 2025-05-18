using System.IO;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Utils;

[RegisterSingleton, AutoConstruct]
public unsafe partial class BannerUtils
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    public Image<Bgra32>? GetCurrentCharaViewImage()
    {
        var agent = AgentBannerEditor.Instance();
        if (agent->EditorState == null || agent->EditorState->CharaView == null)
            return null;

        var charaViewTexture = RenderTargetManager.Instance()->GetCharaViewTexture(agent->EditorState->CharaView->ClientObjectIndex);
        if (charaViewTexture == null || charaViewTexture->D3D11Texture2D == null)
            return null;

        var device = _pluginInterface.UiBuilder.Device;
        var texture = CppObject.FromPointer<Texture2D>((nint)charaViewTexture->D3D11Texture2D);

        // thanks to ChatGPT
        // Get the texture description
        var desc = texture.Description;

        // Create a staging texture with the same description
        using var stagingTexture = new Texture2D(device, new Texture2DDescription()
        {
            ArraySize = 1,
            BindFlags = BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.Read,
            Format = desc.Format,
            Height = desc.Height,
            Width = desc.Width,
            MipLevels = 1,
            OptionFlags = desc.OptionFlags,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging
        });

        // Copy the texture data to the staging texture
        device.ImmediateContext.CopyResource(texture, stagingTexture);

        // Map the staging texture
        device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var dataStream);

        using var pixelDataStream = new MemoryStream();
        dataStream.CopyTo(pixelDataStream);

        // Unmap the staging texture
        device.ImmediateContext.UnmapSubresource(stagingTexture, 0);

        return Image.LoadPixelData<Bgra32>(pixelDataStream.ToArray(), desc.Width, desc.Height);
    }

    public bool IsBannerBgUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerBg>(id, out var bannerBg))
            return false;

        return IsBannerConditionUnlocked(bannerBg.UnlockCondition.RowId);
    }

    public bool IsBannerFrameUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerFrame>(id, out var bannerFrame))
            return false;

        return IsBannerConditionUnlocked(bannerFrame.UnlockCondition.RowId);
    }

    public bool IsBannerDecorationUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerDecoration>(id, out var bannerDecoration))
            return false;

        return IsBannerConditionUnlocked(bannerDecoration.UnlockCondition.RowId);
    }

    public bool IsBannerTimelineUnlocked(uint id)
    {
        if (!_excelService.TryGetRow<BannerTimeline>(id, out var bannerTimeline))
            return false;

        return IsBannerConditionUnlocked(bannerTimeline.UnlockCondition.RowId);
    }

    public bool IsBannerConditionUnlocked(uint id)
    {
        if (id == 0)
            return true;

        var bannerCondition = HaselExdModule.GetBannerConditionByIndex(id);
        if (bannerCondition == null)
            return false;

        return ExdModule.GetBannerConditionUnlockState(bannerCondition) == 0;
    }

    public string GetBannerTimelineName(uint id)
    {
        if (!_excelService.TryGetRow<BannerTimeline>(id, out var bannerTimeline))
            return _textService.GetAddonText(624); // Unknown

        var poseName = bannerTimeline.Name.ExtractText();

        if (string.IsNullOrEmpty(poseName) && bannerTimeline.Type != 0)
        {
            if (bannerTimeline.AdditionalData.TryGetValue<ActionSheet>(out var actionRow))
                poseName = actionRow.Name.ExtractText();
            else if (bannerTimeline.AdditionalData.TryGetValue<Emote>(out var emoteRow))
                poseName = emoteRow.Name.ExtractText();
        }

        return !string.IsNullOrEmpty(poseName)
            ? poseName
            : _textService.GetAddonText(624);
    }
}
