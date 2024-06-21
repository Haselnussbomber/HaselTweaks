using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselCommon.Text;
using HaselCommon.Text.Enums;
using HaselCommon.Text.Expressions;
using HaselCommon.Text.Payloads;
using HaselCommon.Text.Payloads.Macro;
using HaselCommon.Textures;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;
using static HaselTweaks.Tweaks.CustomChatMessageFormatsConfiguration;

namespace HaselTweaks.Tweaks;

public sealed class CustomChatMessageFormatsConfiguration
{
    public Dictionary<uint, LogKindOverride> FormatOverrides = [];

    public record LogKindOverride
    {
        public bool Enabled = true;
        public SeString Format;

        [JsonIgnore]
        public bool EditMode;

        public LogKindOverride(SeString format)
        {
            Format = format;
        }

        public bool IsValid()
        {
            var colorCount = 0;

            foreach (var payload in Format.Payloads)
            {
                if (payload is ColorPayload colorPayload)
                {
                    if (colorPayload.Color?.ExpressionType == ExpressionType.StackColor)
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

public sealed unsafe class CustomChatMessageFormats(
    PluginConfig pluginConfig,
    TextService textService,
    ILogger<CustomChatMessageFormats> Logger,
    IGameInteropProvider GameInteropProvider,
    IDataManager DataManager,
    ExcelService ExcelService,
    IGameConfig GameConfig,
    TextureManager TextureManager)
    : Tweak<CustomChatMessageFormatsConfiguration>(pluginConfig, textService)
{
    private List<(LogKind LogKind, LogFilter LogFilter, SeString Format)>? CachedLogKindRows = null;
    private FrozenDictionary<uint, string>? CachedTextColors = null;
    private static readonly string[] GfdTextures = [
        "common/font/fonticon_xinput.tex",
        "common/font/fonticon_ps3.tex",
        "common/font/fonticon_ps4.tex",
        "common/font/fonticon_ps5.tex",
        "common/font/fonticon_lys.tex",
    ];
    private GfdFileView? gfdFileView = null;
    private GfdFileView GfdFileView => gfdFileView ??= new(DataManager.GetFile("common/font/gfdata.gfd")!.Data);

    private Hook<HaselRaptureLogModule.Delegates.FormatLogMessage>? FormatLogMessageHook;

    public override void OnInitialize()
    {
        FormatLogMessageHook = GameInteropProvider.HookFromAddress<HaselRaptureLogModule.Delegates.FormatLogMessage>(
            HaselRaptureLogModule.MemberFunctionPointers.FormatLogMessage,
            FormatLogMessageDetour);
    }

    public override void OnEnable()
    {
        ReloadChat();
        TextService.LanguageChanged += OnLanguageChange;
        FormatLogMessageHook?.Enable();
    }

    public override void OnDisable()
    {
        ReloadChat();
        TextService.LanguageChanged -= OnLanguageChange;
        FormatLogMessageHook?.Disable();
    }

    public override void OnConfigClose()
    {
        CachedLogKindRows = null;
        CachedTextColors = null;
        gfdFileView = null;
    }

    private void OnLanguageChange(string langCode)
    {
        // TODO: if config window is open?
        CachedLogKindRows = GenerateLogKindCache();
        CachedTextColors = GenerateTextColors();
    }

    private unsafe uint FormatLogMessageDetour(HaselRaptureLogModule* haselRaptureLogModule, uint logKindId, Utf8String* sender, Utf8String* message, int* timestamp, nint a6, Utf8String* a7, int chatTabIndex)
    {
        if (haselRaptureLogModule->LogKindSheet == 0 || haselRaptureLogModule->AtkFontCodeModule == null)
            return 0;

        if (!Config.FormatOverrides.TryGetValue(logKindId, out var logKindOverride) || !logKindOverride.Enabled || !logKindOverride.IsValid())
            return FormatLogMessageHook!.Original(haselRaptureLogModule, logKindId, sender, message, timestamp, a6, a7, chatTabIndex);

        var tempParseMessage1 = haselRaptureLogModule->TempParseMessage.GetPointer(1);
        tempParseMessage1->Clear();

        if (!haselRaptureLogModule->RaptureTextModule->TextModule.FormatString(message->StringPtr, null, tempParseMessage1))
            return 0;

        var senderStr = new SeString([new RawPayload(sender->AsSpan().ToArray())]);
        var messageStr = new SeString([new RawPayload(tempParseMessage1->AsSpan().ToArray())]);

        var tempParseMessage0 = haselRaptureLogModule->TempParseMessage.GetPointer(0);
        tempParseMessage0->Clear();
        tempParseMessage0->SetString(logKindOverride.Format.Resolve([senderStr, messageStr]).Encode());

        var raptureLogModule = (RaptureLogModule*)haselRaptureLogModule;
        if (raptureLogModule->ChatTabShouldDisplayTime[chatTabIndex] && timestamp != null)
        {
            if (raptureLogModule->UseServerTime)
                *timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var tempParseMessage3 = haselRaptureLogModule->TempParseMessage.GetPointer(3);
            tempParseMessage3->SetString(haselRaptureLogModule->RaptureTextModule->FormatAddonText2Int(raptureLogModule->Use12HourClock ? 7841u : 7840u, *timestamp));
            using var buffer = new Utf8String();
            tempParseMessage0->Copy(Utf8String.Concat(tempParseMessage3, &buffer, tempParseMessage0));
        }

        return haselRaptureLogModule->AtkFontCodeModule->CalculateLogLines(a7, tempParseMessage0, a6, false);
    }

    private static unsafe void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    private void SaveAndReloadChat()
    {
        PluginConfig.Save();
        ReloadChat();
    }

    private void DrawEditMode(LogKindOverride entry)
    {
        using var payloadTable = ImRaii.Table("##PayloadTable", 3, ImGuiTableFlags.Borders);
        if (!payloadTable)
            return;

        var ItemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;
        var ArrowUpButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowUp);
        var ArrowDownButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ArrowDown);
        var TrashButtonSize = ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Trash);

        ImGui.TableSetupColumn(TextService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Payload.Label"), ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn(TextService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Value.Label"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(TextService.Translate("CustomChatMessageFormats.Config.Entry.PayloadTableHeader.Actions.Label"), ImGuiTableColumnFlags.WidthFixed,
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

        foreach (var payload in entry.Format.Payloads)
        {
            using var id = ImRaii.PushId("PayloadEdit[" + i.ToString() + "]");

            ImGui.TableNextRow();

            // Payload
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                switch (payload)
                {
                    case TextPayload textPayload:
                        ImGui.TextUnformatted("Text");
                        break;

                    case MacroPayload macroPayload:
                        ImGui.TextUnformatted(macroPayload.Code.ToString());
                        break;

                    default:
                        ImGui.TextUnformatted(payload.GetType().Name);
                        break;
                }
            }

            // Value
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                switch (payload)
                {
                    case TextPayload textPayload:
                        var text = textPayload.Text ?? string.Empty;
                        ImGui.SetNextItemWidth(-1);
                        ImGuiUtils.PushCursorY(-ImGui.GetStyle().CellPadding.Y);
                        if (ImGui.InputText($"##TextPayload{i}", ref text, 255))
                        {
                            textPayload.Text = text;
                            SaveAndReloadChat();
                        }
                        break;

                    case IconPayload iconPayload:
                        var iconId = (uint)(iconPayload.IconId?.ResolveNumber() ?? 0);

                        if (GfdFileView.TryGetEntry(iconId, out var gfdEntry))
                            DrawGfdEntry(gfdEntry);

                        ImGui.SameLine();

                        if (ImGui.Button(TextService.Translate("CustomChatMessageFormats.Config.Entry.OpenIconSelectorButton.Label")))
                        {
                            ImGui.OpenPopup("##IconSelector");
                        }

                        using (var iconSelectMenu = ImRaii.Popup("##IconSelector", ImGuiWindowFlags.NoMove))
                        {
                            if (iconSelectMenu)
                            {
                                var maxLineWidth = 20 * 20;
                                var posStart = ImGui.GetCursorPosX();

                                foreach (var selectorGfdEntry in GfdFileView.Entries)
                                {
                                    if (selectorGfdEntry.IsEmpty || selectorGfdEntry.Id is 69 or 123)
                                        continue;

                                    var startPos = ImGui.GetCursorPos();
                                    using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, 0);
                                    using var buttonActiveColor = ImRaii.PushColor(ImGuiCol.ButtonActive, 0xAAFFFFFF);
                                    using var buttonHoveredColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0x77FFFFFF);
                                    using var buttonRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
                                    if (ImGui.Button($"##Icon{selectorGfdEntry.Id}", ImGuiHelpers.ScaledVector2(selectorGfdEntry.Width, selectorGfdEntry.Height)))
                                    {
                                        iconPayload.IconId = new IntegerExpression(selectorGfdEntry.Id);
                                        SaveAndReloadChat();
                                    }

                                    ImGui.SetCursorPos(startPos);
                                    DrawGfdEntry(selectorGfdEntry);

                                    ImGui.SameLine();

                                    if (ImGui.GetCursorPosX() - posStart > maxLineWidth)
                                        ImGui.NewLine();
                                }
                            }
                        }

                        break;

                    case StringPayload stringPayload:
                        ImGui.TextUnformatted($"{stringPayload.Parameter}");
                        break;

                    case ColorPayload colorPayload:
                        uint value;
                        switch (colorPayload.Color)
                        {
                            case ParameterExpression parameterExpression
                            when parameterExpression.ExpressionType is ExpressionType.PlayerParameter
                              && parameterExpression.Operand is IntegerExpression operandExpression:
                                value = (uint)parameterExpression.ResolveNumber();

                                using (ImRaii.PushColor(ImGuiCol.Text, SwapRedBlue(value)))
                                {
                                    if (CachedTextColors!.TryGetValue(operandExpression.Value, out var label))
                                        ImGui.TextUnformatted(label);
                                    else
                                        ImGui.TextUnformatted(operandExpression.ToString());
                                }
                                break;

                            case IntegerExpression integerExpression:
                                value = (uint)integerExpression.ResolveNumber();

                                ImGui.SetNextItemWidth(-1);
                                ImGuiUtils.PushCursorY(-ImGui.GetStyle().CellPadding.Y);
                                var hexColor = ImGui.ColorConvertU32ToFloat4(SwapRedBlue(value));
                                if (ImGui.ColorEdit4("##ColorPicker", ref hexColor, ImGuiColorEditFlags.NoAlpha))
                                {
                                    colorPayload.Color = new IntegerExpression(SwapRedBlue(ImGui.ColorConvertFloat4ToU32(hexColor)));
                                    SaveAndReloadChat();
                                }
                                break;

                            default:
                                ImGui.TextUnformatted($"{colorPayload.Color}");
                                break;
                        }
                        break;

                    default:
                        ImGui.TextUnformatted($"{payload}");
                        break;
                }

                if (ImGui.IsItemHovered())
                {
                    var isStringPlaceholder = payload is StringPayload stringPayload && stringPayload.Parameter is ParameterExpression parameterExpression;
                    if (isStringPlaceholder)
                    {
                        ImGui.BeginTooltip();
                        TextService.Draw("CustomChatMessageFormats.Config.Entry.Payload.StringPlaceholder.Tooltip");
                        ImGui.EndTooltip();
                    }

                    var isStackColor = payload is ColorPayload stackColorPayload && stackColorPayload.Color?.ExpressionType is ExpressionType.StackColor;
                    if (isStackColor)
                    {
                        ImGui.BeginTooltip();
                        TextService.Draw("CustomChatMessageFormats.Config.Entry.Payload.StackColor.Tooltip");
                        ImGui.EndTooltip();
                    }
                }
            }

            // Actions
            ImGui.TableNextColumn();
            {
                if (i > 0)
                {
                    if (ImGuiUtils.IconButton("##Up", FontAwesomeIcon.ArrowUp, TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.MoveUpButton.Tooltip")))
                    {
                        entryToMoveUp = i;
                    }
                }
                else
                {
                    ImGui.Dummy(ArrowUpButtonSize);
                }

                ImGui.SameLine(0, ItemInnerSpacing.X);

                if (i < entry.Format.Payloads.Count - 1)
                {
                    if (ImGuiUtils.IconButton("##Down", FontAwesomeIcon.ArrowDown, TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.MoveDownButton.Tooltip")))
                    {
                        entryToMoveDown = i;
                    }
                }
                else
                {
                    ImGui.Dummy(ArrowDownButtonSize);
                }

                if (payload is not StringPayload)
                {
                    ImGui.SameLine(0, ItemInnerSpacing.X);

                    if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                    {
                        if (ImGuiUtils.IconButton("##Delete", FontAwesomeIcon.Trash, TextService.Translate("HaselTweaks.Config.Generic.DeleteButton.Tooltip.HoldingShift")))
                        {
                            entryToRemove = i;
                        }
                    }
                    else
                    {
                        ImGuiUtils.IconButton(
                            "##Delete",
                            FontAwesomeIcon.Trash,
                            TextService.Translate(isWindowFocused
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
            var removedItem = entry.Format.Payloads[entryToMoveUp];
            entry.Format.Payloads.RemoveAt(entryToMoveUp);
            entry.Format.Payloads.Insert(entryToMoveUp - 1, removedItem);
            SaveAndReloadChat();
        }

        if (entryToMoveDown != -1)
        {
            var removedItem = entry.Format.Payloads[entryToMoveDown];
            entry.Format.Payloads.RemoveAt(entryToMoveDown);
            entry.Format.Payloads.Insert(entryToMoveDown + 1, removedItem);
            SaveAndReloadChat();
        }

        if (entryToRemove != -1)
        {
            entry.Format.Payloads.RemoveAt(entryToRemove);
            SaveAndReloadChat();
        }

        ImGui.Button(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Label"));
        using (var contextMenu = ImRaii.ContextPopupItem("##AddPayloadContextMenu", ImGuiPopupFlags.MouseButtonLeft))
        {
            if (contextMenu)
            {
                if (ImGui.MenuItem(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.TextPayload")))
                {
                    entry.Format.Payloads.Add(new TextPayload(""));
                    SaveAndReloadChat();
                }

                if (ImGui.MenuItem(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.IconPayload")))
                {
                    entry.Format.Payloads.Add(new IconPayload() { IconId = 1 });
                    SaveAndReloadChat();
                }

                if (ImGui.MenuItem(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.CustomColor")))
                {
                    entry.Format.Payloads.AddRange([
                        new ColorPayload() { Color = new IntegerExpression(0xFFFFFFFF) },
                        new ColorPayload() { Color = new PlaceholderExpression(ExpressionType.StackColor) }
                    ]);
                    SaveAndReloadChat();
                }

                if (ImGui.BeginMenu(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.LogTextColors"))) // GetAddonText(12732)
                {
                    foreach (var (gnumIndex, label) in CachedTextColors!.ToList().OrderBy(entry => entry.Value))
                    {
                        if (gnumIndex == 30) // skip Personal Emotes which is the same as Emotes
                            continue;

                        var parameterExpression = new ParameterExpression(ExpressionType.PlayerParameter, gnumIndex);
                        var value = (uint)parameterExpression.ResolveNumber();

                        using (ImRaii.PushColor(ImGuiCol.Text, SwapRedBlue(value)))
                        {
                            if (ImGui.MenuItem(label + "##TextColor" + gnumIndex.ToString()))
                            {
                                entry.Format.Payloads.AddRange([
                                    new ColorPayload() { Color = parameterExpression },
                                    new ColorPayload() { Color = new PlaceholderExpression(ExpressionType.StackColor) }
                                ]);
                                SaveAndReloadChat();
                            }
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem(TextService.Translate("CustomChatMessageFormats.Config.Entry.Payload.AddPayloadButton.Option.StackColor")))
                {
                    entry.Format.Payloads.Add(new ColorPayload() { Color = new PlaceholderExpression(ExpressionType.StackColor) });
                    SaveAndReloadChat();
                }
            }
        }
    }

    private void DrawExample(SeString format)
    {
        using var textColors = new ImRaii.Color();

        // TODO: compose player name with party index, job icon (depending on setting) and world
        var resolved = format.Resolve(["Player Name", "Message"]);

        for (var i = 0; i < resolved.Payloads.Count; i++)
        {
            var payload = resolved.Payloads[i];

            switch (payload)
            {
                case TextPayload textPayload:
                    ImGui.TextUnformatted(textPayload.Text);
                    break;

                case IconPayload iconPayload:
                    var iconId = (uint)(iconPayload.IconId?.ResolveNumber() ?? 0);

                    if (GfdFileView.TryGetEntry(iconId, out var gfdEntry))
                        DrawGfdEntry(gfdEntry);
                    else
                        ImGui.Dummy(new(20));

                    break;

                case ColorPayload colorPayload:
                    if (colorPayload.Color is IntegerExpression integerExpression)
                        textColors.Push(ImGuiCol.Text, SwapRedBlue((uint)integerExpression.ResolveNumber()));
                    else if (colorPayload.Color?.ExpressionType == ExpressionType.StackColor)
                        textColors.Pop(1);

                    ImGui.Dummy(Vector2.Zero);
                    break;

                default:
                    ImGui.TextUnformatted(payload.ToString());
                    break;
            }

            ImGui.SameLine(0, 0);
        }

        ImGui.NewLine();
    }

    private void DrawGfdEntry(GfdFileView.GfdEntry entry)
    {
        var startPos = new Vector2(entry.Left, entry.Top) * 2 + new Vector2(0, 340);
        var size = new Vector2(entry.Width, entry.Height) * 2;

        GameConfig.TryGet(SystemConfigOption.PadSelectButtonIcon, out uint padSelectButtonIcon);

        TextureManager
            .Get(GfdTextures[padSelectButtonIcon], 1, startPos, startPos + size)
            .Draw(ImGuiHelpers.ScaledVector2(size.X, size.Y) / 2);
    }

    private List<(LogKind LogKind, LogFilter LogFilter, SeString Format)> GenerateLogKindCache()
    {
        var list = new List<(LogKind LogKind, LogFilter LogFilter, SeString Format)>();

        void Add(uint logKindId, uint logFilterId)
        {
            var logKindRow = ExcelService.GetRow<LogKind>(logKindId)!;
            var logFilterRow = ExcelService.GetRow<LogFilter>(logFilterId)!;
            if (logKindRow != null && logFilterRow != null && logKindRow.Format.RawData.Length > 0)
                list.Add((logKindRow, logFilterRow, SeString.Parse(logKindRow.Format.RawData)));
            else
                Logger.LogWarning("GenerateLogKindCache(): Skipped ({logKindId}, {logFilterId})", logKindId, logFilterId);
        }

        Add(10, 1); // Say
        Add(11, 2); // Shout
        Add(12, 3); // Tell (Incoming)
        Add(13, 3); // Tell (Outgoing)
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
        if (logKindId == 12) // Tell (Incoming)
            return TextService.Translate("CustomChatMessageFormats.Config.Entry.Name.TellIncoming");

        if (logKindId == 13) // Tell (Outgoing)
            return TextService.Translate("CustomChatMessageFormats.Config.Entry.Name.TellOutgoing");

        CachedLogKindRows ??= GenerateLogKindCache();

        foreach (var row in CachedLogKindRows)
        {
            if (row.LogKind.RowId == logKindId)
                return row.LogFilter.Name;
        }

        return $"LogKind #{logKindId}";
    }

    private FrozenDictionary<uint, string> GenerateTextColors()
    {
        return new Dictionary<uint, string>
        {
            { 13, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(653) },  // Log Text Colors - Chat 1 - Say
            { 14, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(645) },  // Log Text Colors - Chat 1 - Shout
            { 15, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(7886) }, // Log Text Colors - Chat 1 - Tell
            { 16, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(7887) }, // Log Text Colors - Chat 1 - Party
            { 17, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(7888) }, // Log Text Colors - Chat 1 - Alliance
            { 18, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7890) }, // Log Text Colors - Chat 2 - LS1
            { 19, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7891) }, // Log Text Colors - Chat 2 - LS2
            { 20, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7892) }, // Log Text Colors - Chat 2 - LS3
            { 21, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7893) }, // Log Text Colors - Chat 2 - LS4
            { 22, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7894) }, // Log Text Colors - Chat 2 - LS5
            { 23, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7895) }, // Log Text Colors - Chat 2 - LS6
            { 24, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7896) }, // Log Text Colors - Chat 2 - LS7
            { 25, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7897) }, // Log Text Colors - Chat 2 - LS8
            { 26, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7889) }, // Log Text Colors - Chat 2 - Free Company
            { 27, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7899) }, // Log Text Colors - Chat 2 - PvP Team
            { 29, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(7898) }, // Log Text Colors - Chat 2 - Novice Network
            { 30, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(1911) }, // Log Text Colors - Chat 1 - Personal Emotes
            { 31, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(1911) }, // Log Text Colors - Chat 1 - Emotes
            { 32, TextService.GetAddonText(1935) + " - " + TextService.GetAddonText(1931) }, // Log Text Colors - Chat 1 - Yell
            { 35, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(4397) }, // Log Text Colors - Chat 2 - CWLS1
            { 84, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8390) }, // Log Text Colors - Chat 2 - CWLS2
            { 85, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8391) }, // Log Text Colors - Chat 2 - CWLS3
            { 86, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8392) }, // Log Text Colors - Chat 2 - CWLS4
            { 87, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8393) }, // Log Text Colors - Chat 2 - CWLS5
            { 88, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8394) }, // Log Text Colors - Chat 2 - CWLS6
            { 89, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8395) }, // Log Text Colors - Chat 2 - CWLS7
            { 90, TextService.GetAddonText(1936) + " - " + TextService.GetAddonText(8396) } // Log Text Colors - Chat 2 - CWLS8
        }.ToFrozenDictionary();
    }

    private static uint SwapRedBlue(uint value)
        => 0xFF000000 | ((value & 0x000000FF) << 16) | (value & 0x0000FF00) | ((value & 0x00FF0000) >> 16);

    public override void DrawConfig()
    {
        TextService.Draw("HaselTweaks.Config.SectionTitle.Configuration");

        CachedLogKindRows ??= GenerateLogKindCache();
        CachedTextColors ??= GenerateTextColors();

        var ItemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;

        using var cellpadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ItemInnerSpacing * ImGuiHelpers.GlobalScale);
        using var table = ImRaii.Table("##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX);
        if (!table.Success)
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
                    TextService.Draw(entry.Enabled
                        ? "CustomChatMessageFormats.Config.Entry.EnableCheckbox.Tooltip.IsEnabled"
                        : "CustomChatMessageFormats.Config.Entry.EnableCheckbox.Tooltip.IsDisabled");
                    ImGui.EndTooltip();
                }
                if (ImGui.IsItemClicked())
                    PluginConfig.Save();
            }

            // Channel Name and Format
            ImGui.TableNextColumn();
            {
                ImGuiUtils.PushCursorY(-2 * ImGuiHelpers.GlobalScale);

                if (!isValid)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                        ImGuiUtils.Icon(FontAwesomeIcon.ExclamationCircle);

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        TextService.Draw("CustomChatMessageFormats.Config.Entry.InvalidPayloads.Tooltip");
                        ImGui.EndTooltip();
                    }

                    ImGuiUtils.SameLineSpace();
                }

                ImGui.TextUnformatted(GetLogKindlabel(logKindId));

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
                    if (ImGuiUtils.IconButton("##CloseEditor", FontAwesomeIcon.FilePen, TextService.Translate("CustomChatMessageFormats.Config.Entry.CloseEditorButton.Tooltip")))
                    {
                        entry.EditMode = false;
                    }
                }
                else
                {
                    if (ImGuiUtils.IconButton("##OpenEditor", FontAwesomeIcon.Pen, TextService.Translate("CustomChatMessageFormats.Config.Entry.OpenEditorButton.Tooltip")))
                    {
                        entryToEdit = logKindId;
                    }
                }

