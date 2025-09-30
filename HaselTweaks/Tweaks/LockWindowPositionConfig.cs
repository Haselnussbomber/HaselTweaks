using ZLinq;

namespace HaselTweaks.Tweaks;

public class LockWindowPositionConfiguration
{
    public bool Inverted = false;
    public bool AddLockUnlockContextMenuEntries = true;
    public List<LockedWindowSetting> LockedWindows = [];

    public record LockedWindowSetting
    {
        public bool Enabled = true;
        public string Name = "";
    }
}

public partial class LockWindowPosition
{
    public override void OnConfigClose()
    {
        _hoveredWindowName = "";
        _hoveredWindowPos = default;
        _hoveredWindowSize = default;
        _showPicker = false;
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();

        if (ImGui.Checkbox(_textService.Translate("LockWindowPosition.Config.Inverted.Label"), ref _config.Inverted))
        {
            _pluginConfig.Save();
        }

        if (ImGui.Checkbox(_textService.Translate("LockWindowPosition.Config.AddLockUnlockContextMenuEntries.Label"), ref _config.AddLockUnlockContextMenuEntries))
        {
            _pluginConfig.Save();
        }

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        ImGuiUtils.DrawPaddedSeparator();
        if (_config.LockedWindows.Count != 0)
        {
            ImGui.Text(_textService.Translate("LockWindowPosition.Config.Windows.Title"));

            if (!ImGui.BeginTable("##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX))
            {
                return;
            }

            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash).X);

            var entryToRemove = -1;
            var i = 0;

            foreach (var entry in _config.LockedWindows)
            {
                var key = $"##Table_Row{i}";
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (ImGui.Checkbox(key + "_Enabled", ref entry.Enabled))
                {
                    _pluginConfig.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    var isLocked = entry.Enabled;

                    if (_config.Inverted)
                        isLocked = !isLocked;

                    ImGui.BeginTooltip();
                    ImGui.Text(_textService.Translate(isLocked
                        ? "LockWindowPosition.Config.EnableCheckmark.Tooltip.Locked"
                        : "LockWindowPosition.Config.EnableCheckmark.Tooltip.Unlocked"));
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                ImGui.Text(entry.Name);

                ImGui.TableNextColumn();
                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton(key + "_Delete", FontAwesomeIcon.Trash, _textService.Translate("LockWindowPosition.Config.DeleteButton.Tooltip")))
                    {
                        entryToRemove = i;
                    }
                }
                else
                {
                    ImGuiUtils.IconButton(
                        key + "_Delete",
                        FontAwesomeIcon.Trash,
                        _textService.Translate(isWindowFocused
                            ? "LockWindowPosition.Config.DeleteButton.Tooltip.NotHoldingShift"
                            : "LockWindowPosition.Config.DeleteButton.Tooltip.WindowNotFocused"),
                        disabled: true);
                }

                i++;
            }

            ImGui.EndTable();

            if (entryToRemove != -1)
            {
                _config.LockedWindows.RemoveAt(entryToRemove);
                _pluginConfig.Save();
            }
        }
        else
        {
            using (ImRaii.Disabled())
                ImGui.Text(_textService.Translate("LockWindowPosition.Config.NoWindowsAddedYet"));
            ImGuiUtils.PushCursorY(4);
        }

        if (_showPicker)
        {
            if (ImGui.Button(_textService.Translate("LockWindowPosition.Config.Picker.CancelButton.Label")))
            {
                _showPicker = false;
            }
        }
        else
        {
            if (ImGui.Button(_textService.Translate("LockWindowPosition.Config.Picker.PickWindowButton.Label")))
            {
                _hoveredWindowName = "";
                _hoveredWindowPos = default;
                _hoveredWindowSize = default;
                _showPicker = true;
            }
        }

        if (_config.LockedWindows.Count != 0)
        {
            ImGui.SameLine();

            if (ImGui.Button(_textService.Translate("LockWindowPosition.Config.Picker.ToggleAllButton.Label")))
            {
                foreach (var entry in _config.LockedWindows)
                {
                    entry.Enabled = !entry.Enabled;
                }
                _pluginConfig.Save();
            }
        }

        if (_showPicker && _hoveredWindowPos != default)
        {
            ImGui.SetNextWindowPos(_hoveredWindowPos);
            ImGui.SetNextWindowSize(_hoveredWindowSize);

            using var windowStyles = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
            using var windowColor = Color.Gold.Push(ImGuiCol.Border)
                                              .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

            if (ImGui.Begin("Lock Windows Picker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var drawList = ImGui.GetForegroundDrawList();
                var textPos = _hoveredWindowPos + new Vector2(0, -ImGui.GetTextLineHeight());
                drawList.AddText(textPos + Vector2.One, Color.Black.ToUInt(), _hoveredWindowName);
                drawList.AddText(textPos, Color.Gold.ToUInt(), _hoveredWindowName);

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _showPicker = false;

                    if (_hoveredWindowName != "" && !_config.LockedWindows.Any(entry => entry.Name == _hoveredWindowName))
                    {
                        _config.LockedWindows.Add(new()
                        {
                            Name = _hoveredWindowName
                        });
                        _pluginConfig.Save();
                    }
                }

                ImGui.End();
            }
        }
    }
}
