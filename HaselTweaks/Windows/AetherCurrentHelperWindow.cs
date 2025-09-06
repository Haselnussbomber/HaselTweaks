using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AetherCurrentHelperWindow : SimpleWindow
{
    private static readonly Color TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);
    private static readonly string[] CompassHeadings = ["E", "NE", "N", "NW", "W", "SW", "S", "SE"];

    private readonly IClientState _clientState;
    private readonly ITextureProvider _textureProvider;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly MapService _mapService;
    private readonly PluginConfig _pluginConfig;

    private readonly Dictionary<uint, EObj> _aetherCurrentEObjCache = [];
    private readonly Dictionary<uint, Level> _eObjLevelCache = [];

    private bool _hideUnlocked = true;

    private AetherCurrentHelperConfiguration Config => _pluginConfig.Tweaks.AetherCurrentHelper;

    public AetherCurrentCompFlgSet? CompFlgSet { get; set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(350);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(4096),
        };
    }

    public override bool DrawConditions()
        => CompFlgSet.HasValue && _clientState.IsLoggedIn && !RaptureAtkUnitManager.Instance()->UiFlags.HasFlag(UIModule.UiFlags.ActionBars);

    public override unsafe void Draw()
    {
        DrawMainCommandButton();

        var placeName = CompFlgSet!.Value.Territory.Value.PlaceName.Value.Name.ToString();

        var textSize = ImGui.CalcTextSize(placeName);
        var availableSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var startPos = ImGui.GetCursorPos();

        ImGui.Checkbox("##HideUnlocked", ref _hideUnlocked);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(_textService.Translate("AetherCurrentHelperWindow.HideUnlockedTooltip"));
            ImGui.EndTooltip();
        }

        ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
        ImGui.Text(placeName);
        ImGui.SetCursorPos(startPos + new Vector2(0, textSize.Y + style.ItemSpacing.Y * 4));

        using var cellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4));
        using var table = ImRaii.Table($"##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX);
        if (!table)
            return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed);

        var index = 1;
        var type = 0;
        var linesDisplayed = 0;
        var playerState = PlayerState.Instance();
        foreach (var aetherCurrent in CompFlgSet.Value.AetherCurrents)
        {
            if (aetherCurrent.RowId == 0) continue;

            var isQuest = aetherCurrent.Value.Quest.RowId > 0;
            if (!isQuest && type == 0)
            {
                type = 1;
                index = 1;
            }

            var isUnlocked = playerState->IsAetherCurrentUnlocked(aetherCurrent.RowId);
            if (!_hideUnlocked || !isUnlocked)
            {
                if (type == 0)
                {
                    DrawQuest(index, isUnlocked, aetherCurrent.Value);
                }
                else if (type == 1)
                {
                    DrawEObject(index, isUnlocked, aetherCurrent.Value);
                }

                linesDisplayed++;
            }

            index++;
        }

        if (linesDisplayed == 0)
        {
            // hacky, but it looks the best
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            startPos = ImGui.GetCursorPos() + new Vector2(-startPos.X - 4, 0);
            var text = _textService.Translate("AetherCurrentHelperWindow.AllAetherCurrentsAttuned");
            textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
            ImGui.Text(text);
            ImGui.TableNextColumn();
        }

        if (!Flags.HasFlag(ImGuiWindowFlags.NoFocusOnAppearing))
            Flags |= ImGuiWindowFlags.NoFocusOnAppearing;
    }

    private unsafe bool DrawMainCommandButton()
    {
        if (IsAddonOpen(AgentId.AetherCurrent))
            return false;

        var startPos = ImGui.GetCursorPos();
        var windowSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var iconSize = ImGui.GetFrameHeight();

        ImGui.SetCursorPosX(windowSize.X + style.WindowPadding.X - iconSize - 1);

        _textureProvider.DrawIcon(64, iconSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(_textService.Translate("AetherCurrentHelperWindow.OpenAetherCurrentsWindowTooltip"));
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            AgentAetherCurrent.Instance()->Show();
        }

        ImGui.SetCursorPos(startPos);

        return true;
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

        if (!_excelService.TryGetRow<Quest>(questId, out var quest) || quest.IssuerLocation.RowId == 0)
            return;

        if (!quest.IssuerLocation.IsValid)
            return;

        // Icon
        ImGui.TableNextColumn();
        _textureProvider.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, 40);

        // Content
        ImGui.TableNextColumn();
        ImGui.TextColored(TitleColor, $"[#{index}] {_textService.GetQuestName(quest.RowId)}");
        ImGui.Text($"{_mapService.GetHumanReadableCoords(quest.IssuerLocation.Value)} | {_textService.GetENpcResidentName(quest.IssuerStart.RowId)}");

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            _mapService.OpenMap(quest.IssuerLocation.Value);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, quest.IssuerLocation.Value);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        if (!_aetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobj))
        {
            if (!_excelService.TryFindRow(row => row.Data == aetherCurrent.RowId, out eobj))
                return;

            _aetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj);
        }

        if (!_eObjLevelCache.TryGetValue(eobj.RowId, out var level))
        {
            if (!_excelService.TryFindRow(row => row.Object.RowId == eobj.RowId, out level))
                return;

            _eObjLevelCache.Add(eobj.RowId, level);
        }

        // Icon
        ImGui.TableNextColumn();
        _textureProvider.DrawIcon(60033, 40);

        // Content
        ImGui.TableNextColumn();
        ImGui.TextColored(TitleColor, $"[#{index}] {_textService.GetEObjName(eobj.RowId)}");
        ImGui.Text(_mapService.GetHumanReadableCoords(level));

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##AetherCurrent_{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            _mapService.OpenMap(level);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, level);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawUnlockStatus(bool isUnlocked, Level level)
    {
        var isSameTerritory = level.Territory.RowId == _clientState.TerritoryType;
        ImGuiUtils.PushCursorY(11);

        if (isUnlocked && !Config.AlwaysShowDistance)
        {
            DrawCheckmark(isSameTerritory);
        }
        else
        {
            if (isSameTerritory)
            {
                var distance = _mapService.GetDistanceFromPlayer(level);
                if (distance < float.MaxValue)
                {
                    var direction = distance > 1 ? _mapService.GetCompassDirection(level) : string.Empty;
                    var text = $"{distance:0}y {direction}";

                    if (Config.CenterDistance)
                    {
                        ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);
                    }

                    if (isUnlocked)
                    {
                        ImGui.TextColored(Color.Green, text);
                    }
                    else
                    {
                        ImGui.Text(text);
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
                ImGui.TextColored(Color.Grey4, FontAwesomeIcon.Times.ToIconString());
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

        ImGui.TextColored(Color.Green, icon);
    }
}
