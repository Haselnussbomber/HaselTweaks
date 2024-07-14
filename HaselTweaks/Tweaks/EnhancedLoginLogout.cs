using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Records;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public unsafe partial class EnhancedLoginLogout(
    PluginConfig PluginConfig,
    TextService TextService,
    ILogger<EnhancedLoginLogout> Logger,
    IGameInteropProvider GameInteropProvider,
    IGameConfig GameConfig,
    IClientState ClientState,
    IAddonLifecycle AddonLifecycle,
    TextureService TextureService,
    ExcelService ExcelService,
    PlayerService PlayerService,
    ConfigGui ConfigGui)
    : IConfigurableTweak
{
    public string InternalName => nameof(EnhancedLoginLogout);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private CharaSelectCharacter? CurrentEntry = null;

    #region Core

    private Hook<AgentLobby.Delegates.UpdateCharaSelectDisplay>? UpdateCharaSelectDisplayHook;
    private Hook<CharaSelectCharacterList.Delegates.CleanupCharacters>? CleanupCharactersHook;
    private Hook<EmoteManager.Delegates.ExecuteEmote>? ExecuteEmoteHook;
    private Hook<AgentLobby.Delegates.OpenLoginWaitDialog>? OpenLoginWaitDialogHook;

    public void OnInitialize()
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
    }

    public void OnEnable()
    {
        GameConfig.Changed += OnGameConfigChanged;
        ClientState.Login += OnLogin;
        PlayerService.LoggingOut += OnLogout;

        UpdateCharacterSettings();
        PreloadEmotes();

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Logo", OnLogoPostSetup);

        UpdateCharaSelectDisplayHook?.Enable();
        CleanupCharactersHook?.Enable();
        ExecuteEmoteHook?.Enable();
        OpenLoginWaitDialogHook?.Enable();
    }

    public void OnDisable()
    {
        GameConfig.Changed -= OnGameConfigChanged;
        ClientState.Login -= OnLogin;
        PlayerService.LoggingOut -= OnLogout;

        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Logo", OnLogoPostSetup);

        CleanupCharaSelect();

        UpdateCharaSelectDisplayHook?.Disable();
        CleanupCharactersHook?.Disable();
        ExecuteEmoteHook?.Disable();
        OpenLoginWaitDialogHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        UpdateCharaSelectDisplayHook?.Dispose();
        CleanupCharactersHook?.Dispose();
        ExecuteEmoteHook?.Dispose();
        OpenLoginWaitDialogHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnLogin()
    {
        UpdateCharacterSettings();
    }

    private void OnLogout()
    {
        _isRecordingEmote = false;

        if (Config.ClearTellHistory)
            AcquaintanceModule.Instance()->ClearTellHistory();
    }

    private void OnGameConfigChanged(object? sender, ConfigChangeEvent change)
    {
        if (change.Option is UiConfigOption.PetMirageTypeCarbuncleSupport or UiConfigOption.PetMirageTypeFairy && AgentLobby.Instance()->IsLoggedIn)
            UpdatePetMirageSettings();
    }

    private void UpdateCharacterSettings()
    {
        UpdatePetMirageSettings();
    }

    private void CleanupCharaSelect()
    {
        DespawnPet();
        CurrentEntry = null;
    }

    private ulong ActiveContentId => CurrentEntry?.ContentId ?? ClientState.LocalContentId;

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

        if (CurrentEntry?.ContentId == entry->ContentId)
            return;

        var character = CharaSelectCharacterList.GetCurrentCharacter();
        if (character == null)
            return;

        CurrentEntry = new(character, entry);

        character->Vfx.VoiceId = entry->ClientSelectData.VoiceId;

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
        if (!Config.ShowPets || CurrentEntry == null)
            return;

        if (!(CurrentEntry.ClassJobId is 26 or 27 or 28)) // Arcanist, Summoner, Scholar (Machinist: 31)
        {
            DespawnPet();
            return;
        }

        if (_pet != null)
            return;

        if (!Config.PetMirageSettings.TryGetValue(ActiveContentId, out var petMirageSettings))
            return;

        var bNpcId = CurrentEntry.ClassJobId switch
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

        if (CurrentEntry == null || CurrentEntry.Character == null)
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
            CurrentEntry.Character->SetMode((CharacterModes)emote.EmoteMode.Value!.ConditionMode, (byte)emote.EmoteMode.Row);
        }
        else
        {
            ResetEmoteMode();
        }

        // TODO: figure out how to prevent T-Pose

        if (intro != 0 && loop != 0)
        {
            ((HaselTimelineContainer*)(nint)(&CurrentEntry.Character->Timeline))->PlayActionTimeline(intro, loop);
        }
        else if (loop != 0)
        {
            CurrentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(loop);
        }
        else if (intro != 0)
        {
            CurrentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(intro);
        }
        else
        {
            Logger.LogDebug("No intro or loop, resetting to idle pose (timeline 3)");
            CurrentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(3);
        }
    }

    private void ResetEmoteMode()
    {
        if (CurrentEntry == null || CurrentEntry.Character == null)
            return;

        Logger.LogDebug("Resetting Character Mode");
        CurrentEntry.Character->SetMode(CharacterModes.Normal, 0);
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

        if (CurrentEntry == null)
            return;

        ushort territoryTypeId = CurrentEntry.TerritoryType switch
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

            _ => CurrentEntry.TerritoryType
        };

        if (territoryTypeId <= 0)
            return;

        var territoryType = ExcelService.GetRow<TerritoryType>(territoryTypeId);
        if (territoryType == null)
            return;

        Logger.LogDebug("Preloading territory #{territoryId}: {bg}", territoryTypeId, territoryType.Bg.RawString);

        var layoutWorld = LayoutWorld.Instance();
        layoutWorld->UnloadPrefetchLayout();
        layoutWorld->LoadPrefetchLayout(
            2,
            territoryType.Bg.RawData,
            /* LayerEntryType.PopRange */ 40,
            0,
            territoryTypeId,
            GameMain.Instance(),
            0);
    }

    #endregion
}
