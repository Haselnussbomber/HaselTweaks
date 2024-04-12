using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Config;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Utils;
using HaselTweaks.Records;
using HaselTweaks.Structs;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public class EnhancedLoginLogoutConfiguration
{
    public bool ShowPets = false;
    public bool EnableCharaSelectEmote = false;
    public bool PreloadTerritory = true;
    public bool ClearTellHistory = false;

    public Vector3 PetPosition = new(-0.6f, 0f, 0f);

    public Dictionary<ulong, PetMirageSetting> PetMirageSettings = [];
    public Dictionary<ulong, uint> SelectedEmotes = [];

    public class PetMirageSetting
    {
        public uint CarbuncleType;
        public uint FairyType;
    }
}

[Tweak]
public unsafe partial class EnhancedLoginLogout : Tweak<EnhancedLoginLogoutConfiguration>
{
    private CharaSelectCharacter? _currentEntry = null;

    #region Core

    public override void Enable()
    {
        Service.GameConfig.Changed += GameConfig_Changed;
        UpdateCharacterSettings();
        PreloadEmotes();
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
    [AddressHook<AgentLobby>(nameof(AgentLobby.Addresses.UpdateCharaSelectDisplay))]
    public void UpdateCharaSelectDisplay(AgentLobby* agent, sbyte index, bool a2)
    {
        UpdateCharaSelectDisplayHook.OriginalDisposeSafe(agent, index, a2);

        if (index < 0)
        {
            CleanupCharaSelect();
            return;
        }

        if (index >= 100)
            index -= 100;

        var entry = agent->LobbyData.GetCharacterEntryByIndex(0, agent->WorldIndex, index);
        if (entry == null)
        {
            CleanupCharaSelect();
            return;
        }

        if (_currentEntry?.ContentId == entry->ContentId)
            return;

        var character = CharaSelectCharacterList.GetCurrentCharacter();
        if (character == null)
            return;

        _currentEntry = new(character, entry);

        character->Vfx.VoiceId = entry->CharacterInfo.VoiceId;

        SpawnPet();

        if (Config.SelectedEmotes.TryGetValue(ActiveContentId, out var emoteId))
            PlayEmote(emoteId);
    }

    [AddressHook<CharaSelectCharacterList>(nameof(CharaSelectCharacterList.Addresses.CleanupCharacters))]
    public void CleanupCharaSelectCharacters()
    {
        CleanupCharaSelect();
        CleanupCharaSelectCharactersHook.OriginalDisposeSafe();
    }

    #endregion

    #region Config

    private bool _isRecordingEmote;
    private readonly uint[] _changePoseEmoteIds = [91, 92, 93, 107, 108, 218, 219,];
    private List<uint>? _excludedEmotes;

    public override void DrawConfig()
    {
        var scale = ImGuiHelpers.GlobalScale;

        ImGuiUtils.DrawSection(t("EnhancedLoginLogout.Config.LoginOptions.Title"));
        // ShowPets
        if (ImGui.Checkbox(t("EnhancedLoginLogout.Config.ShowPets.Label"), ref Config.ShowPets))
        {
            if (!Config.ShowPets)
                DespawnPet();
            else
                SpawnPet();

            Service.GetService<Configuration>().Save();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, t("EnhancedLoginLogout.Config.ShowPets.Description"));
            ImGuiUtils.PushCursorY(3);

            if (ActiveContentId != 0)
            {
                if (!Config.PetMirageSettings.ContainsKey(ActiveContentId))
                    ImGuiUtils.TextUnformattedColored(Colors.Red, t("EnhancedLoginLogout.Config.ShowPets.Error.MissingPetMirageSettings"));
            }

            ImGuiUtils.PushCursorY(3);
        }

        // PetPosition
        var showPetsDisabled = Config.ShowPets ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.DragFloat3(t("EnhancedLoginLogout.Config.PetPosition.Label"), ref Config.PetPosition, 0.01f, -10f, 10f))
            {
                ApplyPetPosition();
                Service.GetService<Configuration>().Save();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##PetPositionReset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", "-0.6, 0, 0")))
            {
                Config.PetPosition = new(-0.6f, 0f, 0f);
                ApplyPetPosition();
                Service.GetService<Configuration>().Save();
            }
        }

        showPetsDisabled?.Dispose();

