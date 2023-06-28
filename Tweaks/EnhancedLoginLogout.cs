using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Memory;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using HaselTweaks.Records;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using HaselAgentLobby = HaselTweaks.Structs.AgentLobby;
using HaselCharacter = HaselTweaks.Structs.Character;

namespace HaselTweaks.Tweaks;

// things went downhill fast...
public unsafe partial class EnhancedLoginLogout : Tweak
{
    public override string Name => "Enhanced Login/Logout";

    #region Core

    public override void Enable() => UpdateCharacterSettings();
    public override void OnLogin() => UpdateCharacterSettings();
    public override void Disable() => CleanupCharaSelect();
    public override void Dispose() => Service.Framework.RunOnFrameworkThread(UnloadTextures);
    public override void OnConfigWindowClose() => Service.Framework.RunOnFrameworkThread(UnloadTextures);

    private CharaSelectCharacter? CurrentEntry = null;
    private ulong ActiveContentId => CurrentEntry?.ContentId ?? Service.ClientState.LocalContentId;

    private void UpdateCharacterSettings()
    {
        UpdatePetMirageSettings();
        UpdateVoiceCache();
        UpdateUnlockedEmotes();
    }

    public void UnloadTextures()
    {
        TextureManager?.Dispose();
        TextureManager = null;
        EmoteCategoryTexture = null;
        BgPartsTexture = null;
        EmoteTexture = null;
    }

    private void CleanupCharaSelect()
    {
        DespawnPet();
        CurrentEntry = null;
    }

    [Signature("E8 ?? ?? ?? ?? 48 8B 48 08 49 89 8C 24")]
    private readonly GetCharacterEntryByIndexDelegate GetCharacterEntryByIndex = null!;
    private delegate CharaSelectCharacterEntry* GetCharacterEntryByIndexDelegate(nint a1, int a2, int a3, int index);

