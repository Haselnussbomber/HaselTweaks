using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Textures;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Records;
using HaselTweaks.Structs;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public sealed class EnhancedLoginLogoutConfiguration
{
    public bool SkipLogo = true;
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

public sealed unsafe class EnhancedLoginLogout(
    ILogger<EnhancedLoginLogout> Logger,
    IGameInteropProvider GameInteropProvider,
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    IGameConfig GameConfig,
    IClientState ClientState,
    IAddonLifecycle AddonLifecycle,
    TextureManager TextureManager,
    ExcelService ExcelService,
    TextService TextService)
    : Tweak<EnhancedLoginLogoutConfiguration>(PluginConfig, TranslationManager)
{
    private CharaSelectCharacter? _currentEntry = null;

    #region Core

    private Hook<AgentLobby.Delegates.UpdateCharaSelectDisplay>? UpdateCharaSelectDisplayHook;
    private Hook<CharaSelectCharacterList.Delegates.CleanupCharacters>? CleanupCharactersHook;
    private Hook<EmoteManager.Delegates.ExecuteEmote>? ExecuteEmoteHook;
    private Hook<AgentLobby.Delegates.OpenLoginWaitDialog>? OpenLoginWaitDialogHook;
    private Hook<UIModule.Delegates.HandlePacket>? HandleUIModulePacketHook;

    public override void OnInitialize()
    {
        UpdateCharaSelectDisplayHook = GameInteropProvider.HookFromAddress<AgentLobby.Delegates.UpdateCharaSelectDisplay>(
            AgentLobby.MemberFunctionPointers.UpdateCharaSelectDisplay,
            UpdateCharaSelectDisplayDetour);

        CleanupCharactersHook = GameInteropProvider.HookFromAddress<CharaSelectCharacterList.Delegates.CleanupCharacters>(
            CharaSelectCharacterList.MemberFunctionPointers.CleanupCharacters,
            CleanupCharactersDetour);

        ExecuteEmoteHook = GameInteropProvider.HookFromAddress<EmoteManager.Delegates.ExecuteEmote>(
            EmoteManager.MemberFunctionPointers.ExecuteEmote,
            ExecuteEmoteDetour);

        OpenLoginWaitDialogHook = GameInteropProvider.HookFromAddress<AgentLobby.Delegates.OpenLoginWaitDialog>(
            AgentLobby.MemberFunctionPointers.OpenLoginWaitDialog,
            OpenLoginWaitDialogDetour);

        HandleUIModulePacketHook = GameInteropProvider.HookFromAddress<UIModule.Delegates.HandlePacket>(
            UIModule.StaticVirtualTablePointer->HandlePacket,
            HandleUIModulePacketDetour);
    }

    public override void OnEnable()
    {
        GameConfig.Changed += GameConfig_Changed;

        UpdateCharacterSettings();
        PreloadEmotes();

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Logo", OnLogoPostSetup);

        UpdateCharaSelectDisplayHook?.Enable();
        CleanupCharactersHook?.Enable();
        ExecuteEmoteHook?.Enable();
        OpenLoginWaitDialogHook?.Enable();
        HandleUIModulePacketHook?.Enable();
    }

    public override void OnDisable()
    {
        GameConfig.Changed -= GameConfig_Changed;

        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Logo", OnLogoPostSetup);

        CleanupCharaSelect();

        UpdateCharaSelectDisplayHook?.Disable();
        CleanupCharactersHook?.Disable();
        ExecuteEmoteHook?.Disable();
        OpenLoginWaitDialogHook?.Disable();
        HandleUIModulePacketHook?.Disable();
    }

    private void OnLogin() => UpdateCharacterSettings();
    private void OnLogout() => _isRecordingEmote = false;
    public override void OnConfigClose() => _isRecordingEmote = false;

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

    private ulong ActiveContentId => _currentEntry?.ContentId ?? ClientState.LocalContentId;

    // called every frame
    private void UpdateCharaSelectDisplayDetour(AgentLobby* agent, sbyte index, bool a2)
    {
        UpdateCharaSelectDisplayHook!.Original(agent, index, a2);

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

    private void CleanupCharactersDetour()
    {
        CleanupCharaSelect();
        CleanupCharactersHook!.Original();
    }

    #endregion

    #region Config

    private bool _isRecordingEmote;
    private readonly uint[] _changePoseEmoteIds = [91, 92, 93, 107, 108, 218, 219,];
    private List<uint>? _excludedEmotes;

    public override void DrawConfig()
    {
        var scale = ImGuiHelpers.GlobalScale;

        ImGuiUtils.DrawSection(TextService.Translate("EnhancedLoginLogout.Config.LoginOptions.Title"));
        // SkipLogo
        if (ImGui.Checkbox(TextService.Translate("EnhancedLoginLogout.Config.SkipLogo.Label"), ref Config.SkipLogo))
        {
            PluginConfig.Save();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, TextService.Translate("EnhancedLoginLogout.Config.SkipLogo.Description"));
            ImGuiUtils.PushCursorY(3);
        }

        // ShowPets
        if (ImGui.Checkbox(TextService.Translate("EnhancedLoginLogout.Config.ShowPets.Label"), ref Config.ShowPets))
        {
            if (!Config.ShowPets)
                DespawnPet();
            else
                SpawnPet();

            PluginConfig.Save();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            TextService.DrawWrapped(Colors.Grey, "EnhancedLoginLogout.Config.ShowPets.Description");
            ImGuiUtils.PushCursorY(3);

            if (ActiveContentId != 0)
            {
                if (!Config.PetMirageSettings.ContainsKey(ActiveContentId))
                    TextService.Draw(Colors.Red, "EnhancedLoginLogout.Config.ShowPets.Error.MissingPetMirageSettings");
            }

            ImGuiUtils.PushCursorY(3);
        }

        // PetPosition
        var showPetsDisabled = Config.ShowPets ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.DragFloat3(TextService.Translate("EnhancedLoginLogout.Config.PetPosition.Label"), ref Config.PetPosition, 0.01f, -10f, 10f))
            {
                ApplyPetPosition();
                PluginConfig.Save();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##PetPositionReset", FontAwesomeIcon.Undo, TextService.Translate("HaselTweaks.Config.ResetToDefault", "-0.6, 0, 0")))
            {
                Config.PetPosition = new(-0.6f, 0f, 0f);
                ApplyPetPosition();
                PluginConfig.Save();
            }
        }

        showPetsDisabled?.Dispose();

        // PlayEmote
        if (ImGui.Checkbox(TextService.Translate("EnhancedLoginLogout.Config.PlayEmote.Label"), ref Config.EnableCharaSelectEmote))
        {
            if (!Config.EnableCharaSelectEmote && _currentEntry != null && _currentEntry.Character != null)
            {
                ResetEmoteMode();
                _currentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(3);
            }
            PluginConfig.Save();
        }

        var playEmoteDisabled = Config.EnableCharaSelectEmote ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            TextService.DrawWrapped(Colors.Grey, "EnhancedLoginLogout.Config.PlayEmote.Description");
            TextService.DrawWrapped(Colors.Grey, "EnhancedLoginLogout.Config.PlayEmote.Note");
            ImGuiUtils.PushCursorY(3);

            if (Config.EnableCharaSelectEmote)
            {
                if (ActiveContentId != 0)
                {
                    ImGuiUtils.PushCursorY(3);
                    TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote");
                    ImGui.SameLine();

                    if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
                    {
                        TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.None");
                    }
                    else
                    {
                        var defaultIdlePoseEmote = ExcelService.GetRow<Emote>(90)!; // first "Change Pose"
                        var changePoseIndex = 1;

                        var entry = ExcelService.GetSheet<Emote>()
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
                            TextureManager.GetIcon(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon).Draw(24 * scale);
                            ImGui.SameLine();
                            ImGui.TextUnformatted(name);
                        }
                        else
                        {
                            TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.Unknown");
                        }
                    }

                    if (ClientState.IsLoggedIn)
                    {
                        ImGui.SameLine();

                        ImGuiUtils.PushCursorY(-3);

                        if (_isRecordingEmote)
                        {
                            if (ImGui.Button(TextService.Translate("EnhancedLoginLogout.Config.PlayEmote.StopRecordingButton.Label")))
                            {
                                _isRecordingEmote = false;
                            }
                        }
                        else
                        {
                            if (ImGui.Button(TextService.Translate("EnhancedLoginLogout.Config.PlayEmote.ChangeButton.Label")))
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
                            if (ImGui.Button(TextService.Translate("EnhancedLoginLogout.Config.PlayEmote.UnsetButton.Label")))
                            {
                                SaveEmote(0);
                            }
                        }

                        if (_isRecordingEmote)
                        {
                            TextService.Draw(Colors.Gold, "EnhancedLoginLogout.Config.PlayEmote.RecordingInfo");
                            ImGuiUtils.PushCursorY(3);
                        }
                    }
                    else
                    {
                        TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.NotLoggedIn");
                    }
                }
            }
        }

        playEmoteDisabled?.Dispose();

        // PreloadTerritory
        if (ImGui.Checkbox(TextService.Translate("EnhancedLoginLogout.Config.PreloadTerritory.Label"), ref Config.PreloadTerritory))
        {
            PluginConfig.Save();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiUtils.PushCursorY(-3);
            TextService.DrawWrapped(Colors.Grey, "EnhancedLoginLogout.Config.PreloadTerritory.Description");
            ImGuiUtils.PushCursorY(3);
        }

        ImGuiUtils.DrawSection(TextService.Translate("EnhancedLoginLogout.Config.LogoutOptions.Title"));
        // ClearTellHistory
        if (ImGui.Checkbox(TextService.Translate("EnhancedLoginLogout.Config.ClearTellHistory.Label"), ref Config.ClearTellHistory))
        {
            PluginConfig.Save();
        }
    }

    #endregion

    #region Login: Skip Logo

    private void OnLogoPostSetup(AddonEvent type, AddonArgs args)
    {
        if (Config.SkipLogo)
        {
            var addon = (AtkUnitBase*)args.Addon;
            var value = new AtkValue
            {
                Type = ValueType.Int,
                Int = 0
            };
            Logger.LogInformation("Sending change stage to title screen event...");
            addon->FireCallback(1, &value, true);
            addon->Hide(false, false, 1);
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
            GameConfig.TryGet(UiConfigOption.PetMirageTypeCarbuncleSupport, out petMirageSettings.CarbuncleType);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error reading pet glamours");
        }

        try
        {
            GameConfig.TryGet(UiConfigOption.PetMirageTypeFairy, out petMirageSettings.FairyType);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error reading pet glamours");
        }

        Logger.LogDebug("Updated PetMirageSettings: CarbuncleType {carbuncleType}, FairyType {fairyType}", petMirageSettings.CarbuncleType, petMirageSettings.FairyType);
    }

    private void SpawnPet()
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

            Logger.LogDebug("Pet with index {_petIndex} spanwed ({petAddr:X})", _petIndex, (nint)_pet);
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

    private void DespawnPet()
    {
        if (_petIndex == 0xFFFF)
            return;

        var clientObjectManager = ClientObjectManager.Instance();
        if (clientObjectManager != null && clientObjectManager->GetObjectByIndex(_petIndex) != null)
            clientObjectManager->DeleteObjectByIndex(_petIndex, 0);

        Logger.LogDebug("Pet with index {_petIndex} despawned", _petIndex);
        _petIndex = 0xFFFF;
        _pet = null;
    }

    private void ApplyPetPosition()
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
            var emote = ExcelService.GetRow<Emote>(emoteId);
            if (emote == null)
                continue;

            void PreloadActionTimeline(uint actionTimelineId)
            {
                if (actionTimelineId == 0 || processedActionTimelineIds.Contains(actionTimelineId))
                    return;

                var index = 0u;
                foreach (var row in ExcelService.GetSheet<ActionTimeline>())
                {
                    if (row.RowId == actionTimelineId)
                        break;

                    index++;
                }

                var key = ExcelService.GetRow<ActionTimeline>(actionTimelineId)?.Key.RawString;
                if (string.IsNullOrEmpty(key))
                    return;

                Logger.LogInformation("Preloading tmb {key} (Emote: {emoteId}, ActionTimeline: {actionTimelineId})", key, emoteId, actionTimelineId);

                fixed (byte* keyPtr = Encoding.UTF8.GetBytes(key + "\0"))
                {
                    var preloadInfo = new ActionTimelineManager.PreloadActionTmbInfo
                    {
                        Key = keyPtr,
                        Index = index
                    };

                    ActionTimelineManager.Instance()->PreloadActionTmb(&preloadInfo);
                }

                processedActionTimelineIds.Add(actionTimelineId);
            }

            PreloadActionTimeline(emote.ActionTimeline[0].Row); // EmoteTimelineType.Loop
            PreloadActionTimeline(emote.ActionTimeline[1].Row); // EmoteTimelineType.Intro
        }
    }

    private void SaveEmote(uint emoteId)
    {
        Logger.LogInformation("Saving Emote #{emoteId} => {name}", emoteId, ExcelService.GetRow<Emote>(emoteId)?.Name ?? "");

        if (!Config.SelectedEmotes.TryAdd(ActiveContentId, emoteId))
            Config.SelectedEmotes[ActiveContentId] = emoteId;

        PluginConfig.Save();
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

        var emote = ExcelService.GetRow<Emote>(emoteId);
        if (emote == null)
        {
            ResetEmoteMode();
            return;
        }

        var intro = (ushort)emote.ActionTimeline[1].Row; // EmoteTimelineType.Intro
        var loop = (ushort)emote.ActionTimeline[0].Row; // EmoteTimelineType.Loop

        Logger.LogDebug("Playing Emote {emoteId}: intro {intro}, loop {loop}", emoteId, intro, loop);

        if (emote.EmoteMode.Row != 0)
        {
            Logger.LogDebug("EmoteMode: {rowId}", emote.EmoteMode.Row);
            _currentEntry.Character->SetMode((Character.CharacterModes)emote.EmoteMode.Value!.ConditionMode, (byte)emote.EmoteMode.Row);
        }
        else
        {
            ResetEmoteMode();
        }

        // TODO: figure out how to prevent T-Pose

        if (intro != 0 && loop != 0)
        {
            ((HaselTimelineContainer*)(nint)(&_currentEntry.Character->Timeline))->PlayActionTimeline(intro, loop);
        }
        else if (loop != 0)
        {
            _currentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(loop);
        }
        else if (intro != 0)
        {
            _currentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(intro);
        }
        else
        {
            Logger.LogDebug("No intro or loop, resetting to idle pose (timeline 3)");
            _currentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(3);
        }
    }

    private void ResetEmoteMode()
    {
        if (_currentEntry == null || _currentEntry.Character == null)
            return;

        Logger.LogDebug("Resetting Character Mode");
        _currentEntry.Character->SetMode(Character.CharacterModes.Normal, 0);
    }

    private bool ExecuteEmoteDetour(EmoteManager* handler, ushort emoteId, nint targetData)
    {
        var changePoseIndexBefore = PlayerState.Instance()->SelectedPoses[0];
        var success = ExecuteEmoteHook!.Original(handler, emoteId, targetData);

        if (_excludedEmotes == null)
        {
            _excludedEmotes = [/* Sit */ 50];

            foreach (var emote in ExcelService.GetSheet<Emote>())
            {
                if (emote.RowId == 90) // allow Change Pose
                    continue;

                if (emote.RowId != 0 && emote.Icon != 0 && !(emote.ActionTimeline[0].Row == 0 && emote.ActionTimeline[1].Row == 0))
                    continue;

                _excludedEmotes.Add(emote.RowId);
            }
        }

        if (_isRecordingEmote && success && ClientState.IsLoggedIn && !_excludedEmotes.Contains(emoteId))
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

    private void OpenLoginWaitDialogDetour(AgentLobby* agent, int position)
    {
        OpenLoginWaitDialogHook!.Original(agent, position);

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

        var territoryType = ExcelService.GetRow<TerritoryType>(territoryId);
        if (territoryType == null)
            return;

        var preloadManger = PreloadManger.Instance();
        if (preloadManger == null)
            return;

        Logger.LogDebug("Preloading territory #{territoryId}: {bg}", territoryId, territoryType.Bg.RawString);
        fixed (byte* ptr = territoryType.Bg.RawData)
        {
            preloadManger->PreloadTerritory(2, (nint)ptr, 40, 0, territoryId);
        }
    }

    #endregion

    #region Logout: Clear Tell History

    private void HandleUIModulePacketDetour(UIModule* uiModule, UIModulePacketType type, uint uintParam, void* packet)
    {
        if (type == UIModulePacketType.Logout && Config.ClearTellHistory)
            AcquaintanceModule.Instance()->ClearTellHistory();

        HandleUIModulePacketHook!.Original(uiModule, type, uintParam, packet);
    }

    #endregion
}
