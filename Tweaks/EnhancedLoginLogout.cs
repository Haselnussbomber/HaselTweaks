using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Config;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Enums;
using HaselTweaks.Records;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using HaselAgentLobby = HaselTweaks.Structs.AgentLobby;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Enhanced Login/Logout",
    Description: "Enhances login and logout.",
    Flags: TweakFlags.HasCustomConfig | TweakFlags.NoCustomConfigHeader
)]
public unsafe partial class EnhancedLoginLogout : Tweak
{
    private CharaSelectCharacter? _currentEntry = null;

    #region Core

    public override void Enable()
    {
        Service.GameConfig.Changed += GameConfig_Changed;
        UpdateCharacterSettings();
    }

    public override void Disable()
    {
        Service.GameConfig.Changed -= GameConfig_Changed;
        CleanupCharaSelect();
    }

    public override void OnLogin() => UpdateCharacterSettings();
    public override void OnLogout() => _isRecordingEmote = false;
    public override void OnConfigWindowClose() => _isRecordingEmote = false;

    private void GameConfig_Changed(object? sender, ConfigChangeEvent change)
    {
        if (change.Option is UiConfigOption.PetMirageTypeCarbuncleSupport or UiConfigOption.PetMirageTypeFairy)
            UpdatePetMirageSettings();
    }

    private void UpdateCharacterSettings()
    {
        UpdatePetMirageSettings();
    }

    private void CleanupCharaSelect()
    {
        DespawnPet();
        _currentEntry = null;
    }

    private ulong ActiveContentId => _currentEntry?.ContentId ?? Service.ClientState.LocalContentId;

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

        var entry = agent->Unk40.GetCharacterEntryByIndex(0, agent->Unk10F2, index);
        if (entry == null)
        {
            CleanupCharaSelect();
            return;
        }

        if (_currentEntry?.ContentId == entry->ContentId)
            return;

        var character = HaselAgentLobby.GetCurrentCharaSelectCharacter();
        if (character == null)
            return;

        _currentEntry = new(character, entry);

        character->VoiceId = entry->CharacterInfo.VoiceId;

        SpawnPet();

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

    private bool _isRecordingEmote;
    private readonly uint[] _changePoseEmoteIds = new uint[] { 91, 92, 93, 107, 108, 218, 219, };
    private List<uint>? _excludedEmotes;

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

