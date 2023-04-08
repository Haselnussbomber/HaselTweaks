using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.Raii;
using Dalamud.Logging;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using XXHash3NET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreatePresetDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly ConfirmationButton saveButton;

    private string? name;
    private PortraitPreset? preset;
    private Image<Bgra32>? image;
    private HashSet<Guid>? tags;

    public CreatePresetDialog() : base("Save as Preset")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
    }

    public void Open(string name, PortraitPreset? preset, Image<Bgra32>? image)
    {
        this.name = name;
        this.preset = preset;
        this.image = image;
        tags = new();
        Show();
    }

    public void Close()
    {
        Hide();
        name = null;
        preset = null;
        image?.Dispose();
        image = null;
        tags = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && name != null && preset != null && image != null && tags != null;

    public override void InnerDraw()
    {
        ImGui.Text("Enter a name for the new preset:");
        ImGui.Spacing();
        ImGui.InputText("##PresetName", ref name, 100);

        var disabled = string.IsNullOrEmpty(name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        ImGui.Spacing();

        ImGui.Text("Select Tags (optional):");
        ImGui.Spacing();

        var tagNames = tags!
            .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
            .Where(name => !string.IsNullOrEmpty(name));

        var preview = tagNames.Any() ? string.Join(", ", tagNames) : "None";

        using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
        if (tagsCombo.Success)
        {
            foreach (var tag in Config.PresetTags)
            {
                var isSelected = tags!.Contains(tag.Id);

                if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                {
                    if (isSelected)
                    {
                        tags.Remove(tag.Id);
                    }
                    else
                    {
                        tags.Add(tag.Id);
                    }
                }
            }
        }
        tagsCombo.Dispose();

        saveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (preset == null || image == null || string.IsNullOrEmpty(name?.Trim()))
        {
            PluginLog.Error("Could not save portrait: data missing"); // TODO: show error
            Close();
            return;
        }

        Hide();

        Task.Run(() =>
        {
            var pixelData = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(pixelData);

            var hash = XXHash3.Hash64(pixelData).ToString("x");
            var thumbPath = Config.GetPortraitThumbnailPath(hash);

            image.SaveAsPng(thumbPath, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Rgb // no need for alpha channel
            });

            Config.Presets.Insert(0, new(name.Trim(), preset, tags!, hash));
            Plugin.Config.Save();

            Close();
        });
    }
}
