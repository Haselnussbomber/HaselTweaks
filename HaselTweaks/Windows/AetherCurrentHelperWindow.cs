using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Windows;

[RegisterSingleton]
public unsafe class AetherCurrentHelperWindow : LockableWindow
{
    private bool HideUnlocked = true;

    private static readonly Color TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);
    private static readonly string[] CompassHeadings = ["E", "NE", "N", "NW", "W", "SW", "S", "SE"];
    private AetherCurrentHelperConfiguration Config => PluginConfig.Tweaks.AetherCurrentHelper;

    private readonly IClientState ClientState;
    private readonly TextureService TextureService;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly MapService MapService;

    private readonly Dictionary<uint, EObj> AetherCurrentEObjCache = [];
    private readonly Dictionary<uint, Level> EObjLevelCache = [];

    public AetherCurrentHelperWindow(
        WindowManager windowManager,
        TextService textService,
        LanguageProvider languageProvider,
        PluginConfig pluginConfig,
        IClientState clientState,
        TextureService textureService,
        ExcelService excelService,
        MapService mapService)
        : base(windowManager, textService, languageProvider, pluginConfig)
    {
        ClientState = clientState;
        TextureService = textureService;
        ExcelService = excelService;
        TextService = textService;
        MapService = mapService;

        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(350);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(4096),
        };
    }

    public AetherCurrentCompFlgSet? CompFlgSet { get; set; }

    public override bool DrawConditions()
        => CompFlgSet.HasValue && ClientState.IsLoggedIn && !RaptureAtkModule.Instance()->RaptureAtkUnitManager.UiFlags.HasFlag(UIModule.UiFlags.ActionBars);

    public override unsafe void Draw()
    {
        DrawMainCommandButton();

        var placeName = CompFlgSet!.Value.Territory.Value.PlaceName.Value.Name.ExtractText();

        var textSize = ImGui.CalcTextSize(placeName);
        var availableSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var startPos = ImGui.GetCursorPos();

        ImGui.Checkbox("##HideUnlocked", ref HideUnlocked);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(TextService.Translate("AetherCurrentHelperWindow.HideUnlockedTooltip"));
            ImGui.EndTooltip();
        }

        ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
        ImGui.TextUnformatted(placeName);
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
            if (!HideUnlocked || !isUnlocked)
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
            var text = TextService.Translate("AetherCurrentHelperWindow.AllAetherCurrentsAttuned");
            textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
            ImGui.TextUnformatted(text);
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

        TextureService.DrawIcon(64, iconSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(TextService.Translate("AetherCurrentHelperWindow.OpenAetherCurrentsWindowTooltip"));
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
        ImGui.TextUnformatted($"{MapService.GetHumanReadableCoords(quest.IssuerLocation.Value)} | {TextService.GetENpcResidentName(quest.IssuerStart.RowId)}");

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
        ImGui.TextUnformatted(MapService.GetHumanReadableCoords(level));

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
                    var direction = distance > 1 ? MapService.GetCompassDirection(level) : string.Empty;
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
}