                ImGui.SameLine(0, ItemInnerSpacing.X);

                if (isWindowFocused && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton("##Delete", FontAwesomeIcon.Trash, TextService.Translate("HaselTweaks.Config.Generic.DeleteButton.Tooltip.HoldingShift")))
                    {
                        entryToRemove = logKindId;
                    }
                }
                else
                {
                    ImGuiUtils.IconButton(
                        "##Delete",
                        FontAwesomeIcon.Trash,
                        TextService.Translate(isWindowFocused
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

        var entries = CachedLogKindRows.Where(entry => !Config.FormatOverrides.ContainsKey(entry.LogKind.RowId));
        var entriesCount = entries.Count();
        if (entriesCount > 0)
        {
            ImGui.SetNextItemWidth(-1);
            using var combo = ImRaii.Combo("##LogKindSelect", TextService.Translate("CustomChatMessageFormats.LogKindSelect.PreviewValue"), ImGuiComboFlags.HeightLarge);
            if (combo)
            {
                var i = 0;
                foreach (var entry in entries)
                {
                    if (ImGui.Selectable($"{GetLogKindlabel(entry.LogKind.RowId)}##LogKind{entry.LogKind.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2)))
                    {
                        Config.FormatOverrides.Add(entry.LogKind.RowId, new(SeString.Parse(entry.Format.Encode())));
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

        if (ImGui.Button(TextService.Translate("CustomChatMessageFormats.Config.OpenConfigLogColorButton.Tooltip")))
        {
            unsafe
            {
                GetAgent<AgentInterface>(AgentId.ConfigLogColor)->Show();
            }
        }
    }
}