        public class PetMirageSetting
        {
            public uint CarbuncleType;
            public uint FairyType;
        }
    }

    public override void DrawCustomConfig()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var verticalTextPadding = 3;

        ImGuiUtils.DrawSection("Login Options");
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
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, "Displays a carbuncle (Arcanist/Summoner) or a fairy (Scholar) next to your character.");

            if (ActiveContentId != 0)
            {
                if (!Config.PetMirageSettings.ContainsKey(ActiveContentId))
                    ImGui.TextColored(Colors.Red, "Pet glamour settings for this character not cached! Please log in.");
            }

            ImGuiUtils.PushCursorY(3);
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
            if (ImGuiUtils.IconButton($"##HaselTweaks_Config_{InternalName}_Position_Reset", FontAwesomeIcon.Undo, "Reset to Default: -0.6, 0, 0"))
            {
                Config.PetPosition = new(-0.6f, 0f, 0f);
                ApplyPetPosition();
            }
        }

        showPetsDisabled?.Dispose();

        // PlayEmote
        if (ImGui.Checkbox($"Play emote in character selection##HaselTweaks_Config_{InternalName}_PlayEmote", ref Config.EnableCharaSelectEmote))
        {
            if (!Config.EnableCharaSelectEmote && _currentEntry != null && _currentEntry.Character != null)
            {
                ResetEmoteMode();
                _currentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(3);
            }
        }

        var playEmoteDisabled = Config.EnableCharaSelectEmote ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, "Have your character greet you with an emote!");
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, "Note: Emote settings are per character and not all emotes are supported (e.g. sitting or underwater emotes). What is supported, however, are alternative standing idle poses.");
            ImGuiUtils.PushCursorY(3);

            if (Config.EnableCharaSelectEmote)
            {
                if (ActiveContentId != 0)
                {
                    ImGuiUtils.PushCursorY(verticalTextPadding);
                    ImGui.Text("Current Emote:");
                    ImGui.SameLine();

                    if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
                    {
                        ImGui.Text("None");
                    }
                    else
                    {
                        var defaultIdlePoseEmote = GetRow<Emote>(90)!; // first "Change Pose"
                        var changePoseIndex = 1;

                        var entry = GetSheet<Emote>()
                            .Select(row => (
                                IsChangePose: _changePoseEmoteIds.Contains(row.RowId),
                                Name: _changePoseEmoteIds.Contains(row.RowId) ? $"{defaultIdlePoseEmote.Name.ToDalamudString()} ({changePoseIndex++})" : $"{row.Name.ToDalamudString()}",
                                Emote: row
                            ) as (bool IsChangePose, string Name, Emote Emote)?)
                            .FirstOrDefault(entry => entry != null && entry.Value.Emote.RowId == selectedEmoteId, null);

                        if (entry.HasValue)
                        {
                            var (isChangePose, name, emote) = entry.Value;
                            ImGuiUtils.PushCursorY(-verticalTextPadding);
                            Service.TextureCache.GetIcon(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon).Draw(24 * scale);
                            ImGui.SameLine();
                            ImGui.Text(name);
                        }
                        else
                        {
                            ImGui.Text("Unknown");
                        }
                    }

                    if (Service.ClientState.IsLoggedIn)
                    {
                        ImGui.SameLine();

                        ImGuiUtils.PushCursorY(-verticalTextPadding);

                        if (_isRecordingEmote)
                        {
                            if (ImGui.Button("Stop Recording"))
                            {
                                _isRecordingEmote = false;
                            }
                        }
                        else
                        {
                            if (ImGui.Button("Change"))
                            {
                                _isRecordingEmote = true;

                                var agentEmote = AgentModule.Instance()->GetAgentByInternalId(AgentId.Emote);
                                if (!agentEmote->IsAgentActive())
                                {
                                    agentEmote->Show();
                                }
                            }
                        }

                        if (selectedEmoteId != 0)
                        {
                            ImGui.SameLine();

                            ImGuiUtils.PushCursorY(-verticalTextPadding);
                            if (ImGui.Button("Unset"))
                            {
                                SaveEmote(0);
                            }
                        }

                        if (_isRecordingEmote)
                        {
                            ImGui.TextColored(Colors.Gold, "Perform an emote now to set it for this character!");
                            ImGuiUtils.PushCursorY(verticalTextPadding);
                        }
                    }
                    else
                    {
                        ImGui.Text("Please log in to set an emote.");
                    }
                }
            }
        }

        playEmoteDisabled?.Dispose();

        // PreloadTerritory
        ImGui.Checkbox($"Preload territory when queued##HaselTweaks_Config_{InternalName}_PreloadTerritory", ref Config.PreloadTerritory);
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, "When it puts you in queue, it will preload the territory textures in the background, just as it does as when you start teleporting.");
            ImGuiUtils.PushCursorY(verticalTextPadding);
        }

        ImGuiUtils.DrawSection("Logout Options");
        // ClearTellHistory
        ImGui.Checkbox($"Clear tell history on logout##HaselTweaks_Config_{InternalName}_ClearTellHistory", ref Config.ClearTellHistory);
    }

    #endregion

    #region Login: Show pets in character selection

    private BattleChara* _pet = null;
    private short _petIndex = -1;

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
            Service.GameConfig.TryGet(UiConfigOption.PetMirageTypeCarbuncleSupport, out petMirageSettings.CarbuncleType);
        }
        catch (Exception e)
        {
            Error(e, "Error reading pet glamours");
        }

        try
        {
            Service.GameConfig.TryGet(UiConfigOption.PetMirageTypeFairy, out petMirageSettings.FairyType);
        }
        catch (Exception e)
        {
            Error(e, "Error reading pet glamours");
        }

        Debug($"Updated PetMirageSettings: CarbuncleType {petMirageSettings.CarbuncleType}, FairyType {petMirageSettings.FairyType}");
    }

    public void SpawnPet()
    {
        if (!Config.ShowPets || _currentEntry == null)
            return;

        if (!(_currentEntry.ClassJobId is 26 or 27 or 28)) // Arcanist, Summoner, Scholar (Machinist: 31)
        {
            DespawnPet();
            return;
        }

        if (_pet != null)
            return;

        if (!Config.PetMirageSettings.TryGetValue(ActiveContentId, out var petMirageSettings))
            return;

        var bNpcId = _currentEntry.ClassJobId switch
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

        if (_pet == null)
        {
            _petIndex = (short)clientObjectManager->CreateBattleCharacter();
            if (_petIndex == -1)
                return;

            _pet = (BattleChara*)clientObjectManager->GetObjectByIndex((ushort)_petIndex);

            Debug($"Pet with index {_petIndex} spanwed ({(nint)_pet:X})");
        }

        if (_pet == null)
        {
            _petIndex = -1;
            return;
        }

        ((HaselCharacter*)_pet)->SetupBNpc(bNpcId);

        ApplyPetPosition();

        _pet->Character.GameObject.EnableDraw();
    }

    public void DespawnPet()
    {
        if (_petIndex < 0)
            return;

        ClientObjectManager.Instance()->DeleteObjectByIndex((ushort)_petIndex, 0);
        Debug($"Pet with index {_petIndex} despawned");
        _petIndex = -1;
        _pet = null;
    }

    // easier than setting position, lel
    public void ApplyPetPosition()
    {
        if (_pet == null)
            return;

        ((HaselCharacter*)_pet)->SetPosition(Config.PetPosition.X, Config.PetPosition.Y, Config.PetPosition.Z);
    }

    #endregion

    #region Login: Play emote in character selection

    private void SaveEmote(uint emoteId)
    {
        Log($"Saving Emote #{emoteId} => {GetRow<Emote>(emoteId)?.Name ?? ""}");

        if (!Config.SelectedEmotes.ContainsKey(ActiveContentId))
            Config.SelectedEmotes.Add(ActiveContentId, emoteId);
        else
            Config.SelectedEmotes[ActiveContentId] = emoteId;
    }

    private void PlayEmote(uint emoteId)
    {
        if (!Config.EnableCharaSelectEmote)
            return;

        if (_currentEntry == null || _currentEntry.Character == null)
            return;

        if (emoteId == 0)
        {
            ResetEmoteMode();
            return;
        }

        var emote = GetRow<Emote>(emoteId);
        if (emote == null)
        {
            ResetEmoteMode();
            return;
        }

        var intro = (ushort)emote.ActionTimeline[1].Row; // EmoteTimelineType.Intro
        var loop = (ushort)emote.ActionTimeline[0].Row; // EmoteTimelineType.Loop

        Debug($"Playing Emote {emoteId}: intro {intro}, loop {loop}");

        if (emote.EmoteMode.Row != 0)
        {
            Debug($"EmoteMode: {emote.EmoteMode.Row}");
            _currentEntry.Character->SetMode((Character.CharacterModes)emote.EmoteMode.Value!.ConditionMode, (byte)emote.EmoteMode.Row);
        }
        else
        {
            ResetEmoteMode();
        }

        if (intro != 0 && loop != 0)
        {
            _currentEntry.HaselCharacter->ActionTimelineManager.PlayActionTimeline(intro, loop);
        }
        else if (loop != 0)
        {
            _currentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(loop);
        }
        else if (intro != 0)
        {
            _currentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(intro);
        }
        else
        {
            Debug("No intro or loop, resetting to idle pose (timeline 3)");
            _currentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(3);
        }
    }

    private void ResetEmoteMode()
    {
        if (_currentEntry == null || _currentEntry.Character == null)
            return;

        Debug("Resetting Character Mode");
        _currentEntry.Character->SetMode(Character.CharacterModes.Normal, 0);
    }

    [AddressHook<EmoteManager>(nameof(EmoteManager.Addresses.ExecuteEmote))]
    public bool ExecuteEmote(EmoteManager* handler, ushort emoteId, nint targetData)
    {
        var changePoseIndexBefore = PlayerState.Instance()->SelectedPoses[0];
        var success = ExecuteEmoteHook.OriginalDisposeSafe(handler, emoteId, targetData);

        if (_excludedEmotes == null)
        {
            _excludedEmotes = new() { /* Sit */ 50, };

            foreach (var emote in GetSheet<Emote>())
            {
                if (emote.RowId == 90) // allow Change Pose
                    continue;

                if (emote.RowId != 0 && emote.Icon != 0 && !(emote.ActionTimeline[0].Row == 0 && emote.ActionTimeline[1].Row == 0))
                    continue;

                _excludedEmotes.Add(emote.RowId);
            }
        }

        if (_isRecordingEmote && success && Service.ClientState.IsLoggedIn && !_excludedEmotes.Contains(emoteId))
        {
            // special case for Change Pose
            if (emoteId == 90)
            {
                var changePoseIndex = PlayerState.Instance()->SelectedPoses[0];
                if (changePoseIndexBefore != changePoseIndex) // only process if standing pose was changed
                {
                    if (changePoseIndex >= 0 && changePoseIndex < _changePoseEmoteIds.Length)
                    {
                        SaveEmote(_changePoseEmoteIds[changePoseIndex]);
                    }
                    else
                    {
                        SaveEmote(emoteId);
                    }
                }
            }
            else
            {
                SaveEmote(emoteId);
            }
        }

        return success;
    }

    #endregion

    #region Login: Preload territory when queued

    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.OpenLoginWaitDialog))]
    public void OpenLoginWaitDialog(HaselAgentLobby* agent, int position)
    {
        OpenLoginWaitDialogHook.OriginalDisposeSafe(agent, position);

        if (_currentEntry == null)
            return;

        ushort territoryId = _currentEntry.TerritoryType switch
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

            _ => _currentEntry.TerritoryType
        };

        if (territoryId <= 0)
            return;

        var territoryType = GetRow<TerritoryType>(territoryId);
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
