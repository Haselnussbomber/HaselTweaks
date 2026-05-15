using System.Threading.Tasks;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterSingleton, AutoConstruct]
public partial class CreatePresetDialog
{
    private readonly ILogger<CreatePresetDialog> _logger;
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly ThumbnailService _thumbnailService;

    private bool _shouldOpen;
    private bool _isSaving;
    private string? _name;
    private PortraitPreset? _preset;
    private BgraImage? _image;
    private HashSet<Guid>? _tags;

    public void Open(string name, PortraitPreset? preset, BgraImage? image)
    {
        _name = name;
        _preset = preset;
        _image = image;
        _tags = [];
        _isSaving = false;
        _shouldOpen = true;
    }

    public void Draw()
    {
        if (_preset == null || _image == null || _tags == null)
            return;

        var title = _textService.Translate("PortraitHelperWindows.CreatePresetDialog.Title");

        if (_shouldOpen)
        {
            ImGui.OpenPopup(title);
            _shouldOpen = false;
        }

        if (!ImGui.IsPopupOpen(title))
            return;

        // Always center this window when appearing
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

        using var modal = ImRaii.PopupModal(title, ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal) return;

        using var disabledDueToSaving = ImRaii.Disabled(_isSaving);

        ImGui.Text(_textService.Translate("PortraitHelperWindows.CreatePresetDialog.Name.Label"));

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        var name = _name ?? string.Empty;
        if (ImGui.InputText("##PresetName", ref name, Constants.PresetNameMaxLength))
            _name = name;

        var availableTags = _pluginConfig.Tweaks.PortraitHelper.PresetTags;
        if (availableTags.Count != 0)
        {
            ImGui.Spacing();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.CreatePresetDialog.Tags.Label"));

            var tagNames = _tags
                .Select(id => availableTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : _textService.Translate("PortraitHelperWindows.CreatePresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo)
            {
                foreach (var tag in availableTags)
                {
                    var isSelected = _tags.Contains(tag.Id);

                    if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                    {
                        if (isSelected)
                        {
                            _tags.Remove(tag.Id);
                        }
                        else
                        {
                            _tags.Add(tag.Id);
                        }
                    }
                }
            }
        }

        var disabled = string.IsNullOrWhiteSpace(_name);
        var shouldSave = !disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var combinedButtonWidths = ImStyle.ItemSpacing.X
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Save")).X)
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Cancel")).X);

        ImCursor.X += (ImStyle.ContentRegionAvail.X - combinedButtonWidths) / 2f;

        using (ImRaii.Disabled(disabled))
        {
            if (ImGui.Button(_textService.Translate(_isSaving ? "ConfirmationButtonWindow.Saving" : "ConfirmationButtonWindow.Save"), new Vector2(120, 0)) || shouldSave)
            {
                _isSaving = true;

                _ = Task.Run(() =>
                {
                    try
                    {
                        var guid = Guid.CreateVersion7();
                        var thumbPath = _thumbnailService.GetPortraitThumbnailPath(guid);

                        _image.SaveAsPng(thumbPath, _preset.ToExportedString());

                        _pluginConfig.Tweaks.PortraitHelper.Presets.Insert(0, new(guid, name.Trim(), _preset, _tags));
                        _pluginConfig.Save();

                        _name = string.Empty;
                        _preset = null;
                        _image.Dispose();
                        _image = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not create preset");
                    }

                    _isSaving = false;
                    ImGui.CloseCurrentPopup();
                });
            }
        }

        ImGui.SetItemDefaultFocus();
        ImGui.SameLine();
        if (ImGui.Button(_textService.Translate("ConfirmationButtonWindow.Cancel"), new Vector2(120, 0)))
        {
            _name = string.Empty;
            _preset = null;
            _image.Dispose();
            _image = null;
            ImGui.CloseCurrentPopup();
        }
    }
}
