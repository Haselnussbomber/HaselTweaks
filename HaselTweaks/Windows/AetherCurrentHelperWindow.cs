using System.Globalization;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using HaselTweaks.Caches;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public unsafe class AetherCurrentHelperWindow : LockableWindow
{
    private readonly EObjDataIdCache EObjDataIdCache;
    private readonly QuestNameCache QuestNameCache;
    private bool HideUnlocked = true;

    private static readonly HaselColor TitleColor = new(216f / 255f, 187f / 255f, 125f / 255f);
    private static readonly string[] CompassHeadings = ["E", "NE", "N", "NW", "W", "SW", "S", "SE"];
    private AetherCurrentHelperConfiguration Config => PluginConfig.Tweaks.AetherCurrentHelper;

    private readonly IClientState ClientState;
    private readonly TextureManager TextureManager;

    public AetherCurrentHelperWindow(
        WindowManager windowManager,
        Configuration pluginConfig,
        IClientState clientState,
        CacheManager cacheManager,
        TextureManager textureManager)
        : base(windowManager, pluginConfig, "[HaselTweaks] Aether Current Helper")
    {
        ClientState = clientState;
        TextureManager = textureManager;

        EObjDataIdCache = cacheManager.Get<EObjDataIdCache>();
        QuestNameCache = cacheManager.Get<QuestNameCache>();

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
            ImGui.TextUnformatted(t("AetherCurrentHelperWindow.HideUnlockedTooltip"));
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
            var text = t("AetherCurrentHelperWindow.AllAetherCurrentsAttuned");
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

        TextureManager.GetIcon(64).Draw(iconSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(t("AetherCurrentHelperWindow.OpenAetherCurrentsWindowTooltip"));
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            GetAgent<AgentAetherCurrent>()->AgentInterface.Show();
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

        var quest = GetRow<Quest>(questId);
        if (questId == 0 || quest == null || quest.IssuerLocation.Row == 0)
            return;

        var extendedIssuerLocation = GetRow<ExtendedLevel>(quest.IssuerLocation.Row);
        if (extendedIssuerLocation == null)
            return;

        // Icon
        ImGui.TableNextColumn();
        TextureManager.GetIcon(quest.JournalGenre.Value!.Icon).Draw(40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {QuestNameCache.Get(quest.RowId)}");
        ImGui.TextUnformatted($"{GetHumanReadableCoords(extendedIssuerLocation)} | {GetENpcResidentName(quest.IssuerStart)}");

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##aetherCurrent-{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            extendedIssuerLocation.OpenMap();
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, extendedIssuerLocation);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawEObject(int index, bool isUnlocked, AetherCurrent aetherCurrent)
    {
        var eobj = EObjDataIdCache.Get(aetherCurrent.RowId);
        if (eobj == null) return;

        var level = ExtendedLevel.GetByObjectId(eobj.RowId);
        if (level == null) return;

        // Icon
        ImGui.TableNextColumn();
        TextureManager.GetIcon(60033).Draw(40);

        // Content
        ImGui.TableNextColumn();
        ImGuiUtils.TextUnformattedColored(TitleColor, $"[#{index}] {GetEObjName(eobj.RowId)}");
        ImGui.TextUnformatted(GetHumanReadableCoords(level!));

        // Actions
        ImGui.TableNextColumn();
        var selected = false;
        ImGui.Selectable($"##AetherCurrent_{aetherCurrent.RowId}", ref selected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y) * 2));
        if (selected)
        {
            level.OpenMap();
        }
        ImGui.SameLine();

        DrawUnlockStatus(isUnlocked, level);
        ImGui.SameLine(); // for padding
        ImGui.Dummy(new Vector2(0, 0));
    }

    private void DrawUnlockStatus(bool isUnlocked, ExtendedLevel level)
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
                var distance = level.GetDistanceFromPlayer();
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

    private void DrawCheckmark(bool isSameTerritory)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        var icon = FontAwesomeIcon.Check.ToIconString();

        if (isSameTerritory && Config.CenterDistance)
        {
            ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2 - ImGui.CalcTextSize(icon).X / 2);
        }

        ImGuiUtils.TextUnformattedColored(Colors.Green, icon);
    }

    private static string GetHumanReadableCoords(ExtendedLevel level)
    {
        var coords = level.GetCoords();
        var x = coords.X.ToString("0.0", CultureInfo.InvariantCulture);
        var y = coords.Y.ToString("0.0", CultureInfo.InvariantCulture);
        return t("AetherCurrentHelperWindow.Coords", x, y);
    }

    //! https://gamedev.stackexchange.com/a/49300
    public static string GetCompassDirection(Vector2 a, Vector2 b)
    {
        var vector = a - b;
        var angle = Math.Atan2(vector.Y, vector.X);
        var octant = (int)Math.Round(8 * angle / (2 * Math.PI) + 8) % 8;

        return t($"AetherCurrentHelperWindow.Compass.{CompassHeadings[octant]}");
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