        // PlayEmote
        if (ImGui.Checkbox(t("EnhancedLoginLogout.Config.PlayEmote.Label"), ref Config.EnableCharaSelectEmote))
        {
            if (!Config.EnableCharaSelectEmote && _currentEntry != null && _currentEntry.Character != null)
            {
                ResetEmoteMode();
                _currentEntry.Character->ActionTimelineManager.Driver.PlayTimeline(3);
            }
            Service.GetService<Configuration>().Save();
        }

        var playEmoteDisabled = Config.EnableCharaSelectEmote ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, t("EnhancedLoginLogout.Config.PlayEmote.Description"));
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, t("EnhancedLoginLogout.Config.PlayEmote.Note"));
            ImGuiUtils.PushCursorY(3);

            if (Config.EnableCharaSelectEmote)
            {
                if (ActiveContentId != 0)
                {
                    ImGuiUtils.PushCursorY(3);
                    ImGui.TextUnformatted(t("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote"));
                    ImGui.SameLine();

                    if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
                    {
                        ImGui.TextUnformatted(t("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.None"));
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
                            ImGuiUtils.PushCursorY(-3);
                            Service.TextureManager.GetIcon(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon).Draw(24 * scale);
                            ImGui.SameLine();
                            ImGui.TextUnformatted(name);
                        }
                        else
                        {
                            ImGui.TextUnformatted(t("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.Unknown"));
                        }
                    }

                    if (Service.ClientState.IsLoggedIn)
                    {
                        ImGui.SameLine();

                        ImGuiUtils.PushCursorY(-3);

                        if (_isRecordingEmote)
                        {
                            if (ImGui.Button(t("EnhancedLoginLogout.Config.PlayEmote.StopRecordingButton.Label")))
                            {
                                _isRecordingEmote = false;
                            }
                        }
                        else
                        {
                            if (ImGui.Button(t("EnhancedLoginLogout.Config.PlayEmote.ChangeButton.Label")))
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

                            ImGuiUtils.PushCursorY(-3);
                            if (ImGui.Button(t("EnhancedLoginLogout.Config.PlayEmote.UnsetButton.Label")))
                            {
                                SaveEmote(0);
                            }
                        }

                        if (_isRecordingEmote)
                        {
                            ImGuiUtils.TextUnformattedColored(Colors.Gold, t("EnhancedLoginLogout.Config.PlayEmote.RecordingInfo"));
                            ImGuiUtils.PushCursorY(3);
                        }
                    }
                    else
                    {
                        ImGui.TextUnformatted(t("EnhancedLoginLogout.Config.PlayEmote.NotLoggedIn"));
                    }
                }
            }
        }

        playEmoteDisabled?.Dispose();

        // PreloadTerritory
        if (ImGui.Checkbox(t("EnhancedLoginLogout.Config.PreloadTerritory.Label"), ref Config.PreloadTerritory))
        {
            Service.GetService<Configuration>().Save();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, t("EnhancedLoginLogout.Config.PreloadTerritory.Description"));
            ImGuiUtils.PushCursorY(3);
        }

        ImGuiUtils.DrawSection(t("EnhancedLoginLogout.Config.LogoutOptions.Title"));
        // ClearTellHistory
        if (ImGui.Checkbox(t("EnhancedLoginLogout.Config.ClearTellHistory.Label"), ref Config.ClearTellHistory))
        {
            Service.GetService<Configuration>().Save();
        }
    }

    #endregion

    #region Login: Show pets in character selection

    private BattleChara* _pet = null;
    private ushort _petIndex = 0xFFFF;

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
            // Arcanist or Summoner
            26 or 27 when petMirageSettings.CarbuncleType is 0 => 13498u, // Carbuncle
            26 or 27 when petMirageSettings.CarbuncleType is 1 => 13501u, // Emerald Carbuncle (Blue)
            26 or 27 when petMirageSettings.CarbuncleType is 2 => 13500u, // Topaz Carbuncle (Yellow)
            26 or 27 when petMirageSettings.CarbuncleType is 3 => 13499u, // Ruby Carbuncle (Red)
            26 or 27 when petMirageSettings.CarbuncleType is 5 => 13502u, // Ifrit-Egi
            26 or 27 when petMirageSettings.CarbuncleType is 6 => 13503u, // Titan-Egi
            26 or 27 when petMirageSettings.CarbuncleType is 7 => 13504u, // Garuda-Egi

            // Scholar
            28 when petMirageSettings.FairyType is 0 => 1008u, // Eos
            28 when petMirageSettings.FairyType is 1 => 13501u, // Emerald Carbuncle (Blue)
            28 when petMirageSettings.FairyType is 2 => 13500u, // Topaz Carbuncle (Yellow)
            28 when petMirageSettings.FairyType is 3 => 13499u, // Ruby Carbuncle (Red)
            28 when petMirageSettings.FairyType is 4 => 13498u, // Carbuncle
            28 when petMirageSettings.FairyType is 8 => 1009u, // Selene
            _ => 0u
        };

        if (bNpcId == 0)
            return;

        var clientObjectManager = ClientObjectManager.Instance();
        if (clientObjectManager == null)
            return;

        if (_pet == null)
        {
            _petIndex = (ushort)clientObjectManager->CreateBattleCharacter();
            if (_petIndex == 0xFFFF)
                return;

            _pet = (BattleChara*)clientObjectManager->GetObjectByIndex(_petIndex);

            Debug($"Pet with index {_petIndex} spanwed ({(nint)_pet:X})");
        }

        if (_pet == null)
        {
            _petIndex = 0xFFFF;
            return;
        }

        _pet->Character.CharacterSetup.SetupBNpc(bNpcId);

        ApplyPetPosition();

        _pet->Character.GameObject.EnableDraw();
    }

    public void DespawnPet()
    {
        if (_petIndex == 0xFFFF)
            return;

        var clientObjectManager = ClientObjectManager.Instance();
        if (clientObjectManager != null && clientObjectManager->GetObjectByIndex(_petIndex) != null)
            clientObjectManager->DeleteObjectByIndex(_petIndex, 0);

        Debug($"Pet with index {_petIndex} despawned");
        _petIndex = 0xFFFF;
        _pet = null;
    }

    public void ApplyPetPosition()
    {
        if (_pet == null)
            return;

        _pet->Character.GameObject.SetPosition(Config.PetPosition.X, Config.PetPosition.Y, Config.PetPosition.Z);
    }

    #endregion

    #region Login: Play emote in character selection

    private void PreloadEmotes()
    {
        var processedActionTimelineIds = new HashSet<uint>();

        foreach (var emoteId in Config.SelectedEmotes.Values.ToHashSet())
        {
            var emote = GetRow<Emote>(emoteId);
            if (emote == null)
                continue;

            void PreloadActionTimeline(uint actionTimelineId)
            {
                if (actionTimelineId == 0 || processedActionTimelineIds.Contains(actionTimelineId))
                    return;

                var key = Statics.GetActionTimelineKey(actionTimelineId);
                Log("Preloading tmb {0} (Emote: {1}, ActionTimeline: {2})", MemoryHelper.ReadStringNullTerminated((nint)key), emoteId, actionTimelineId);
                HaselSchedulerActionTimelineManager.Instance()->PreloadActionTmbByKey(&key);

                processedActionTimelineIds.Add(actionTimelineId);
            }

            PreloadActionTimeline(emote.ActionTimeline[0].Row); // EmoteTimelineType.Loop
            PreloadActionTimeline(emote.ActionTimeline[1].Row); // EmoteTimelineType.Intro
        }
    }

    private void SaveEmote(uint emoteId)
    {
        Log($"Saving Emote #{emoteId} => {GetRow<Emote>(emoteId)?.Name ?? ""}");

        if (!Config.SelectedEmotes.ContainsKey(ActiveContentId))
            Config.SelectedEmotes.Add(ActiveContentId, emoteId);
        else
            Config.SelectedEmotes[ActiveContentId] = emoteId;

        Service.GetService<Configuration>().Save();
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

        // TODO: figure out how to prevent T-Pose

        if (intro != 0 && loop != 0)
        {
            ((HaselActionTimelineManager*)(nint)(&_currentEntry.Character->ActionTimelineManager))->PlayActionTimeline(intro, loop);
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
            _excludedEmotes = [/* Sit */ 50];

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

    [AddressHook<AgentLobby>(nameof(AgentLobby.Addresses.OpenLoginWaitDialog))]
    public void OpenLoginWaitDialog(AgentLobby* agent, int position)
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

    [VTableHook<UIModule>(111)]
    public void UIModule_vf111(UIModule* self, int a2, uint a3, nint a4)
    {
        if (a2 == 7) // logout
        {
            if (Config.ClearTellHistory)
                AcquaintanceModule.Instance()->ClearTellHistory(); // this is what /cleartellhistory calls
        }

        UIModule_vf111Hook.OriginalDisposeSafe(self, a2, a3, a4);
    }

    #endregion
}
