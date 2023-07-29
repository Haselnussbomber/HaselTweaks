using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Caches;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using ImColor = HaselTweaks.Structs.ImColor;

namespace HaselTweaks.Windows;

public unsafe partial class AetherCurrentHelperWindow : Window
{
    private readonly AetherCurrentHelper _aetherCurrentHelper;
    private AetherCurrentCompFlgSet _compFlgSet;
    private readonly AgentAetherCurrent* _agentAetherCurrent;
    private readonly Dictionary<uint, EObj?> _eObjCache = new(); // key is AetherCurrent.RowId
    private readonly Dictionary<uint, Level?> _levelCache = new(); // key is Level.RowId
    private readonly Dictionary<uint, string> _questNameCache = new(); // key is Quest.RowId, value is stripped from private use utf8 chars
    private bool _hideUnlocked = true;

    private static readonly ImColor TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);

    [GeneratedRegex("^[\\ue000-\\uf8ff]+ ")]
    private static partial Regex Utf8PrivateUseAreaRegex();

    public static AetherCurrentHelper.Configuration Config => Plugin.Config.Tweaks.AetherCurrentHelper;

    public AetherCurrentHelperWindow(AetherCurrentHelper aetherCurrentHelper, AetherCurrentCompFlgSet compFlgSet) : base("[HaselTweaks] Aether Current Helper")
    {
        _aetherCurrentHelper = aetherCurrentHelper;
        _compFlgSet = compFlgSet;

        base.SizeCondition = ImGuiCond.Appearing;
        base.Size = new Vector2(350);
        base.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(4096),
        };

        GetAgent(AgentId.AetherCurrent, out _agentAetherCurrent);

    }

    public void SetCompFlgSet(AetherCurrentCompFlgSet compFlgSet)
        => _compFlgSet = compFlgSet;

    public override void OnClose()
    {
        _aetherCurrentHelper.CloseWindow();
    }

    public override bool DrawConditions()
        => Service.ClientState.IsLoggedIn;

    public override unsafe void Draw()
    {
        DrawMainCommandButton();

        var placeName = _compFlgSet.Territory.Value!.PlaceName.Value!.Name;

        var textSize = ImGui.CalcTextSize(placeName);
        var availableSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var startPos = ImGui.GetCursorPos();

        ImGui.Checkbox("##HaselTweaks_AetherCurrents_HideUnlocked", ref _hideUnlocked);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Check to hide unlocked Aether Currents");
        }

        ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
        ImGui.TextUnformatted(placeName);
        ImGui.SetCursorPos(startPos + new Vector2(0, textSize.Y + style.ItemSpacing.Y * 4));

        using var cellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4));
        using var table = ImRaii.Table($"##HaselTweaks_AetherCurrents", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed);

        var index = 1;
        var type = 0;
        var linesDisplayed = 0;
        var playerState = PlayerState.Instance();
        foreach (var aetherCurrent in _compFlgSet.AetherCurrent)
        {
            if (aetherCurrent.Row == 0) continue;

            var isQuest = aetherCurrent.Value!.Quest.Row > 0;
            if (!isQuest && type == 0)
            {
                type = 1;
                index = 1;
            }

            var isUnlocked = playerState->IsAetherCurrentUnlocked(aetherCurrent.Row);
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
            var text = Service.ClientState.ClientLanguage switch
            {
                ClientLanguage.German => "Alle Windätherquellen gebündelt!",
                ClientLanguage.French => "Toutes les sources d'éther sont réunies !",
                ClientLanguage.Japanese => "すべてのエーテル風車を結集！",
                _ => "All aether currents attuned!"
            };
            textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
            ImGui.TextUnformatted(text);
            ImGui.TableNextColumn();
        }
    }

    private unsafe bool DrawMainCommandButton()
    {
        if (GetAddon((AgentInterface*)_agentAetherCurrent) != null)
            return false;

        var startPos = ImGui.GetCursorPos();
        var windowSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var iconSize = ImGui.GetFrameHeight();

        ImGui.SetCursorPosX(windowSize.X + style.WindowPadding.X - iconSize - 1);

        Service.TextureCache.GetIcon(64).Draw(iconSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Open Aether Currents Window");
        }

        if (ImGui.IsItemClicked())
        {
            _agentAetherCurrent->AgentInterface.Show();
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

        var quest = Service.DataManager.GetExcelSheet<Quest>()?.GetRow(questId);
        if (questId == 0 || quest == null) return;

        // Icon
        ImGui.TableNextColumn();
        Service.TextureCache.GetIcon(quest.JournalGenre.Value!.Icon).Draw(40);

        // Content
        ImGui.TableNextColumn();
        if (!_questNameCache.TryGetValue(quest.RowId, out var questName))
        {
            questName = Utf8PrivateUseAreaRegex().Replace(StringCache.GetSheetText<Quest>(quest.RowId, "Name"), "");
            _questNameCache.Add(quest.RowId, questName);
        }
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {questName}");
        ImGui.TextUnformatted(GetHumanReadableCoords(quest.IssuerLocation.Value!) + " | " + StringCache.GetENpcResidentName(quest.IssuerStart));

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            OpenMapLocation(quest.IssuerLocation.Value);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, quest.IssuerLocation.Value);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var eobj = GetEObjByData(aetherCurrent.RowId);
        if (eobj == null) return;

        var level = GetLevelByObjectId(eobj.RowId);

        // Icon
        ImGui.TableNextColumn();
        Service.TextureCache.GetIcon(60033).Draw(40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {StringCache.GetEObjName(eobj.RowId)}");
        ImGui.TextUnformatted(GetHumanReadableCoords(level!));

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            OpenMapLocation(level);
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, level);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private static void DrawUnlockStatus(bool isUnlocked, Level? level)
    {
        var isSameTerritory = level?.Territory.Row == Service.ClientState.TerritoryType;
        ImGuiUtils.PushCursorY(11);

        if (isUnlocked && !Config.AlwaysShowDistance)
        {
            DrawCheckmark(isSameTerritory);
        }
        else
        {
            if (isSameTerritory)
            {
                var distance = GetDistance(level);
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
                        ImGuiUtils.TextUnformattedColored(Colors.Green, text);
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
                ImGuiUtils.TextUnformattedColored(Colors.Grey4, FontAwesomeIcon.Times.ToIconString());
            }
        }
    }

    private static void DrawCheckmark(bool isSameTerritory)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        var icon = FontAwesomeIcon.Check.ToIconString();

        if (isSameTerritory && Config.CenterDistance)
        {
            ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(icon).X / 2);
        }

        ImGuiUtils.TextUnformattedColored(Colors.Green, icon);
    }

    private EObj? GetEObjByData(uint aetherCurrentId)
    {
        if (!_eObjCache.TryGetValue(aetherCurrentId, out var value))
        {
            value = Service.DataManager.GetExcelSheet<EObj>()?.FirstOrDefault(row => row.Data == aetherCurrentId);
            _eObjCache.Add(aetherCurrentId, value);
        }

        return value;
    }

    private Level? GetLevelByObjectId(uint objId)
    {
        if (!_levelCache.TryGetValue(objId, out var value))
        {
            value = Service.DataManager.GetExcelSheet<Level>()?.FirstOrDefault(row => row.Object == objId);
            _levelCache.Add(objId, value);
        }

        return value;
    }

    private static Vector2 GetLevelPos(Level level)
    {
        var map = level.Map.Value;
        var c = map!.SizeFactor / 100.0f;
        var x = 41.0f / c * (((level.X + map.OffsetX) * c + 1024.0f) / 2048.0f) + 1f;
        var y = 41.0f / c * (((level.Z + map.OffsetY) * c + 1024.0f) / 2048.0f) + 1f;
        return new(x, y);
    }

    private static string GetHumanReadableCoords(Level level)
    {
        var coords = GetLevelPos(level);
        return $"X: {coords.X.ToString("0.0", CultureInfo.InvariantCulture)}, Y: {coords.Y.ToString("0.0", CultureInfo.InvariantCulture)}";
    }

    private static void OpenMapLocation(Level? level)
    {
        if (level == null)
            return;

        var map = level?.Map?.Value;
        var terr = map?.TerritoryType?.Value;

        if (terr == null)
            return;

        Service.GameGui.OpenMapWithMapLink(new MapLinkPayload(
            terr.RowId,
            map!.RowId,
            (int)(level!.X * 1_000f),
            (int)(level.Z * 1_000f)
        ));
    }

    public static float GetDistance(Level? level)
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        if (localPlayer == null || level == null || level.Territory.Row != Service.ClientState.TerritoryType)
        {
            return float.MaxValue; // far, far away
        }

        return Vector2.Distance(
            new Vector2(localPlayer.Position.X, localPlayer.Position.Z),
            new Vector2(level.X, level.Z)
        );
    }

    private static readonly string[] CompassHeadings = new string[] { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    //! https://gamedev.stackexchange.com/a/49300
    public static string GetCompassDirection(Vector2 a, Vector2 b)
    {
        var vector = a - b;
        var angle = Math.Atan2(vector.Y, vector.X);
        var octant = (int)Math.Round(8 * angle / (2 * Math.PI) + 8) % 8;

        return CompassHeadings[octant];
    }

    public static string GetCompassDirection(Level? level)
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        return localPlayer == null || level == null
            ? string.Empty
            : GetCompassDirection(
                new Vector2(-localPlayer.Position.X, localPlayer.Position.Z),
                new Vector2(-level.X, level.Z)
            );
    }
}
