using System.IO;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Utils;

public unsafe class BannerUtils(IDalamudPluginInterface PluginInterface, ExcelService ExcelService, TextService TextService)
{
    public Image<Bgra32>? GetCurrentCharaViewImage()
    {
        var agent = AgentBannerEditor.Instance();
        if (agent->EditorState == null || agent->EditorState->CharaView == null)
            return null;

        var charaViewTexture = RenderTargetManager.Instance()->GetCharaViewTexture(agent->EditorState->CharaView->ClientObjectIndex);
        if (charaViewTexture == null || charaViewTexture->D3D11Texture2D == null)
            return null;

        var device = PluginInterface.UiBuilder.Device;
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
        var bannerBg = ExcelService.GetRow<BannerBg>(id);
        if (bannerBg == null)
            return false;

        return IsBannerConditionUnlocked(bannerBg.UnlockCondition.Row);
    }

    public bool IsBannerFrameUnlocked(uint id)
    {
        var bannerFrame = ExcelService.GetRow<BannerFrame>(id);
        if (bannerFrame == null)
            return false;

        return IsBannerConditionUnlocked(bannerFrame.UnlockCondition.Row);
    }

    public bool IsBannerDecorationUnlocked(uint id)
    {
        var bannerDecoration = ExcelService.GetRow<BannerDecoration>(id);
        if (bannerDecoration == null)
            return false;

        return IsBannerConditionUnlocked(bannerDecoration.UnlockCondition.Row);
    }

    public bool IsBannerTimelineUnlocked(uint id)
    {
        var bannerTimeline = ExcelService.GetRow<BannerTimeline>(id);
        if (bannerTimeline == null)
            return false;

        return IsBannerConditionUnlocked(bannerTimeline.UnlockCondition.Row);
    }

    public bool IsBannerConditionUnlocked(uint id)
    {
        if (id == 0)
            return true;

        var bannerCondition = BannerConditionRow.GetByRowId(id);
        if (bannerCondition == null)
            return false;

        return bannerCondition->GetUnlockState() == 0;
    }

    public string GetBannerTimelineName(uint id)
    {
        var poseName = ExcelService.GetRow<BannerTimeline>(id)?.Name.ExtractText();

        if (string.IsNullOrEmpty(poseName))
        {
            var bannerTimeline = ExcelService.GetRow<BannerTimeline>(id);
            if (bannerTimeline != null && bannerTimeline.Type != 0)
            {
                // ref: "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 41 8B C9 49 8B F8"
                if (bannerTimeline.Type <= 2)
                {
                    poseName = TextService.GetActionName(bannerTimeline.AdditionalData);
                }
                else if (bannerTimeline.Type - 10 <= 1)
                {
                    poseName = TextService.GetEmoteName(bannerTimeline.AdditionalData);
                }
            }
        }

        return !string.IsNullOrEmpty(poseName) ?
            poseName :
            TextService.GetAddonText(624); // Unknown
    }
}
