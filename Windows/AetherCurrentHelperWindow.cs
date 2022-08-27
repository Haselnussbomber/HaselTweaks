using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public class AetherCurrentHelperWindow : Window
{
    private readonly Dictionary<uint, EObj?> EObjCache = new(); // key is AetherCurrent.RowId
    private readonly Dictionary<uint, Level?> LevelCache = new(); // key is Level.RowId
    private AetherCurrentCompFlgSet? compFlgSet;
    private bool hideUnlocked = true; // false;

    private readonly Vector4 TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f, 1);

    public static AetherCurrentHelper.Configuration Config => Configuration.Instance.Tweaks.AetherCurrentHelper;

    public AetherCurrentHelperWindow() : base("[HaselTweaks] Aether Current Helper")
    {
        base.Size = new Vector2(300, 350);
        base.SizeCondition = ImGuiCond.Appearing;

        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.AlwaysAutoResize;
    }

    public void SetCompFlgSet(AetherCurrentCompFlgSet compFlgSet)
    {
        this.compFlgSet = compFlgSet;
    }

    public override bool DrawConditions()
    {
        return compFlgSet != null;
    }

    public override void Draw()
    {
        DrawMainCommandButton();

        var placeName = compFlgSet!.Territory.Value!.PlaceName.Value!.Name;

        var textSize = ImGui.CalcTextSize(placeName);
        var availableSize = ImGui.GetContentRegionAvail();
        var style = ImGui.GetStyle();
        var startPos = ImGui.GetCursorPos();

        ImGui.Checkbox("##HaselTweaks_AetherCurrents_HideUnlocked", ref hideUnlocked);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Check to hide unlocked Aether Currents");
        }

        ImGui.Dummy(new Vector2(Size!.Value.X, 0)); // set min-width

        ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
        ImGui.Text(placeName);
        ImGui.SetCursorPos(startPos + new Vector2(0, textSize.Y + style.ItemSpacing.Y * 4));

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4));

        if (!ImGui.BeginTable($"##HaselTweaks_AetherCurrents", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.NoPadOuterX))
        {
            ImGui.PopStyleVar();
            return;
        }

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Selectable", ImGuiTableColumnFlags.WidthFixed, 0);

        var index = 1;
        var type = 0;
        var linesDisplayed = 0;
        foreach (var aetherCurrent in compFlgSet.AetherCurrent)
        {
            if (aetherCurrent.Row == 0) continue;

            var isQuest = aetherCurrent.Value!.Quest.Row > 0;
            if (!isQuest && type == 0)
            {
                type = 1;
                index = 1;
            }

            var isUnlocked = Service.GameFunctions.IsAetherCurrentUnlocked(aetherCurrent.Row);
            if (!hideUnlocked || !isUnlocked)
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
                // ClientLanguage.Japanese => "",
                ClientLanguage.German => "Alle Windätherquellen gebündelt!",
                // ClientLanguage.French => "",
                _ => "All aether currents attuned!"
            };
            textSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(startPos + new Vector2(availableSize.X / 2 - textSize.X / 2, style.ItemSpacing.Y));
            ImGui.Text(text);
            ImGui.TableNextColumn();
        }

        ImGui.EndTable();
        ImGui.PopStyleVar();
    }

    private unsafe bool DrawMainCommandButton()
    {
        if (AtkUtils.GetUnitBase("AetherCurrent") != null)
        {
            return false;
        }

        var startPos = ImGui.GetCursorPos();
        var windowSize = ImGui.GetWindowSize();
        var style = ImGui.GetStyle();
        var iconSize = 26;

        ImGui.SetCursorPos(new Vector2(windowSize.X - style.ItemSpacing.X - iconSize, iconSize + style.ItemSpacing.Y));

        ImGuiUtils.DrawIcon(64, iconSize, iconSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Open Aether Currents Window");
        }

        if (ImGui.IsItemClicked())
        {
            var agent = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.AetherCurrent);
            if (agent != null && agent->AddonId == 0)
            {
                agent->Show();
            }
        }

        ImGui.SetCursorPos(startPos);

        return true;
    }

    private void DrawQuest(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var quest = aetherCurrent.Quest.Value!;

        ImGui.TableNextColumn();
        {
            ImGuiUtils.DrawIcon(quest.JournalGenre.Value!.Icon, 40, 40);
        }

        ImGui.TableNextColumn();
        {
            ImGui.TextColored(TitleColor, $"[#{index}] {Service.StringUtils.GetQuestName(quest.RowId, true)}");

            ImGui.Text(GetHumanReadableCoords(quest.IssuerLocation.Value!) + " | " + Service.StringUtils.GetENpcResidentName(quest.IssuerStart));
        }

        ImGui.TableNextColumn();
        {
            var selected = false;
            ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, 40));
            if (selected)
            {
                OpenMapLocation(quest.IssuerLocation.Value);
            }
            ImGui.SameLine();

            DrawUnlockStatus(isUnlocked, quest.IssuerLocation.Value);
            ImGui.SameLine(); // for padding
            ImGui.Dummy(new Vector2(0, 0));
        }
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var eobj = GetEObjByData(aetherCurrent.RowId);
        if (eobj == null) return;

        var level = GetLevelByObjectId(eobj.RowId);

        ImGui.TableNextColumn();
        {
            ImGuiUtils.DrawIcon(60033, 40, 40);
        }

        ImGui.TableNextColumn();
        {
            ImGui.TextColored(TitleColor, $"[#{index}] {Service.StringUtils.GetEObjName(eobj.RowId)}");
            ImGui.Text(GetHumanReadableCoords(level!));
        }

        ImGui.TableNextColumn();
        {
            var selected = false;
            ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, 40));
            if (selected)
            {
                OpenMapLocation(level);
            }
            ImGui.SameLine();

            DrawUnlockStatus(isUnlocked, level);
            ImGui.SameLine(); // for padding
            ImGui.Dummy(new Vector2(0, 0));
        }
    }

    private void DrawUnlockStatus(bool isUnlocked, Level? level)
    {
        var isSameTerritory = level?.Territory.Row == Service.ClientState.TerritoryType;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 11);

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
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(text).X / 2);

                    if (isUnlocked)
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), text);
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
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2);
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.3f, 0.3f, 0.3f, 1), FontAwesomeIcon.Times.ToIconString());
                ImGui.PopFont();
            }
        }
    }

    private void DrawCheckmark(bool isSameTerritory)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var icon = FontAwesomeIcon.Check.ToIconString();

        if (isSameTerritory)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(icon).X / 2);
        }

        ImGui.TextColored(new Vector4(0, 1, 0, 1), icon);
        ImGui.PopFont();
    }

    private EObj? GetEObjByData(uint aetherCurrentId)
    {
        if (EObjCache.ContainsKey(aetherCurrentId))
        {
            return EObjCache[aetherCurrentId];
        }

        var eobj = Service.Data.GetExcelSheet<EObj>()?.FirstOrDefault(row => row.Data == aetherCurrentId);

        EObjCache.Add(aetherCurrentId, eobj);

        return eobj;
    }

    private Level? GetLevelByObjectId(uint objId)
    {
        if (LevelCache.ContainsKey(objId))
        {
            return LevelCache[objId];
        }

        var level = Service.Data.GetExcelSheet<Level>()?.FirstOrDefault(row => row.Object == objId);

        LevelCache.Add(objId, level);

        return level;
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

    private readonly string[] CompassHeadings = new string[] { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };

    //! https://gamedev.stackexchange.com/a/49300
    public string GetCompassDirection(Vector2 a, Vector2 b)
    {
        var vector = a - b;
        var angle = Math.Atan2(vector.Y, vector.X);
        var octant = (int)Math.Round(8 * angle / (2 * Math.PI) + 8) % 8;

        return CompassHeadings[octant];
    }

    public string GetCompassDirection(Level? level)
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        return localPlayer == null || level == null
            ? string.Empty
            : GetCompassDirection(
                new Vector2(localPlayer.Position.X, localPlayer.Position.Z),
                new Vector2(level.X, level.Z)
            );
    }
}