    // called every frame
    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.UpdateCharaSelectDisplay))]
    public void UpdateCharaSelectDisplay(HaselAgentLobby* agent, sbyte index, bool a2)
    {
        UpdateCharaSelectDisplayHook.OriginalDisposeSafe(agent, index, a2);

        if (index < 0)
        {
            CleanupCharaSelect();
            return;
        }

        if (index >= 100)
            index -= 100;

        var entry = GetCharacterEntryByIndex((nint)agent + 0x40, 0, agent->Unk10F2, index); // what a headache
        if (entry == null)
        {
            CleanupCharaSelect();
            return;
        }

        if (CurrentEntry?.ContentId == entry->ContentId)
            return;

        var character = CharaSelect.GetCurrentCharacter();
        if (character == null)
            return;

        CurrentEntry = new(character, entry);

        SpawnPet();
        SetVoice();

        if (Config.SelectedEmotes.TryGetValue(ActiveContentId, out var emoteId))
            PlayEmote(emoteId);
    }

    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.CleanupCharaSelectCharacters))]
    public void CleanupCharaSelectCharacters()
    {
        CleanupCharaSelect();
        CleanupCharaSelectCharactersHook.OriginalDisposeSafe();
    }

    #endregion

    #region Config

    private TextureManager? TextureManager;
    private Texture? EmoteCategoryTexture;
    private Texture? BgPartsTexture;
    private Texture? EmoteTexture;
    private uint SelectedEmoteCategory = 1;
    private string EmoteSearchInput = string.Empty;
    private uint HoverEmote;
    private readonly uint[] AllowedChangePoseEmoteIds = new uint[] { 91, 92, 93, 107, 108, 218, 219, };
    private readonly uint[] ExcludedEmotes = new uint[] { /* Sit */ 50, };

    public static Configuration Config => Plugin.Config.Tweaks.EnhancedLoginLogout;

    public class Configuration
    {
        public bool ShowPets = false;
        public bool EnableCharaSelectEmote = false;
        public bool PreloadTerritory = true;
        public bool ClearTellHistory = false;

        public Vector3 PetPosition = new(-0.6f, 0f, 0f);

        public Dictionary<ulong, PetMirageSetting> PetMirageSettings = new();
        public Dictionary<ulong, uint> SelectedEmotes = new();
        public Dictionary<ulong, List<uint>> UnlockedEmotes = new();
        public Dictionary<ulong, ushort> VoiceCache = new();

        public class PetMirageSetting
        {
            public uint CarbuncleType;
            public uint FairyType;
        }
    }

    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        TextureManager ??= new();

        ImGui.TextUnformatted("Login options:");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

        // ShowPets
        if (ImGui.Checkbox($"Show pets in character selection##HaselTweaks_Config_{InternalName}_ShowPets", ref Config.ShowPets))
        {
            if (!Config.ShowPets)
                DespawnPet();
            else
                SpawnPet();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "Displays a carbuncle (Arcanist/Summoner) or a fairy (Scholar) next to your character.");

            if (ActiveContentId != 0)
            {
                if (!Config.PetMirageSettings.ContainsKey(ActiveContentId))
                    ImGui.TextColored(ImGuiUtils.ColorRed, "Pet Glamor settings for this character not cached! Please log in once.");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        }

        // PetPosition
        var showPetsDisabled = Config.ShowPets ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.DragFloat3($"Position##HaselTweaks_Config_{InternalName}_Position", ref Config.PetPosition, 0.01f, -10f, 10f))
            {
                ApplyPetPosition();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton($"##HaselTweaks_Config_{InternalName}_Position_Reset", FontAwesomeIcon.Undo, "Reset to Default: -1, 0, 0"))
            {
                Config.PetPosition = new(-0.6f, 0f, 0f);
                ApplyPetPosition();
            }
        }

        showPetsDisabled?.Dispose();

        // PlayEmote
        if (ImGui.Checkbox($"Play emote in character selection##HaselTweaks_Config_{InternalName}_PlayEmote", ref Config.EnableCharaSelectEmote))
        {
            if (!Config.EnableCharaSelectEmote && CurrentEntry != null && CurrentEntry.Character != null)
            {
                ResetEmoteMode();
                CurrentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(3);
            }
        }

        var playEmoteDisabled = Config.EnableCharaSelectEmote ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "Let your character dance, strike a pose or blow you a kiss!");
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "Emote settings are per-character.");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

            if (Config.EnableCharaSelectEmote)
            {
                if (ActiveContentId != 0)
                {
                    /*
#if DEBUG
                    ImGui.Text($"Current Character: {selectedCharacterName}");
                    if (ImGui.IsItemHovered())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsItemClicked())
                        ImGui.SetClipboardText($"{(nint)character:X}");
#endif
                    */

                    if (!Config.VoiceCache.ContainsKey(ActiveContentId))
                    {
                        ImGui.TextColored(ImGuiUtils.ColorRed, "Voice ID for this character not cached! Please log in once.");
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
                    }
                    
                    if (!Config.UnlockedEmotes.TryGetValue(ActiveContentId, out var unlockedEmotes))
                    {
                        ImGui.TextColored(ImGuiUtils.ColorRed, "Unlocked emotes for this character not cached! Please log in once.");
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
                    }
                    else
                    {
                        ImGui.Text("Current Emote:");
                        ImGui.SameLine();

                        if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
                        {
                            ImGui.Text("None");
                        }
                        else
                        {
                            var defaultIdlePoseEmote = Service.Data.GetExcelSheet<Emote>()!.GetRow(90)!; // first "Change Pose"
                            var changePoseIndex = 1;

                            var entry = Service.Data.GetExcelSheet<Emote>()!
                                .Select(row => (
                                    IsChangePose: AllowedChangePoseEmoteIds.Contains(row.RowId),
                                    Name: AllowedChangePoseEmoteIds.Contains(row.RowId) ? $"{defaultIdlePoseEmote.Name.ToDalamudString()} ({changePoseIndex++})" : $"{row.Name.ToDalamudString()}",
                                    Emote: row
                                ) as (bool IsChangePose, string Name, Emote Emote)?)
                                .FirstOrDefault(entry => entry != null && entry.Value.Emote.RowId == selectedEmoteId, null);

                            if (entry.HasValue)
                            {
                                var (isChangePose, name, emote) = entry.Value;
                                TextureManager.GetIcon(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon).Draw(new(24));
                                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                                ImGuiUtils.PushCursorY(3);
                                ImGui.Text(name);
                            }
                            else
                            {
                                ImGui.Text("Unknown");
                            }
                        }

                        if (ImGui.Button("Select Emote"))
                        {
                            EmoteSearchInput = string.Empty;
                            ImGui.OpenPopup("##EmotePicker");
                            UIModule.PlaySound(23, 0, 0, 0);
                        }

                        if (selectedEmoteId > 0)
                        {
                            ImGui.SameLine();

                            if (ImGuiUtils.IconButton("##EmoteReset", FontAwesomeIcon.Undo, "Reset"))
                            {
                                selectedEmoteId = 0;
                                SaveEmote(0);
                            }
                        }

                        DrawEmotePopup(selectedEmoteId, unlockedEmotes);
                    }
                }
            }
        }

        playEmoteDisabled?.Dispose();

        // PreloadTerritory
        ImGui.Checkbox($"Preload territory when queued##HaselTweaks_Config_{InternalName}_PreloadTerritory", ref Config.PreloadTerritory);
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "When it puts you in queue, it will preload the territory textures in the background, just as it does as when you start teleporting.");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        ImGui.TextUnformatted("Logout options:");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

        // ClearTellHistory
        ImGui.Checkbox($"Clear tell history on logout##HaselTweaks_Config_{InternalName}_ClearTellHistory", ref Config.ClearTellHistory);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046")]
    private void DrawEmotePopup(uint selectedEmoteId, List<uint> unlockedEmotes)
    {
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1);
        using var borderRadius = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 5);
        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(ImGuiUtils.ColorGold));

        using var popup = ImRaii.Popup("##EmotePicker", ImGuiWindowFlags.NoMove);
        if (!popup.Success)
            return;

        borderSize?.Dispose();
        borderRadius?.Dispose();
        borderColor?.Dispose();

        EmoteCategoryTexture ??= TextureManager?.GetTexture("ui/uld/EmoteCategory.tex");
        BgPartsTexture ??= TextureManager?.GetTexture("ui/uld/BgParts.tex");
        EmoteTexture ??= TextureManager?.GetTexture("ui/uld/Emote.tex");

        ImGuiUtils.PushCursorY(3);

        var isSearching = !string.IsNullOrEmpty(EmoteSearchInput);
        var tabsDisable = !isSearching ? null : ImRaii.Disabled();

        foreach (var emoteCategory in Service.Data.GetExcelSheet<EmoteCategory>()!)
        {
            if (emoteCategory.RowId == 0 || string.IsNullOrEmpty(emoteCategory.Name))
                continue;

            ImGui.SameLine(0, 0);

            var pos = ImGui.GetCursorPos();
            EmoteCategoryTexture?.DrawPart(new Vector2(36 * emoteCategory.RowId, 0), new Vector2(36), new(36));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.SetTooltip(emoteCategory.Name);
            }

            if (!isSearching && SelectedEmoteCategory == emoteCategory.RowId)
            {
                ImGui.SetCursorPos(pos);
                BgPartsTexture?.DrawPart(new Vector2(69, 1), new Vector2(36), new(36));
            }

            if (ImGui.IsItemClicked())
            {
                UIModule.PlaySound(1, 0, 0, 0);
                SelectedEmoteCategory = emoteCategory.RowId;
            }
        }

        tabsDisable?.Dispose();

        ImGui.SameLine();
        ImGuiUtils.PushCursorY(6);
        ImGui.InputTextWithHint("##EmoteSearch", "Search...", ref EmoteSearchInput, 100, ImGuiInputTextFlags.AutoSelectAll);
        if (ImGui.IsItemActivated())
        {
            UIModule.PlaySound(35, 0, 0, 0);
        }
        if (ImGui.IsItemDeactivated())
        {
            UIModule.PlaySound(3, 0, 0, 0);
        }

        ImGuiUtils.PushCursorY(3);
        using var child = ImRaii.Child("##EmoteTableWrapper", new(400, 300), true);
        if (!child.Success)
            return;

        using var table = ImRaii.Table("##EmoteTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 24);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

        var defaultIdlePoseEmote = Service.Data.GetExcelSheet<Emote>()!.GetRow(90)!; // first "Change Pose"
        var changePoseIndex = 1;

        var sortedEmotes = Service.Data.GetExcelSheet<Emote>()!
            .Select(row => (Name: AllowedChangePoseEmoteIds.Contains(row.RowId) ? $"{defaultIdlePoseEmote.Name.ToDalamudString()} ({changePoseIndex++})" : $"{row.Name.ToDalamudString()}", row))
            .Where(entry =>
            {
                var row = entry.row;

                if (row.RowId == defaultIdlePoseEmote.RowId || AllowedChangePoseEmoteIds.Contains(row.RowId))
                    return true;

                if (row.Order == 0)
                    return false;

                // Check if usable under normal conditions
                // part of E8 ?? ?? ?? ?? 84 C0 74 C5
                if (!(row.RowId - 60 > 1 && row.ActionTimeline[0].Row != 0))
                    return false;

                return !ExcludedEmotes.Contains(row.RowId);
            })
            .OrderBy(entry => entry.Name)
            .ToArray();

        foreach (var entry in sortedEmotes)
        {
            var emote = entry.row;
            var isAlternativeChangePoseEmote = AllowedChangePoseEmoteIds.Contains(emote.RowId);

            if (isAlternativeChangePoseEmote && SelectedEmoteCategory != 1)
                continue;

            if (!isSearching && !isAlternativeChangePoseEmote && emote.EmoteCategory.Row != SelectedEmoteCategory)
                continue;

            if (!isAlternativeChangePoseEmote && !unlockedEmotes!.Contains(emote.RowId))
                continue;

            var effectiveEmote = emote;
            var effectiveEmoteId = emote.RowId;

            if (isAlternativeChangePoseEmote)
                effectiveEmote = defaultIdlePoseEmote;

            var emoteName = entry.Name;

            if (!string.IsNullOrEmpty(EmoteSearchInput) && !emoteName.ToLower().Contains(EmoteSearchInput.ToLower()))
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            TextureManager?.GetIcon(effectiveEmote.Icon).Draw(new(24));

            ImGui.TableNextColumn();
            if (ImGui.Selectable($"##Emote_{effectiveEmoteId}", selectedEmoteId == effectiveEmoteId, ImGuiSelectableFlags.None, new(0, 24)))
            {
                UIModule.PlaySound(1, 0, 0, 0);
                ResetEmoteMode();
                SaveEmote(effectiveEmoteId);
                ImGui.CloseCurrentPopup();
            }
            if (ImGuiUtils.IsGameWindowFocused() && ImGui.IsItemHovered() && HoverEmote != effectiveEmoteId)
            {
                PlayEmote(effectiveEmoteId);
                HoverEmote = effectiveEmoteId;
            }
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorY(3);
            ImGui.Text(emoteName);
        }
    }

    #endregion

    #region Login: Show pets in character selection

    private BattleChara* Pet = null;
    private short PetIndex = -1;

    private void UpdatePetMirageSettings()
    {
        var playerState = PlayerState.Instance();
        if (playerState == null || playerState->IsLoaded != 0x01)
            return;

        var contentId = playerState->ContentId;
        if (contentId == 0)
            return;

        if (!Config.PetMirageSettings.TryGetValue(contentId, out var petMirageSettings))
            Config.PetMirageSettings.Add(contentId, petMirageSettings = new());

        try
        {
            Service.GameConfig.UiConfig.TryGetUInt("PetMirageTypeCarbuncleSupport", out petMirageSettings.CarbuncleType);
        }
        catch (Exception e)
        {
            Error("Error reading pet glamours", e);
        }

        try
        {
            Service.GameConfig.UiConfig.TryGetUInt("PetMirageTypeFairy", out petMirageSettings.FairyType);
        }
        catch (Exception e)
        {
            Error("Error reading pet glamours", e);
        }

        Debug($"Updated PetMirageSettings: CarbuncleType {petMirageSettings.CarbuncleType}, FairyType {petMirageSettings.FairyType}");
    }

    [AddressHook<ConfigEntry>(nameof(ConfigEntry.Addresses.SetValueUInt))]
    public bool ConfigEntry_SetValueUInt(ConfigEntry* entry, uint value, uint unk = 1)
    {
        if (entry->Owner == &Framework.Instance()->SystemConfig.CommonSystemConfig.UiConfig &&
            MemoryHelper.ReadStringNullTerminated((nint)entry->Name) is "PetMirageTypeCarbuncleSupport" or "PetMirageTypeFairy")
        {
            UpdatePetMirageSettings();
        }

        return ConfigEntry_SetValueUIntHook.OriginalDisposeSafe(entry, value, unk);
    }

    public void SpawnPet()
    {
        if (!Config.ShowPets || CurrentEntry == null)
            return;

        if (!(CurrentEntry.ClassJobId is 26 or 27 or 28)) // Arcanist, Summoner, Scholar (Machinist: 31)
        {
            DespawnPet();
            return;
        }

        if (Pet != null)
            return;

        if (!Config.PetMirageSettings.TryGetValue(ActiveContentId, out var petMirageSettings))
            return;

        var bNpcId = CurrentEntry.ClassJobId switch
        {
            26 => 13498u + petMirageSettings.CarbuncleType, // Arcanist
            27 => 13498u + petMirageSettings.CarbuncleType, // Summoner
            28 => 1008u + petMirageSettings.FairyType, // Scholar
            _ => 0u
        };

        if (bNpcId == 0)
            return;

        var clientObjectManager = ClientObjectManager.Instance();
        if (clientObjectManager == null)
            return;

        if (Pet == null)
        {
            PetIndex = (short)clientObjectManager->CreateBattleCharacter();
            if (PetIndex == -1)
                return;

            Pet = (BattleChara*)clientObjectManager->GetObjectByIndex((ushort)PetIndex);

            Debug($"Pet with index {PetIndex} spanwed ({(nint)Pet:X})");
        }

        if (Pet == null)
        {
            PetIndex = -1;
            return;
        }

        ((HaselCharacter*)Pet)->SetupBNpc(bNpcId);

        ApplyPetPosition();

        Pet->Character.GameObject.EnableDraw();
    }

    public void DespawnPet()
    {
        if (PetIndex < 0)
            return;

        ClientObjectManager.Instance()->DeleteObjectByIndex((ushort)PetIndex, 0);
        Debug($"Pet with index {PetIndex} despawned");
        PetIndex = -1;
        Pet = null;
    }

    // easier than setting position, lel
    public void ApplyPetPosition()
    {
        if (Pet == null)
            return;

        ((HaselCharacter*)Pet)->SetPosition(Config.PetPosition.X, Config.PetPosition.Y, Config.PetPosition.Z);
    }

    #endregion

    #region Login: Play emote in character selection

    private void UpdateVoiceCache()
    {
        var playerState = PlayerState.Instance();
        if (playerState == null || playerState->IsLoaded != 0x01)
            return;

        var contentId = playerState->ContentId;
        if (contentId == 0)
            return;

        var localPlayer = Service.ClientState.LocalPlayer;
        if (localPlayer == null)
            return;

        var character = (HaselCharacter*)localPlayer.Address;
        var voiceId = character->VoiceId;

        if (!Config.VoiceCache.ContainsKey(contentId))
            Config.VoiceCache.Add(contentId, voiceId);
        else
            Config.VoiceCache[contentId] = voiceId;

        Debug($"Updated voice id: {voiceId}");
    }

    private void UpdateUnlockedEmotes()
    {
        var uiState = UIState.Instance();
        if (uiState == null)
            return;

        var playerState = uiState->PlayerState;
        if (playerState.IsLoaded != 0x01)
            return;

        var contentId = playerState.ContentId;
        if (contentId == 0)
            return;

        var list = new List<uint>();

        foreach (var emote in Service.Data.GetExcelSheet<Emote>()!)
        {
            if (emote.RowId == 0 || emote.Icon == 0)
                continue;

            // Storm Salute
            if (emote.RowId == 55 && playerState.GrandCompany != 1)
                continue;

            // Serpent Salute
            if (emote.RowId == 56 && playerState.GrandCompany != 2)
                continue;

            // Flame Salute
            if (emote.RowId == 57 && playerState.GrandCompany != 3)
                continue;

            if (emote.UnlockLink != 0 && !uiState->IsUnlockLinkUnlockedOrQuestCompleted(emote.UnlockLink))
                continue;

            list.Add(emote.RowId);
        }

        if (!Config.UnlockedEmotes.ContainsKey(contentId))
            Config.UnlockedEmotes.Add(contentId, list);
        else
            Config.UnlockedEmotes[contentId] = list;

        Debug($"Updated unlocked emotes: {string.Join(", ", list)}");
    }

    private void SaveEmote(uint emoteId)
    {
        if (!Config.SelectedEmotes.ContainsKey(ActiveContentId))
            Config.SelectedEmotes.Add(ActiveContentId, emoteId);
        else
            Config.SelectedEmotes[ActiveContentId] = emoteId;
    }

    private void SetVoice()
    {
        if (CurrentEntry == null || CurrentEntry.Character == null)
            return;

        if (Config.VoiceCache.TryGetValue(ActiveContentId, out var voiceId))
            CurrentEntry.HaselCharacter->VoiceId = voiceId;
    }

    private void PlayEmote(uint emoteId)
    {
        if (!Config.EnableCharaSelectEmote)
            return;

        if (CurrentEntry == null || CurrentEntry.Character == null)
            return;

        if (emoteId == 0)
        {
            ResetEmoteMode();
            return;
        }

        var emote = Service.Data.GetExcelSheet<Emote>()!.GetRow(emoteId);
        if (emote == null)
        {
            ResetEmoteMode();
            return;
        }

        var intro = (ushort)emote.ActionTimeline[1].Row; // EmoteTimelineType.Intro
        var loop = (ushort)emote.ActionTimeline[0].Row; // EmoteTimelineType.Loop

        Debug($"Playing Emote {emoteId}: intro {intro}, loop {loop})");

        if (emote.EmoteMode.Row != 0)
        {
            Debug($"EmoteMode: {emote.EmoteMode.Row}");
            CurrentEntry.Character->SetMode((CharacterModes)emote.EmoteMode.Value!.ConditionMode, (byte)emote.EmoteMode.Row);
        }
        else
        {
            ResetEmoteMode();
        }

        if (intro != 0 && loop != 0)
        {
            CurrentEntry.HaselCharacter->ActionTimelineManager.PlayActionTimeline(intro, loop);
        }
        else if (loop != 0)
        {
            CurrentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(loop);
        }
        else if (intro != 0)
        {
            CurrentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(intro);
        }
        else
        {
            Debug("No intro or loop, resetting to idle pose (timeline 3)");
            CurrentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(3);
        }
    }

    private void ResetEmoteMode()
    {
        if (CurrentEntry == null || CurrentEntry.Character == null)
            return;

        Debug("Resetting Character Mode");
        CurrentEntry.Character->SetMode(CharacterModes.Normal, 0);
    }

    #endregion

    #region Login: Preload territory when queued

    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.OpenLoginWaitDialog))]
    public void OpenLoginWaitDialog(HaselAgentLobby* agent, int position)
    {
        OpenLoginWaitDialogHook.OriginalDisposeSafe(agent, position);

        if (CurrentEntry == null)
            return;

        ushort territoryId = CurrentEntry.TerritoryId switch
        {
               282  // Private Cottage - Mist
            or 283  // Private House - Mist
            or 284  // Private Mansion - Mist
            or 384  // Private Chambers - Mist
            => 339, // Mist

               342  // Private Cottage - The Lavender Beds
            or 343  // Private House - The Lavender Beds
            or 344  // Private Mansion - The Lavender Beds
            or 385  // Private Chambers - The Lavender Beds
            => 340, // The Lavender Beds


               345  // Private Cottage - The Goblet
            or 346  // Private House - The Goblet
            or 347  // Private Mansion - The Goblet
            or 386  // Private Chambers - The Goblet
            => 341, // The Goblet

            _ => CurrentEntry.TerritoryId
        };

        if (territoryId <= 0)
            return;

        var territoryType = Service.Data.GetExcelSheet<TerritoryType>()?.GetRow(territoryId);
        if (territoryType == null)
            return;

        var preloadManger = PreloadManger.Instance();
        if (preloadManger == null)
            return;

        Debug($"Preloading territory #{territoryId}: {territoryType.Bg.RawString}");
        preloadManger->PreloadTerritory(2, territoryType.Bg.RawString, 40, 0, territoryId);
    }

    #endregion

    #region Logout: Clear Tell History

    [VTableHook<UIModule>(110)]
    public void UIModule_vf110(UIModule* self, int a2, uint a3, nint a4)
    {
        if (a2 == 7) // logout
        {
            if (Config.ClearTellHistory)
                AcquaintanceModule.Instance()->ClearTellHistory(); // this is what /cleartellhistory calls
        }

        UIModule_vf110Hook.OriginalDisposeSafe(self, a2, a3, a4);
    }

    #endregion
}
