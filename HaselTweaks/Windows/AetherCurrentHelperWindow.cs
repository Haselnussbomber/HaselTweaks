using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Caches;
using HaselTweaks.Config;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public unsafe class AetherCurrentHelperWindow : LockableWindow
{
    private readonly EObjDataIdCache EObjDataIdCache;
    private readonly LevelObjectCache LevelObjectCache;
    private bool HideUnlocked = true;

    private static readonly Color TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);
    private static readonly string[] CompassHeadings = ["E", "NE", "N", "NW", "W", "SW", "S", "SE"];
    private AetherCurrentHelperConfiguration Config => PluginConfig.Tweaks.AetherCurrentHelper;

    private readonly IClientState ClientState;
    private readonly TextureService TextureService;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly MapService MapService;

    public AetherCurrentHelperWindow(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        IClientState clientState,
        TextureService textureService,
        ExcelService excelService,
        TextService textService,
        MapService mapService,
        EObjDataIdCache eObjDataIdCache,
        LevelObjectCache levelObjectCache)
        : base(windowManager, pluginConfig, textService, "[HaselTweaks] Aether Current Helper")
    {
        ClientState = clientState;
        TextureService = textureService;
        ExcelService = excelService;
        TextService = textService;
        MapService = mapService;
        EObjDataIdCache = eObjDataIdCache;
        LevelObjectCache = levelObjectCache;

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
        => CompFlgSet != null && ClientState.IsLoggedIn && !RaptureAtkModule.Instance()->RaptureAtkUnitManager.UiFlags.HasFlag(UIModule.UiFlags.ActionBars);

    public override unsafe void Draw()
    {
        DrawMainCommandButton();

        var placeName = CompFlgSet!.Territory.Value!.PlaceName.Value!.Name;

        var textSize = ImGui.CalcTextSize(placeName);
        var availableSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var startPos = ImGui.GetCursorPos();

        ImGui.Checkbox("##HideUnlocked", ref HideUnlocked);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            TextService.Draw("AetherCurrentHelperWindow.HideUnlockedTooltip");
            ImGui.EndTooltip();
        }

        ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
        ImGui.TextUnformatted(placeName);
        ImGui.SetCursorPos(startPos + new Vector2(0, textSize.Y + style.ItemSpacing.Y * 4));

        using var cellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4));
        using var table = ImRaii.Table($"##Table", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed);

        var index = 1;
        var type = 0;
        var linesDisplayed = 0;
        var playerState = PlayerState.Instance();
        foreach (var aetherCurrent in CompFlgSet.AetherCurrent)
        {
            if (aetherCurrent.Row == 0) continue;

            var isQuest = aetherCurrent.Value!.Quest.Row > 0;
            if (!isQuest && type == 0)
            {
                type = 1;
                index = 1;
            }

            var isUnlocked = playerState->IsAetherCurrentUnlocked(aetherCurrent.Row);
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
            TextService.Draw("AetherCurrentHelperWindow.OpenAetherCurrentsWindowTooltip");
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
        var questId = aetherCurrent.Quest.Row;

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

        var quest = ExcelService.GetRow<Quest>(questId);
        if (questId == 0 || quest == null || quest.IssuerLocation.Row == 0)
            return;

        var issuerLocation = ExcelService.GetRow<Level>(quest.IssuerLocation.Row);
        if (issuerLocation == null)
            return;

        // Icon
        ImGui.TableNextColumn();
        TextureService.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, 40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {TextService.GetQuestName(quest.RowId)}");
        ImGui.TextUnformatted($"{GetHumanReadableCoords(issuerLocation)} | {TextService.GetENpcResidentName(quest.IssuerStart)}");

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            MapService.OpenMap(issuerLocation);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, issuerLocation);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        if (!EObjDataIdCache.TryGetValue(aetherCurrent.RowId, out var eobj))
            return;

        if (!LevelObjectCache.TryGetValue(eobj.RowId, out var level))
            return;

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
        var isSameTerritory = level.Territory.Row == ClientState.TerritoryType;
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

    public string GetCompassDirection(Level? level)
    {
        var localPlayer = ClientState.LocalPlayer;
        return localPlayer == null || level == null
            ? string.Empty
            : GetCompassDirection(
                new Vector2(-localPlayer.Position.X, localPlayer.Position.Z),
                new Vector2(-level.X, level.Z)
            );
    }
}
