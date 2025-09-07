using System.Text;
using System.Text.Json.Serialization;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

namespace HaselTweaks.Tweaks;

public class CustomChatMessageFormatsConfiguration
{
    public Dictionary<uint, LogKindOverride> FormatOverrides = [];

    public record LogKindOverride
    {
        public bool Enabled = true;
        public ReadOnlySeString Format;

        [JsonIgnore]
        public bool EditMode;

        public LogKindOverride(ReadOnlySeString format)
        {
            Format = format;
        }

        public bool IsValid()
        {
            var colorCount = 0;

            foreach (var payload in Format)
            {
                if (payload.Type == ReadOnlySePayloadType.Macro && payload.MacroCode == MacroCode.Color)
                {
                    if (!payload.TryGetExpression(out var eColor))
                        continue;

                    if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
                        colorCount--;
                    else
                        colorCount++;
                }

                if (colorCount < 0) // if StackColor before Color
                    return false;
            }

            return colorCount == 0;
        }
    }
}

public partial class CustomChatMessageFormats
{
    public CustomChatMessageFormatsConfiguration Config => _pluginConfig.Tweaks.CustomChatMessageFormats;

    private bool _isConfigWindowOpen;
    private List<(LogKind LogKind, LogFilter LogFilter, ReadOnlySeString Format)>? _cachedLogKindRows = null;
    private TextColorEntry[]? _cachedTextColor = null;

    public override void OnConfigOpen()
    {
        _isConfigWindowOpen = true;
    }

