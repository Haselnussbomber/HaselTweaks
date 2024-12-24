using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui;
using HaselCommon.Gui.Yoga;
using HaselCommon.Gui.Yoga.Components;
using HaselCommon.Gui.Yoga.Components.Events;
using HaselCommon.Gui.Yoga.Events;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;
using YogaSharp;

namespace HaselTweaks.Windows;

public unsafe class AetherCurrentHelperWindow : LockableWindow
{
    private static readonly Color TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);
    private static readonly string[] CompassHeadings = ["E", "NE", "N", "NW", "W", "SW", "S", "SE"];
    private AetherCurrentHelperConfiguration Config => PluginConfig.Tweaks.AetherCurrentHelper;

    private readonly IClientState ClientState;
    private readonly TextureService TextureService;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly MapService MapService;
    private readonly AddonObserver AddonObserver;
    private readonly UnlocksObserver UnlocksObserver;

    private readonly Dictionary<uint, EObj> AetherCurrentEObjCache = [];
    private readonly Dictionary<uint, Level> EObjLevelCache = [];

    private readonly Checkbox _hideUnlockedCheckbox;
    private readonly TextNode _placeNameTextNode;
    private readonly IconNode _aetherCurrentIcon;
    private readonly Node _headerNode;
    private readonly Node _listNode;
    private readonly Node _allAttunedWrapper;
    private readonly TextNode _allAttunedTextNode;
    private AetherCurrentCompFlgSet? _compFlgSet;

    public AetherCurrentHelperWindow(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        IClientState clientState,
        TextureService textureService,
        ExcelService excelService,
        TextService textService,
        MapService mapService,
        AddonObserver addonObserver,
        UnlocksObserver unlocksObserver,
        ILogger<AetherCurrentHelperWindow> logger)
        : base(windowManager, pluginConfig, textService, "[HaselTweaks] Aether Current Helper")
    {
        ClientState = clientState;
        TextureService = textureService;
        ExcelService = excelService;
        TextService = textService;
        MapService = mapService;
        AddonObserver = addonObserver;
        UnlocksObserver = unlocksObserver;

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(350);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(4096),
        };

        // TODO: move everything to a Setup() function that's only called once

        var style = ImGui.GetStyle();

        // -- Nodes --

        _hideUnlockedCheckbox = new Checkbox();
        _placeNameTextNode = new TextNode();
        _aetherCurrentIcon = new IconNode()
        {
            Icon = IsAddonOpen(AgentId.AetherCurrent) ? 0 : 64,
            Width = ImGui.GetFrameHeight(),
            Height = ImGui.GetFrameHeight()
        };

        _headerNode = new Node
        {
            FlexDirection = YGFlexDirection.Row,
            AlignItems = YGAlign.Center,
            JustifyContent = YGJustify.SpaceBetween,
            ColumnGap = style.ItemInnerSpacing.X,
            Children = [
                _hideUnlockedCheckbox,
                _placeNameTextNode,
                _aetherCurrentIcon
            ]
        };

        _listNode = new Node()
        {
            Display = YGDisplay.None,
            RowGap = style.ItemSpacing.Y,
        };

        _allAttunedWrapper = new Node()
        {
            Display = YGDisplay.None,
            FlexGrow = 1,
            JustifyContent = YGJustify.Center,
            AlignItems = YGAlign.Center,
        };

        _allAttunedTextNode = new TextNode();

        _allAttunedWrapper.Add(_allAttunedTextNode);

        // -- RootNode --

        RootNode.ColumnGap = style.ItemSpacing.X;
        RootNode.RowGap = style.ItemSpacing.Y;
        RootNode.Add(_headerNode);
        RootNode.Add(_listNode);
        RootNode.Add(_allAttunedWrapper);

        // -- Events --

        _hideUnlockedCheckbox.AddEventListener<MouseEvent>((node, evt) =>
        {
            if (evt.EventType == MouseEventType.MouseHover)
            {
                using var tooltip = ImRaii.Tooltip();
                TextService.Draw("AetherCurrentHelperWindow.HideUnlockedTooltip");
            }
        });

        _hideUnlockedCheckbox.AddEventListener<CheckboxStateChangeEvent>((node, evt) =>
        {
            UpdateList();
        });

        _aetherCurrentIcon.AddEventListener<MouseEvent>((node, evt) =>
        {
            switch (evt.EventType)
            {
                case MouseEventType.MouseHover:
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    break;

                case MouseEventType.MouseClick when evt.Button == ImGuiMouseButton.Left:
                    AgentAetherCurrent.Instance()->Show();
                    break;
            }
        });

        UpdateTexts();

        TextService.LanguageChanged += OnLanguageChanged;
        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;
        UnlocksObserver.Update += OnUnlocksUpdate;
    }

    public override void Dispose()
    {
        TextService.LanguageChanged -= OnLanguageChanged;
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;
        base.Dispose();
    }

    public AetherCurrentCompFlgSet? CompFlgSet
    {
        get => _compFlgSet;
        set
        {
            _compFlgSet = value;
            UpdateList();
            UpdateTexts();
        }
    }

    private void OnLanguageChanged(string langCode)
    {
        UpdateTexts();
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName == "AetherCurrent")
        {
            _aetherCurrentIcon.Icon = 0; // nice hack, huh?
        }
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "AetherCurrent")
        {
            _aetherCurrentIcon.Icon = 64;
        }
    }

    // TODO: check if this is triggered on loggin/relogging
    private void OnUnlocksUpdate()
    {
        UpdateList();
    }

    private void UpdateList()
    {
        _listNode.Clear();

        if (CompFlgSet == null)
            return;

        var index = 1;
        var type = 0;
        var linesDisplayed = 0;
        var playerState = PlayerState.Instance();
        foreach (var aetherCurrent in CompFlgSet.Value.AetherCurrents)
        {
            if (aetherCurrent.RowId == 0)
                continue;

            var isQuest = aetherCurrent.Value.Quest.RowId > 0;
            if (!isQuest && type == 0)
            {
                type = 1;
                index = 1;
            }

            var isUnlocked = playerState->IsAetherCurrentUnlocked(aetherCurrent.RowId);
            if (!_hideUnlockedCheckbox.IsChecked || !isUnlocked)
            {
                if (type == 0)
                {
                    AddQuestNode(index, isUnlocked, aetherCurrent.Value);
                }
                else if (type == 1)
                {
                    AddEObjNode(index, isUnlocked, aetherCurrent.Value);
                }

                linesDisplayed++;
            }

            index++;
        }

        _listNode.Display = linesDisplayed == 0 ? YGDisplay.None : YGDisplay.Flex;
        _allAttunedWrapper.Display = linesDisplayed == 0 ? YGDisplay.Flex : YGDisplay.None;

        /*
        if (linesDisplayed == 0)
        {
            // hacky, but it looks the best
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            startPos = ImGui.GetCursorPos() + new Vector2(-startPos.X - 4, 0);
            var text = TextService.Translate("AetherCurrentHelperWindow.AllAetherCurrentsAttuned");
            textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
            ImGui.TextUnformatted(text);
            ImGui.TableNextColumn();
        }
        */
    }

    private void AddQuestNode(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var questId = aetherCurrent.Quest.RowId;

        // Some AetherCurrents link to the wrong Quest.
        // See https://github.com/Haselnussbomber/HaselTweaks/issues/15

        // The Dravanian Forelands (CompFlgSet#2)
        if (aetherCurrent.RowId == 2818065 && questId == 67328) // Natural Repellent
            questId = 67326; // Stolen Munitions
        else if (aetherCurrent.RowId == 2818066 && questId == 67334) // Chocobo's Last Stand
            questId = 67333; // The Hunter Becomes the Kweh

        // The Churning Mists (CompFlgSet#4)
        else if (aetherCurrent.RowId == 2818096 && questId == 67365) // The Unceasing Gardener
            questId = 67364; // Hide Your Moogles

        // The Sea of Clouds (CompFlgSet#5)
        else if (aetherCurrent.RowId == 2818110 && questId == 67437) // Search and Rescue
            questId = 67410; // Honoring the Past

        // Thavnair (CompFlgSet#21)
        else if (aetherCurrent.RowId == 2818328 && questId == 70030) // Curing What Ails
            questId = 69793; // In Agama's Footsteps

        if (!ExcelService.TryGetRow<Quest>(questId, out var quest) || quest.IssuerLocation.RowId == 0)
            return;

        if (!quest.IssuerLocation.IsValid)
            return;

        _listNode.Add(new QuestNode(TextService, index, isUnlocked, quest));
    }

    private void AddEObjNode(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        if (!AetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobj))
        {
            if (!ExcelService.TryFindRow(row => row.Data == aetherCurrent.RowId, out eobj))
                return;

            AetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj);
        }

        if (!EObjLevelCache.TryGetValue(eobj.RowId, out var level))
        {
            if (!ExcelService.TryFindRow(row => row.Object.RowId == eobj.RowId, out level))
                return;

            EObjLevelCache.Add(eobj.RowId, level);
        }

        _listNode.Add(new EObjNode(TextService, index, isUnlocked, eobj, level));
    }

    private void UpdateTexts()
    {
        _placeNameTextNode.Text = CompFlgSet != null && ExcelService.TryGetRow<PlaceName>(CompFlgSet.Value.Territory.Value.PlaceName.RowId, out var placeName)
            ? placeName.Name.ExtractText()
            : ""u8;

        _allAttunedTextNode.Text = TextService.Translate("AetherCurrentHelperWindow.AllAetherCurrentsAttuned");

        RootNode.Traverse(node =>
        {
            if (node is AetherCurrentEntryNode entryNode)
            {
                entryNode.UpdateText();
            }
        });
    }

    public override bool DrawConditions()
        => CompFlgSet.HasValue && ClientState.IsLoggedIn && !RaptureAtkModule.Instance()->RaptureAtkUnitManager.UiFlags.HasFlag(UIModule.UiFlags.ActionBars);

    public unsafe void Draw2()
    {
        // TODO: ?????
        if (!Flags.HasFlag(ImGuiWindowFlags.NoFocusOnAppearing))
            Flags |= ImGuiWindowFlags.NoFocusOnAppearing;
    }

    private void DrawQuest(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var questId = aetherCurrent.Quest.RowId;

        // Some AetherCurrents link to the wrong Quest.
        // See https://github.com/Haselnussbomber/HaselTweaks/issues/15

        // The Dravanian Forelands (CompFlgSet#2)
        if (aetherCurrent.RowId == 2818065 && questId == 67328) // Natural Repellent
            questId = 67326; // Stolen Munitions
        else if (aetherCurrent.RowId == 2818066 && questId == 67334) // Chocobo's Last Stand
            questId = 67333; // The Hunter Becomes the Kweh

        // The Churning Mists (CompFlgSet#4)
        else if (aetherCurrent.RowId == 2818096 && questId == 67365) // The Unceasing Gardener
            questId = 67364; // Hide Your Moogles

        // The Sea of Clouds (CompFlgSet#5)
        else if (aetherCurrent.RowId == 2818110 && questId == 67437) // Search and Rescue
            questId = 67410; // Honoring the Past

        // Thavnair (CompFlgSet#21)
        else if (aetherCurrent.RowId == 2818328 && questId == 70030) // Curing What Ails
            questId = 69793; // In Agama's Footsteps

        if (!ExcelService.TryGetRow<Quest>(questId, out var quest) || quest.IssuerLocation.RowId == 0)
            return;

        if (!quest.IssuerLocation.IsValid)
            return;

        // Icon
        ImGui.TableNextColumn();
        TextureService.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, 40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {TextService.GetQuestName(quest.RowId)}");
        ImGui.TextUnformatted($"{GetHumanReadableCoords(quest.IssuerLocation.Value)} | {TextService.GetENpcResidentName(quest.IssuerStart.RowId)}");

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            MapService.OpenMap(quest.IssuerLocation.Value);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, quest.IssuerLocation.Value);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        if (!AetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobj))
        {
            if (!ExcelService.TryFindRow(row => row.Data == aetherCurrent.RowId, out eobj))
                return;

            AetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj);
        }

        if (!EObjLevelCache.TryGetValue(eobj.RowId, out var level))
        {
            if (!ExcelService.TryFindRow(row => row.Object.RowId == eobj.RowId, out level))
                return;

            EObjLevelCache.Add(eobj.RowId, level);
        }

        // Icon
        ImGui.TableNextColumn();
        TextureService.DrawIcon(60033, 40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {TextService.GetEObjName(eobj.RowId)}");
        ImGui.TextUnformatted(GetHumanReadableCoords(level));

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##AetherCurrent_{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            MapService.OpenMap(level);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, level);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawUnlockStatus(bool isUnlocked, Level level)
    {
        var isSameTerritory = level.Territory.RowId == ClientState.TerritoryType;
        ImGuiUtils.PushCursorY(11);

        if (isUnlocked && !Config.AlwaysShowDistance)
        {
            DrawCheckmark(isSameTerritory);
        }
        else
        {
            if (isSameTerritory)
            {
                var distance = MapService.GetDistanceFromPlayer(level);
                if (distance < float.MaxValue)
                {
                    var direction = distance > 1 ? GetCompassDirection(level) : string.Empty;
                    var text = $"{distance:0}y {direction}";

                    if (Config.CenterDistance)
                    {
                        ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
                    }

                    if (isUnlocked)
                    {
                        ImGuiUtils.TextUnformattedColored(Color.Green, text);
                    }
                    else
                    {
                        ImGui.TextUnformatted(text);
                    }
                }
            }
            else if (isUnlocked)
            {
                DrawCheckmark(isSameTerritory);
            }
            else
            {
                ImGuiUtils.PushCursorX(2);
                using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
                ImGuiUtils.TextUnformattedColored(Color.Grey4, FontAwesomeIcon.Times.ToIconString());
            }
        }
    }

    private void DrawCheckmark(bool isSameTerritory)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        var icon = FontAwesomeIcon.Check.ToIconString();

        if (isSameTerritory && Config.CenterDistance)
        {
            ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(icon).X / 2);
        }

        ImGuiUtils.TextUnformattedColored(Color.Green, icon);
    }

    private string GetHumanReadableCoords(Level level)
    {
        var coords = MapService.GetCoords(level);
        var x = coords.X.ToString("0.0", CultureInfo.InvariantCulture);
        var y = coords.Y.ToString("0.0", CultureInfo.InvariantCulture);
        return TextService.Translate("AetherCurrentHelperWindow.Coords", x, y);
    }

    //! https://gamedev.stackexchange.com/a/49300
    public string GetCompassDirection(Vector2 a, Vector2 b)
    {
        var vector = a - b;
        var angle = Math.Atan2(vector.Y, vector.X);
        var octant = (int)Math.Round(8 * angle / (2 * Math.PI) + 8) % 8;

        return TextService.Translate($"AetherCurrentHelperWindow.Compass.{CompassHeadings[octant]}");
    }

    public string GetCompassDirection(Level level)
    {
        var localPlayer = ClientState.LocalPlayer;
        return localPlayer == null
            ? string.Empty
            : GetCompassDirection(
                new Vector2(-localPlayer.Position.X, localPlayer.Position.Z),
                new Vector2(-level.X, level.Z)
            );
    }

    public abstract class AetherCurrentEntryNode : Node
    {
        protected readonly TextService _textService;
        protected readonly IconNode _iconNode;
        protected readonly Node _contentNode;
        protected readonly TextNode _titleNode;
        protected readonly TextNode _coordsNode;

        public AetherCurrentEntryNode(TextService textService, int index, bool isUnlocked)
        {
            _textService = textService;

            FlexDirection = YGFlexDirection.Row;
            ColumnGap = ImGui.GetStyle().ItemSpacing.X;

            Add(_iconNode = new IconNode()
            {
                Icon = 0,
                Width = 40,
                Height = 40,
            });

            // TODO: SelectableNode??
            Add(_contentNode = new Node()
            {
                FlexGrow = 1,
                JustifyContent = YGJustify.Center,
            });

            _contentNode.Add(_titleNode = new TextNode() { TextColor = TitleColor });
            _contentNode.Add(_coordsNode = new TextNode());

            AddEventListener<MouseEvent>((node, evt) =>
            {
                Service.Get<IPluginLog>().Debug($"{evt.EventType}");
            });
        }

        public abstract void UpdateText();

        protected string GetHumanReadableCoords(Level level)
        {
            var coords = MapService.GetCoords(level);
            var x = coords.X.ToString("0.0", CultureInfo.InvariantCulture);
            var y = coords.Y.ToString("0.0", CultureInfo.InvariantCulture);
            return _textService.Translate("AetherCurrentHelperWindow.Coords", x, y);
        }
    }

    public class QuestNode(TextService textService, int index, bool isUnlocked, Quest quest) : AetherCurrentEntryNode(textService, index, isUnlocked)
    {
        private readonly int _index = index;
        private readonly bool _isUnlocked = isUnlocked;
        private readonly Quest _quest = quest;

        public override void UpdateContent()
        {
            UpdateText();
        }

        public override void UpdateText()
        {
            _iconNode.Icon = _quest.EventIconType.Value!.MapIconAvailable + 1;
            _titleNode.Text = $"[#{_index}] {_textService.GetQuestName(_quest.RowId)}";
            _coordsNode.Text = $"{GetHumanReadableCoords(_quest.IssuerLocation.Value)} | {_textService.GetENpcResidentName(_quest.IssuerStart.RowId)}";
        }
    }

    public class EObjNode(TextService textService, int index, bool isUnlocked, EObj eObj, Level level) : AetherCurrentEntryNode(textService, index, isUnlocked)
    {
        private readonly int _index = index;
        private readonly bool _isUnlocked = isUnlocked;
        private readonly EObj _eObj = eObj;
        private readonly Level _level = level;

        public override void UpdateContent()
        {
            UpdateText();
        }

        public override void UpdateText()
        {
            _iconNode.Icon = 60033;
            _titleNode.Text = $"[#{_index}] {_textService.GetEObjName(_eObj.RowId)}";
            _coordsNode.Text = GetHumanReadableCoords(_level);
        }
    }
}
