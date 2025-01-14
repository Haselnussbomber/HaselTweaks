using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Extensions;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped]
public class CreatePresetDialog : ConfirmationDialog
{
    private readonly IDalamudPluginInterface PluginInterface;
    private readonly INotificationManager NotificationManager;
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    private readonly ConfirmationButton SaveButton;
    private string? Name;
    private PortraitPreset? Preset;
    private Image<Bgra32>? Image;
    private HashSet<Guid>? Tags;

    private PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    public CreatePresetDialog(
        IDalamudPluginInterface pluginInterface,
        INotificationManager notificationManager,
        PluginConfig pluginConfig,
        TextService textService)
        : base(textService.Translate("PortraitHelperWindows.CreatePresetDialog.Title"))
    {
        PluginInterface = pluginInterface;
        NotificationManager = notificationManager;
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(SaveButton = new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
    }

    public void Open(string name, PortraitPreset? preset, Image<Bgra32>? image)
    {
        Name = name;
        Preset = preset;
        Image = image;
        Tags = [];
        Show();
    }

    public void Close()
    {
        Hide();
        Name = null;
        Preset = null;
        Image?.Dispose();
        Image = null;
        Tags = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Name != null && Preset != null && Image != null && Tags != null;

    public override void InnerDraw()
    {
        TextService.Draw("PortraitHelperWindows.CreatePresetDialog.Name.Label");
        ImGui.Spacing();
        ImGui.InputText("##PresetName", ref Name, 100);

        var disabled = string.IsNullOrEmpty(Name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Count != 0)
        {
            ImGui.Spacing();
            TextService.Draw("PortraitHelperWindows.CreatePresetDialog.Tags.Label");

            var tagNames = Tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : TextService.Translate("PortraitHelperWindows.CreatePresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo.Success)
            {
                foreach (var tag in Config.PresetTags)
                {
                    var isSelected = Tags!.Contains(tag.Id);

                    if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                    {
                        if (isSelected)
                        {
                            Tags.Remove(tag.Id);
                        }
                        else
                        {
                            Tags.Add(tag.Id);
                        }
                    }
                }
            }
        }

        SaveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (Preset == null || Image == null || string.IsNullOrEmpty(Name?.Trim()))
        {
            NotificationManager.AddNotification(new()
            {
                Title = "Could not save portrait"
            });
            Close();
            return;
        }

        Hide();

        Task.Run(() =>
        {
            var guid = Guid.NewGuid();
            var thumbPath = PluginInterface.GetPortraitThumbnailPath(guid);

            if (Config.EmbedPresetStringInThumbnails)
            {
                Image.Metadata.ExifProfile ??= new();
                Image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, Preset.ToExportedString());
            }

            Image.SaveAsPng(thumbPath, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Rgb // no need for alpha channel
            });

            Config.Presets.Insert(0, new(guid, Name.Trim(), Preset, Tags!));
            PluginConfig.Save();

            Close();
        });
    }
}