    public override void OnConfigClose()
    {
        _isConfigWindowOpen = false;
        _cachedLogKindRows = null;
        _cachedTextColor = null;
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();

        _cachedLogKindRows ??= GenerateLogKindCache();
        _cachedTextColor ??= GenerateTextColor();

        var ItemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;

        using var cellpadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ItemInnerSpacing * ImGuiHelpers.GlobalScale);
        using var table = ImRaii.Table("##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX);
        if (!table)
            return;
        cellpadding?.Dispose();

        ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Channel Name and Format", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed,
            ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Pen).X + ItemInnerSpacing.X + ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash).X);

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        var entryToEdit = 0u;
        var entryToRemove = 0u;

        foreach (var (logKindId, entry) in Config.FormatOverrides)
        {
            using var id = ImRaii.PushId("Setting[" + logKindId.ToString() + "]");
            var isValid = entry.IsValid();

            ImGui.TableNextRow();

            // Enabled
            ImGui.TableNextColumn();
            {
                ImGui.Checkbox("##Enabled", ref entry.Enabled);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(_textService.Translate(entry.Enabled
                        ? "CustomChatMessageFormats.Config.Entry.EnableCheckbox.Tooltip.IsEnabled"
                        : "CustomChatMessageFormats.Config.Entry.EnableCheckbox.Tooltip.IsDisabled"));
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemClicked())
                    SaveAndReloadChat();
            }

            // Channel Name and Format
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(-2 * ImGuiHelpers.GlobalScale);

                if (!isValid)
                {
                    ImGuiUtils.Icon(FontAwesomeIcon.ExclamationCircle, Color.Red.ToUInt());

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(_textService.Translate("CustomChatMessageFormats.Config.Entry.InvalidPayloads.Tooltip"));
                        ImGui.EndTooltip();
                    }

                    ImGuiUtils.SameLineSpace();
                }

                ImGui.Text(GetLogKindlabel(logKindId));

                DrawExample(entry.Format);

                if (entry.EditMode)
                {
                    ImGuiUtils.PushCursorY(3 * ImGuiHelpers.GlobalScale);
                    DrawEditMode(entry);
                }
            }

            // Actions
            ImGui.TableNextColumn();
            {
                if (entry.EditMode)
                {
                    if (ImGuiUtils.IconButton("##CloseEditor", FontAwesomeIcon.FilePen, _textService.Translate("CustomChatMessageFormats.Config.Entry.CloseEditorButton.Tooltip")))
                    {
                        entry.EditMode = false;
                    }
                }
                else
                {
                    if (ImGuiUtils.IconButton("##OpenEditor", FontAwesomeIcon.Pen, _textService.Translate("CustomChatMessageFormats.Config.Entry.OpenEditorButton.Tooltip")))
                    {
                        entryToEdit = logKindId;
                    }
                }

                ImGui.SameLine(0, ItemInnerSpacing.X);

                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton("##Delete", FontAwesomeIcon.Trash, _textService.Translate("HaselTweaks.Config.Generic.DeleteButton.Tooltip.HoldingShift")))
                    {
                        entryToRemove = logKindId;
                    }
                }
                else
                {
                    ImGuiUtils.IconButton(
                        "##Delete",
                        FontAwesomeIcon.Trash,
                        _textService.Translate(isWindowFocused
                            ? "HaselTweaks.Config.Generic.DeleteButton.Tooltip.NotHoldingShift"
                            : "HaselTweaks.Config.Generic.DeleteButton.Tooltip.WindowNotFocused"),
                        disabled: true);
                }
            }
        }

        table?.Dispose();

        if (entryToEdit != 0)
        {
            Config.FormatOverrides[entryToEdit].EditMode = !Config.FormatOverrides[entryToEdit].EditMode;
        }

        if (entryToRemove != 0)
        {
            Config.FormatOverrides.Remove(entryToRemove);
            SaveAndReloadChat();
        }

        ImGui.Spacing();

        var entries = _cachedLogKindRows.Where(entry => !Config.FormatOverrides.ContainsKey(entry.LogKind.RowId));
        var entriesCount = entries.Count();
        if (entriesCount > 0)
        {
            ImGui.SetNextItemWidth(-1);
            using var combo = ImRaii.Combo("##LogKindSelect", _textService.Translate("CustomChatMessageFormats.LogKindSelect.PreviewValue"), ImGuiComboFlags.HeightLarge);
            if (combo)
            {
                var i = 0;
                foreach (var entry in entries)
                {
                    if (ImGui.Selectable($"{GetLogKindlabel(entry.LogKind.RowId)}##LogKind{entry.LogKind.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2)))
                    {
                        Config.FormatOverrides.Add(entry.LogKind.RowId, new(entry.Format));
                        SaveAndReloadChat();
                    }

                    var afterCursor = ImGui.GetCursorPos();

                    ImGuiUtils.PushCursor(14 * ImGuiHelpers.GlobalScale, -ImGui.GetTextLineHeight() - ImGui.GetStyle().ItemSpacing.Y);
                    DrawExample(entry.Format);

                    ImGui.SetCursorPos(afterCursor);

                    if (i < entriesCount - 1)
                        ImGui.Separator();
                    i++;
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(_textService.Translate("CustomChatMessageFormats.Config.OpenConfigLogColorButton.Tooltip")))
        {
            unsafe
            {
                AgentModule.Instance()->GetAgentByInternalId(AgentId.ConfigLogColor)->Show();
            }
        }
    }

    private void SaveAndReloadChat()
    {
        _pluginConfig.Save();
        ReloadChat();
    }

    private void DrawEditMode(CustomChatMessageFormatsConfiguration.LogKindOverride entry)
    {
        using var payloadTable = ImRaii.Table("##PayloadTable", 3, ImGuiTableFlags.Borders);
        if (!payloadTable)
            return;

        var ItemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;
        var ArrowUpButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowUp);
        var ArrowDownButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowDown);
        var TrashButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash);

        ImGui.TableSetupColumn(_textService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Payload.Label"), ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn(_textService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Value.Label"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(_textService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Actions.Label"), ImGuiTableColumnFlags.WidthFixed,
            ArrowUpButtonSize.X +
            ItemInnerSpacing.X +
            ArrowDownButtonSize.X +
            ItemInnerSpacing.X +
            TrashButtonSize.X);
        ImGui.TableHeadersRow();

        var isWindowFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        var i = 0;
        var entryToMoveUp = -1;
        var entryToMoveDown = -1;
        var entryToRemove = -1;

        foreach (var payload in entry.Format)
        {
            using var id = ImRaii.PushId("PayloadEdit[" + i.ToString() + "]");

            ImGui.TableNextRow();

            // Payload
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                if (payload.Type == ReadOnlySePayloadType.Text)
                    ImGui.Text("Text");
                else if (payload.Type == ReadOnlySePayloadType.Macro)
                    ImGui.Text(payload.MacroCode.ToString());
                else if (payload.Type == ReadOnlySePayloadType.Invalid)
                    ImGui.Text("Invalid");
            }

            // Value
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                if (payload.Type == ReadOnlySePayloadType.Text)
                {
                    var text = Encoding.UTF8.GetString(payload.Body.ToArray());
                    ImGui.SetNextItemWidth(-1);
                    ImGuiUtils.PushCursorY(-ImGui.GetStyle().CellPadding.Y);
                    if (ImGui.InputText($"##TextPayload{i}", ref text, 255))
                    {
                        var sb = new SeStringBuilder();
                        var j = 0;
                        foreach (var tempPayload in entry.Format)
                        {
                            if (i == j)
                                sb.Append(text);
                            else
                                sb.Append(tempPayload);
                            j++;
                        }
                        entry.Format = sb.ToReadOnlySeString();
                        SaveAndReloadChat();
                    }
                }
                else if (payload.Type == ReadOnlySePayloadType.Macro)
                {
                    switch (payload.MacroCode)
                    {
                        case MacroCode.Icon:
                            if (!payload.TryGetExpression(out var iconExpr) || !iconExpr.TryGetUInt(out var iconId))
                                break;

                            _gfdService.Draw(iconId, 20);

                            ImGui.SameLine();

                            if (ImGui.Button(_textService.Translate("CustomChatMessageFormats.Config.Entry.OpenIconSelectorButton.Label")))
                            {
                                ImGui.OpenPopup("##IconSelector");
                            }

                            using (var iconSelectMenu = ImRaii.Popup("##IconSelector", ImGuiWindowFlags.NoMove))
                            {
                                if (iconSelectMenu)
                                {
                                    var maxLineWidth = 20 * 20;
                                    var posStart = ImGui.GetCursorPosX();

                                    foreach (var selectorGfdEntry in _gfdService.Entries)
                                    {
                                        if (selectorGfdEntry.IsEmpty || selectorGfdEntry.Id is 69 or 123)
                                            continue;

                                        var startPos = ImGui.GetCursorPos();
                                        using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, 0);
                                        using var buttonActiveColor = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xAAFFFFFF);
                                        using var buttonHoveredColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0x77FFFFFF);
                                        using var buttonRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
                                        var size = selectorGfdEntry.Size * 2f;
                                        if (ImGui.Button($"##Icon{selectorGfdEntry.Id}", size))
                                        {
                                            var sb = new SeStringBuilder();
                                            var j = 0;
                                            foreach (var tempPayload in entry.Format)
                                            {
                                                if (i == j)
                                                    sb.AppendIcon(selectorGfdEntry.Id);
                                                else
                                                    sb.Append(tempPayload);
                                                j++;
                                            }
                                            entry.Format = sb.ToReadOnlySeString();
                                            SaveAndReloadChat();
                                        }

                                        ImGui.SetCursorPos(startPos);
                                        _gfdService.Draw(selectorGfdEntry.Id, size);

                                        ImGui.SameLine();

                                        if (ImGui.GetCursorPosX() - posStart > maxLineWidth)
                                            ImGui.NewLine();
                                    }
                                }
                            }

                            break;

                        case MacroCode.String:
                            {
                                if (payload.TryGetExpression(out var strExpr)
                                    && strExpr.TryGetParameterExpression(out var expressionType, out var operand)
                                    && (ExpressionType)expressionType == ExpressionType.LocalString
                                    && operand.TryGetUInt(out var uintVal))
                                {
                                    if (uintVal == 1)
                                    {
                                        ImGui.Text(_textService.Translate("CustomChatMessageFormats.Config.LStr1.Label")); // "Player Name"
                                        break;
                                    }
                                    if (uintVal == 2)
                                    {
                                        ImGui.Text(_textService.Translate("CustomChatMessageFormats.Config.LStr2.Label")); // "Message"
                                        break;
                                    }
                                }

                                ImGui.Text(payload.ToString());
                                break;
                            }

                        case MacroCode.Color:
                            {
                                if (!payload.TryGetExpression(out var eColor))
                                    break;

                                if (eColor.TryGetParameterExpression(out var eColorExprType, out var operand)
                                    && (ExpressionType)eColorExprType == ExpressionType.GlobalNumber
                                    && operand.TryGetUInt(out var parameterIndex)
                                    && TryGetGNumDefault(parameterIndex - 1, out var eColorVal))
                                {
                                    using (ImRaii.PushColor(ImGuiCol.Text, SwapRedBlue(eColorVal)))
                                    {
                                        var textColorEntry = _cachedTextColor?.FirstOrDefault(entry => entry.GNumIndex == parameterIndex);
                                        if (textColorEntry != null)
                                            ImGui.Text(textColorEntry.Label);
                                        else
                                            ImGui.Text((parameterIndex - 1).ToString());
                                    }
                                    break;
                                }

                                if (eColor.TryGetUInt(out eColorVal))
                                {
                                    ImGui.SetNextItemWidth(-1);
                                    ImGuiUtils.PushCursorY(-ImGui.GetStyle().CellPadding.Y);
                                    var hexColor = Color.FromBGRA(eColorVal).ToVector();
                                    if (ImGui.ColorEdit4("##ColorPicker", ref hexColor, ImGuiColorEditFlags.NoAlpha))
                                    {
                                        var sb = new SeStringBuilder();
                                        var j = 0;
                                        foreach (var tempPayload in entry.Format)
                                        {
                                            if (i == j)
                                                sb.PushColorRgba(hexColor);
                                            else
                                                sb.Append(tempPayload);
                                            j++;
                                        }
                                        entry.Format = sb.ToReadOnlySeString();
                                        SaveAndReloadChat();
                                    }
                                    break;
                                }

                                ImGui.Text(payload.ToString());
                                break;
                            }

                        default:
                            ImGui.Text(payload.ToString());
                            break;
                    }

                }
                else if (payload.Type == ReadOnlySePayloadType.Invalid)
                {
                    ImGui.Text("Invalid");
                }

                if (ImGui.IsItemHovered())
                {
                    var isStringPlaceholder =
                        payload.Type == ReadOnlySePayloadType.Macro
                        && payload.MacroCode == MacroCode.String
                        && payload.TryGetExpression(out var expr)
                        && expr.TryGetParameterExpression(out _, out _);
                    if (isStringPlaceholder)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.StringPlaceholder.Tooltip"));
                        ImGui.EndTooltip();
                    }

                    var isStackColor =
                        payload.Type == ReadOnlySePayloadType.Macro
                        && payload.MacroCode == MacroCode.Color
                        && payload.TryGetExpression(out expr)
                        && expr.TryGetPlaceholderExpression(out var exprType)
                        && (ExpressionType)exprType == ExpressionType.StackColor;
                    if (isStackColor)
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.StackColor.Tooltip"));
                        ImGui.EndTooltip();
                    }
                }
            }

            // Actions
            ImGui.TableNextColumn();
            {
                if (i > 0)
                {
                    if (ImGuiUtils.IconButton("##Up", FontAwesomeIcon.ArrowUp, _textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.MoveUpButton.Tooltip")))
                    {
                        entryToMoveUp = i;
                    }
                }
                else
                {
                    ImGui.Dummy(ArrowUpButtonSize);
                }

                ImGui.SameLine(0, ItemInnerSpacing.X);

                if (i < entry.Format.PayloadCount - 1)
                {
                    if (ImGuiUtils.IconButton("##Down", FontAwesomeIcon.ArrowDown, _textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.MoveDownButton.Tooltip")))
                    {
                        entryToMoveDown = i;
                    }
                }
                else
                {
                    ImGui.Dummy(ArrowDownButtonSize);
                }

                if (payload.Type != ReadOnlySePayloadType.Macro || payload.MacroCode != MacroCode.String)
                {
                    ImGui.SameLine(0, ItemInnerSpacing.X);

                    if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                    {
                        if (ImGuiUtils.IconButton("##Delete", FontAwesomeIcon.Trash, _textService.Translate("HaselTweaks.Config.Generic.DeleteButton.Tooltip.HoldingShift")))
                        {
                            entryToRemove = i;
                        }
                    }
                    else
                    {
                        ImGuiUtils.IconButton(
                            "##Delete",
                            FontAwesomeIcon.Trash,
                            _textService.Translate(isWindowFocused
                                ? "HaselTweaks.Config.Generic.DeleteButton.Tooltip.NotHoldingShift"
                                : "HaselTweaks.Config.Generic.DeleteButton.Tooltip.WindowNotFocused"),
                            disabled: true);
                    }
                }
            }

            i++;
        }
        payloadTable?.Dispose();

        if (entryToMoveUp != -1)
        {
            var sb = new SeStringBuilder();
            ReadOnlySePayload? tempPayload = null;
            var j = 0;
            foreach (var payload in entry.Format)
            {
                if (j == entryToMoveUp - 1)
                {
                    tempPayload = payload;
                    j++;
                    continue;
                }

                if (j == entryToMoveUp && tempPayload != null)
                {
                    sb.Append(payload);
                    sb.Append((ReadOnlySePayload)tempPayload);
                    tempPayload = null;
                    j++;
                    continue;
                }

                sb.Append(payload);
                j++;
            }
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        if (entryToMoveDown != -1)
        {
            var sb = new SeStringBuilder();
            ReadOnlySePayload? tempPayload = null;
            var j = 0;
            foreach (var payload in entry.Format)
            {
                if (j == entryToMoveDown)
                {
                    tempPayload = payload;
                    j++;
                    continue;
                }

                sb.Append(payload);

                if (tempPayload != null)
                {
                    sb.Append((ReadOnlySePayload)tempPayload);
                    tempPayload = null;
                }

                j++;
            }

            if (tempPayload != null)
                sb.Append((ReadOnlySePayload)tempPayload);

            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        if (entryToRemove != -1)
        {
            var sb = new SeStringBuilder();
            var j = 0;
            foreach (var payload in entry.Format)
            {
                if (j != entryToRemove)
                    sb.Append(payload);
                j++;
            }
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        ImGui.Button(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Label"));

        using var contextMenu = ImRaii.ContextPopupItem("##AddPayloadContextMenu", ImGuiPopupFlags.MouseButtonLeft);
        if (!contextMenu)
            return;

        if (ImGui.MenuItem(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.TextPayload")))
        {
            var sb = new SeStringBuilder();
            sb.Append(entry.Format);
            sb.Append(" ");
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        if (ImGui.MenuItem(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.IconPayload")))
        {
            var sb = new SeStringBuilder();
            sb.Append(entry.Format);
            sb.AppendIcon(1);
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        if (ImGui.MenuItem(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.CustomColor")))
        {
            var sb = new SeStringBuilder();
            sb.Append(entry.Format);
            sb.PushColorRgba(0xFFFFFFFF);
            sb.PopColor();
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }

        if (ImGui.BeginMenu(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.LogTextColor"))) // GetAddonText(12732)
        {
            foreach (var textColorEntry in _cachedTextColor!)
            {
                if (textColorEntry.GNumIndex == 30) // skip Personal Emotes which is the same as Emotes
                    continue;

                var gNumIndex = textColorEntry.GNumIndex;

                if (!TryGetGNumDefault((uint)gNumIndex - 1, out var value))
                    continue;

                using (ImRaii.PushColor(ImGuiCol.Text, SwapRedBlue(value | 0xFF000000u)))
                {
                    if (ImGui.MenuItem(textColorEntry.Label + "##TextColor" + gNumIndex.ToString()))
                    {
                        var sb = new SeStringBuilder();
                        sb.Append(entry.Format);
                        sb.BeginMacro(MacroCode.Color).AppendGlobalNumberExpression(gNumIndex).EndMacro();
                        sb.PopColor();
                        entry.Format = sb.ToReadOnlySeString();
                        SaveAndReloadChat();
                    }
                }
            }

            ImGui.EndMenu();
        }

        if (ImGui.MenuItem(_textService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.StackColor")))
        {
            var sb = new SeStringBuilder();
            sb.Append(entry.Format);
            sb.PopColor();
            entry.Format = sb.ToReadOnlySeString();
            SaveAndReloadChat();
        }
    }

    private void DrawExample(ReadOnlySeString format)
    {
        using var textColor = new ImRaii.Color();

        var resolved = _seStringEvaluator.Evaluate(format, [
            _textService.Translate("CustomChatMessageFormats.Config.LStr1.Label"), // "Player Name"
            _textService.Translate("CustomChatMessageFormats.Config.LStr2.Label"), // "Message"
        ]);

        foreach (var payload in resolved)
        {
            if (payload.Type == ReadOnlySePayloadType.Text)
            {
                ImGui.Text(Encoding.UTF8.GetString(payload.Body.ToArray()));
                ImGui.SameLine(0, 0);
                continue;
            }

            if (payload.Type != ReadOnlySePayloadType.Macro)
                continue;

            switch (payload.MacroCode)
            {
                case MacroCode.Icon:
                    if (payload.TryGetExpression(out var iconExpr) && iconExpr.TryGetUInt(out var iconId))
                        _gfdService.Draw(iconId, 20);
                    break;

                case MacroCode.Color:
                    if (!payload.TryGetExpression(out var eColor))
                        break;

                    if (eColor.TryGetPlaceholderExpression(out var ph) && ph == (int)ExpressionType.StackColor)
                        textColor.Pop();
                    else if (eColor.TryGetUInt(out var eColorVal)) //if (TryResolveUInt(ref context, eColor, out var eColorVal))
                        textColor.Push(ImGuiCol.Text, SwapRedBlue(eColorVal | 0xFF000000u));

                    ImGui.Dummy(Vector2.Zero);
                    break;
            }

            ImGui.SameLine(0, 0);
        }

        ImGui.NewLine();
    }

    private List<(LogKind LogKind, LogFilter LogFilter, ReadOnlySeString Format)> GenerateLogKindCache()
    {
        var list = new List<(LogKind LogKind, LogFilter LogFilter, ReadOnlySeString Format)>();

        void Add(uint logKindId, uint logFilterId)
        {
            _excelService.TryGetRow<LogKind>(logKindId, out var logKindRow);
            _excelService.TryGetRow<LogFilter>(logFilterId, out var logFilterRow);
            if (logKindRow.Format.ByteLength > 0)
                list.Add((logKindRow, logFilterRow, logKindRow.Format));
            else
                _logger.LogWarning("GenerateLogKindCache(): Skipped ({logKindId}, {logFilterId})", logKindId, logFilterId);
        }

        Add(10, 1); // Say
        Add(11, 2); // Shout
        Add(12, 3); // Tell (Outgoing)
        Add(13, 3); // Tell (Incoming)
        Add(14, 4); // Party
        Add(15, 17); // Alliance
        Add(16, 8); // Linkshell [1]
        Add(17, 9); // Linkshell [2]
        Add(18, 10); // Linkshell [3]
        Add(19, 11); // Linkshell [4]
        Add(20, 12); // Linkshell [5]
        Add(21, 13); // Linkshell [6]
        Add(22, 14); // Linkshell [7]
        Add(23, 15); // Linkshell [8]
        Add(24, 7); // Free Company
        Add(27, 18); // Novice Network
        // Add(28, 6); // Custom Emotes
        // Add(29, 5); // Standard Emotes
        Add(30, 16); // Yell
        Add(36, 19); // PvP Team
        Add(37, 20); // Cross-world Linkshell [1]
        Add(101, 300); // Cross-world Linkshell [2]
        Add(102, 301); // Cross-world Linkshell [3]
        Add(103, 302); // Cross-world Linkshell [4]
        Add(104, 303); // Cross-world Linkshell [5]
        Add(105, 304); // Cross-world Linkshell [6]
        Add(106, 305); // Cross-world Linkshell [7]
        Add(107, 306); // Cross-world Linkshell [8]

        return list;
    }

    private string GetLogKindlabel(uint logKindId)
    {
        if (logKindId == 12) // Tell (Outgoing)
            return _textService.Translate("CustomChatMessageFormats.Config.Entry.Name.TellOutgoing");

        if (logKindId == 13) // Tell (Incoming)
            return _textService.Translate("CustomChatMessageFormats.Config.Entry.Name.TellIncoming");

        _cachedLogKindRows ??= GenerateLogKindCache();

        foreach (var row in _cachedLogKindRows)
        {
            if (row.LogKind.RowId == logKindId)
                return row.LogFilter.Name.ToString();
        }

        return $"LogKind #{logKindId}";
    }

    private record TextColorEntry(int GNumIndex, string Label);
    private TextColorEntry[] GenerateTextColor()
    {
        return [.. new TextColorEntry[]
        {
            new(13, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(653)),  // Log Text Color - Chat 1 - Say
            new(14, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(645)),  // Log Text Color - Chat 1 - Shout
            new(15, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(7886)), // Log Text Color - Chat 1 - Tell
            new(16, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(7887)), // Log Text Color - Chat 1 - Party
            new(17, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(7888)), // Log Text Color - Chat 1 - Alliance
            new(18, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7890)), // Log Text Color - Chat 2 - LS1
            new(19, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7891)), // Log Text Color - Chat 2 - LS2
            new(20, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7892)), // Log Text Color - Chat 2 - LS3
            new(21, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7893)), // Log Text Color - Chat 2 - LS4
            new(22, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7894)), // Log Text Color - Chat 2 - LS5
            new(23, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7895)), // Log Text Color - Chat 2 - LS6
            new(24, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7896)), // Log Text Color - Chat 2 - LS7
            new(25, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7897)), // Log Text Color - Chat 2 - LS8
            new(26, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7889)), // Log Text Color - Chat 2 - Free Company
            new(27, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7899)), // Log Text Color - Chat 2 - PvP Team
            new(29, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(7898)), // Log Text Color - Chat 2 - Novice Network
            new(30, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(1911)), // Log Text Color - Chat 1 - Personal Emotes
            new(31, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(1911)), // Log Text Color - Chat 1 - Emotes
            new(32, _textService.GetAddonText(1935) + " - " + _textService.GetAddonText(1931)), // Log Text Color - Chat 1 - Yell
            new(35, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(4397)), // Log Text Color - Chat 2 - CWLS1
            new(84, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8390)), // Log Text Color - Chat 2 - CWLS2
            new(85, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8391)), // Log Text Color - Chat 2 - CWLS3
            new(86, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8392)), // Log Text Color - Chat 2 - CWLS4
            new(87, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8393)), // Log Text Color - Chat 2 - CWLS5
            new(88, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8394)), // Log Text Color - Chat 2 - CWLS6
            new(89, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8395)), // Log Text Color - Chat 2 - CWLS7
            new(90, _textService.GetAddonText(1936) + " - " + _textService.GetAddonText(8396)) // Log Text Color - Chat 2 - CWLS8
        }.OrderBy(kv => kv.Label)];
    }

    private static uint SwapRedBlue(uint value)
        => 0xFF000000 | ((value & 0x000000FF) << 16) | (value & 0x0000FF00) | ((value & 0x00FF0000) >> 16);

    private unsafe bool TryGetGNumDefault(uint parameterIndex, out uint value)
    {
        value = 0u;

        var rtm = RaptureTextModule.Instance();
        if (rtm is null)
            return false;

        if (!ThreadSafety.IsMainThread)
            return false;

        ref var gp = ref rtm->TextModule.MacroDecoder.GlobalParameters;
        if (parameterIndex >= gp.MySize)
            return false;

        var p = rtm->TextModule.MacroDecoder.GlobalParameters[parameterIndex];
        if (p.Type != TextParameterType.Integer)
            return false;

        value = (uint)p.IntValue;
        return true;
    }
}
