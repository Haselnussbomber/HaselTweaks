using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HaselCommon.Extensions.Collections;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Extensions;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public class PresetCard : IDisposable
{
    public static readonly Vector2 PortraitSize = new(576, 960); // native texture size

    private readonly uint ButtonActiveColor = Color.White with { A = 0.3f };
    private readonly uint ButtonHoveredColor = Color.White with { A = 0.2f };

    private readonly ILogger Logger;
    private readonly IDalamudPluginInterface PluginInterface;
    private readonly IDataManager DataManager;
    private readonly ITextureProvider TextureProvider;
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;
    private readonly ExcelService ExcelService;
    private readonly BannerUtils BannerUtils;
    private readonly SavedPreset Preset;
    private readonly uint BannerFrameImage;
    private readonly uint BannerDecorationImage;
    private readonly bool IsBannerTimelineUnlocked;
    private readonly bool IsBannerBgUnlocked;
    private readonly bool IsBannerFrameUnlocked;
    private readonly bool IsBannerDecorationUnlocked;

    private CancellationTokenSource? CloseTokenSource;

    private bool IsImageLoading;
    private bool DoesImageFileExist;
    private bool IsImageUpdatePending;

    private Guid? TextureGuid;
    private Image<Rgba32>? Image;
    private IDalamudTextureWrap? TextureWrap;
    private DateTime LastTextureCheck = DateTime.MinValue;

    private float LastScale;

    public PresetCard(
        SavedPreset preset,
        ILogger logger,
        IDalamudPluginInterface pluginInterface,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        PluginConfig pluginConfig,
        TextService textService,
        ExcelService excelService,
        BannerUtils bannerUtils)
    {
        Preset = preset;
        Logger = logger;
        PluginInterface = pluginInterface;
        DataManager = dataManager;
        TextureProvider = textureProvider;
        PluginConfig = pluginConfig;
        TextService = textService;
        ExcelService = excelService;
        BannerUtils = bannerUtils;

        if (excelService.TryGetRow<BannerFrame>(Preset.Preset!.BannerFrame, out var bannerFrameRow))
            BannerFrameImage = (uint)bannerFrameRow.Image;

        if (excelService.TryGetRow<BannerDecoration>(Preset.Preset.BannerDecoration, out var bannerDecorationImageRow))
            BannerDecorationImage = (uint)bannerDecorationImageRow.Image;

        IsBannerTimelineUnlocked = bannerUtils.IsBannerTimelineUnlocked(Preset.Preset.BannerTimeline);
        IsBannerBgUnlocked = bannerUtils.IsBannerBgUnlocked(Preset.Preset.BannerBg);
        IsBannerFrameUnlocked = bannerUtils.IsBannerFrameUnlocked(Preset.Preset.BannerFrame);
        IsBannerDecorationUnlocked = bannerUtils.IsBannerDecorationUnlocked(Preset.Preset.BannerDecoration);
    }

    public void Dispose()
    {
        CloseTokenSource?.Cancel();
        CloseTokenSource?.Dispose();
        Image?.Dispose();
        TextureWrap?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Draw(PresetBrowserOverlay overlay, float scale, uint defaultImGuiTextColor)
    {
        Update(scale);

        var hasErrors = !IsBannerTimelineUnlocked || !IsBannerBgUnlocked || !IsBannerFrameUnlocked || !IsBannerDecorationUnlocked;
        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(Preset.Id.ToString());

        var cursorPos = ImGui.GetCursorPos();
        var center = cursorPos + PortraitSize * scale / 2f;

        var TextureService = Service.Get<TextureService>();

        TextureService.DrawIcon(190009, PortraitSize * scale);
        ImGui.SetCursorPos(cursorPos);

        if (IsImageLoading)
        {
            DrawLoadingSpinner(ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + center);
        }
        else if (!DoesImageFileExist)
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            using var color = Color.Red.Push(ImGuiCol.Text);
            ImGui.SetCursorPos(center - ImGui.CalcTextSize(FontAwesomeIcon.FileImage.ToIconString()) / 2f);
            ImGui.TextUnformatted(FontAwesomeIcon.FileImage.ToIconString());
        }
        else if (TextureWrap != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(TextureWrap.ImGuiHandle, PortraitSize * scale);
        }

        if (BannerFrameImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            TextureService.DrawIcon(BannerFrameImage, PortraitSize * scale);
        }

        if (BannerDecorationImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            TextureService.DrawIcon(BannerDecorationImage, PortraitSize * scale);
        }

        if (hasErrors)
        {
            ImGui.SetCursorPos(cursorPos + new Vector2(PortraitSize.X - 190, 10) * scale);
            TextureService.Draw("ui/uld/Warning_hr1.tex", 160 * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        {
            using var Color = ImRaii.PushColor(ImGuiCol.Button, 0)
                .Push(ImGuiCol.ButtonActive, ButtonActiveColor)
                .Push(ImGuiCol.ButtonHovered, ButtonHoveredColor);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"##{Preset.Id}_Button", PortraitSize * scale);
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.MovingPresetCard.Tooltip", Preset.Name));

                unsafe
                {
                    var bytes = Preset.Id.ToByteArray();
                    fixed (byte* ptr = bytes)
                    {
                        ImGui.SetDragDropPayload("MovePresetCard", (nint)ptr, (uint)bytes.Length);
                    }
                }
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target)
            {
                var payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                unsafe
                {
                    if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                    {
                        var config = PluginConfig.Tweaks.PortraitHelper;
                        var presetId = MemoryHelper.Read<Guid>(payload.Data).ToString();
                        var oldIndex = config.Presets.AsEnumerable().IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = config.Presets.IndexOf(Preset);
                        var item = config.Presets[oldIndex];
                        config.Presets.RemoveAt(oldIndex);
                        config.Presets.Insert(newIndex, item);
                        PluginConfig.Save();
                    }
                }
            }
        }

        if (ImGui.IsItemHovered() && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.TextUnformatted(Preset.Name);

                if (hasErrors)
                {
                    ImGui.Separator();

                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Red))
                    {
                        ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.Title"));

                        using (ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 2))
                        using (ImRaii.PushIndent(1))
                        {
                            if (!IsBannerTimelineUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.PoseNotUnlocked", BannerUtils.GetBannerTimelineName(Preset.Preset!.BannerTimeline)));
                            }

                            if (!IsBannerBgUnlocked && ExcelService.TryGetRow<BannerBg>(Preset.Preset!.BannerBg, out var bannerBgRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.BackgroundNotUnlocked", bannerBgRow.Name.ExtractText()));
                            }

                            if (!IsBannerFrameUnlocked && ExcelService.TryGetRow<BannerFrame>(Preset.Preset!.BannerFrame, out var bannerFrameRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.FrameNotUnlocked", bannerFrameRow.Name.ExtractText()));
                            }

                            if (!IsBannerDecorationUnlocked && ExcelService.TryGetRow<BannerDecoration>(Preset.Preset!.BannerDecoration, out var bannerDecorationRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(TextService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.DecorationNotUnlocked", bannerDecorationRow.Name.ExtractText()));
                            }
                        }
                    }
                }
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                Preset.Preset?.ToState(Logger, BannerUtils, ImportFlags.All);
                overlay.MenuBar.CloseOverlays();
            }
        }

        using var popup = ImRaii.ContextPopupItem($"{Preset.Id}_Popup");
        if (!popup)
            return;

        using var textColor = ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor);

        if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.LoadPreset.Label")))
        {
            Preset.Preset?.ToState(Logger, BannerUtils, ImportFlags.All);
            overlay.MenuBar.CloseOverlays();
        }

        if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.EditPreset.Label")))
        {
            overlay.EditPresetDialog.Open(Preset);
        }

        if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.ExportToClipboard.Label")))
        {
            Preset.Preset?.ToClipboard(Logger);
        }

        if (Image != null && ImGui.BeginMenu(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Label")))
        {
            if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Everything.Label")))
            {
                Task.Run(async () => await CopyImage());
            }

            if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrame.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame));
            }

            if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutDecoration.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoDecoration));
            }

            if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrameAndDecoration.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame | CopyImageFlags.NoDecoration));
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.MenuItem(TextService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.DeletePreset.Label")))
        {
            overlay.DeletePresetDialog.Open(overlay, Preset);
        }
    }

    public static void DrawLoadingSpinner(Vector2 center, float radius = 10f)
    {
        var angle = 0.0f;
        var numSegments = 10;
        var angleStep = (float)(Math.PI * 2.0f / numSegments);
        var time = ImGui.GetTime();
        var drawList = ImGui.GetWindowDrawList();

        for (var i = 0; i < numSegments; i++)
        {
            var pos = center + new Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle));

            var t = (float)(-angle / (float)Math.PI / 2f + time) % 1f;
            var color = new Vector4(1f, 1f, 1f, 1 - t);

            drawList.AddCircleFilled(pos, 2f, ImGui.ColorConvertFloat4ToU32(color));

            angle += angleStep;
        }
    }

    private async Task CopyImage(CopyImageFlags flags = CopyImageFlags.None)
    {
        if (Image == null)
            return;

        using var tempImage = Image.Clone();

        if (!flags.HasFlag(CopyImageFlags.NoFrame) && BannerFrameImage != 0)
        {
            if (TextureProvider.TryGetIconPath(BannerFrameImage, out var iconPath))
            {
                var texture = DataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && BannerDecorationImage != 0)
        {
            if (TextureProvider.TryGetIconPath(BannerDecorationImage, out var iconPath))
            {
                var texture = DataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        await ClipboardUtils.SetClipboardImage(tempImage);
    }

    private void Update(float scale)
    {
        if (!IsImageLoading && Preset.Id != TextureGuid)
        {
            Image?.Dispose();
            Image = null;

            TextureWrap?.Dispose();
            TextureWrap = null;

            if (DateTime.Now - LastTextureCheck > TimeSpan.FromSeconds(1))
            {
                var thumbPath = PluginInterface.GetPortraitThumbnailPath(Preset.Id);

                if (File.Exists(thumbPath))
                {
                    IsImageLoading = true;
                    CloseTokenSource ??= new();
                    Task.Run(async () =>
                    {
                        try
                        {
                            Image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(thumbPath, CloseTokenSource.Token);
                            IsImageUpdatePending = true;
                            DoesImageFileExist = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Error while loading thumbnail");
                            IsImageLoading = false;
                            DoesImageFileExist = false;
                        }
                        finally
                        {
                            TextureGuid = Preset.Id;
                        }
                    }, CloseTokenSource.Token);
                }

                LastTextureCheck = DateTime.Now;
            }
        }

        if (scale != LastScale)
        {
            IsImageUpdatePending = true;
            LastScale = scale;
        }

        if (Image != null && IsImageUpdatePending)
        {
            TextureWrap?.Dispose();
            IsImageUpdatePending = false;
            CloseTokenSource ??= new();

            Task.Run(() =>
            {
                try
                {
                    using var scaledImage = Image.Clone();

                    if (CloseTokenSource.IsCancellationRequested)
                        return;

                    scaledImage.Mutate(i => i.Resize((int)(PortraitSize.X * scale), (int)(PortraitSize.Y * scale), KnownResamplers.Lanczos3));

                    if (CloseTokenSource.IsCancellationRequested)
                        return;

                    var data = new byte[4 * scaledImage.Width * scaledImage.Height];
                    scaledImage.CopyPixelDataTo(data);

                    if (CloseTokenSource.IsCancellationRequested)
                        return;

                    TextureWrap = TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(scaledImage.Width, scaledImage.Height), data);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while resizing/loading thumbnail");
                }
                finally
                {
                    IsImageLoading = false;
                }
            }, CloseTokenSource.Token);
        }
    }
}
