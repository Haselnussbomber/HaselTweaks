using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using HaselAgentLobby = HaselTweaks.Structs.AgentLobby;
using HaselCharacter = HaselTweaks.Structs.Character;

namespace HaselTweaks.Tweaks;

public unsafe partial class EnhancedLoginLogout : Tweak
{
    public override string Name => "Enhanced Login/Logout";
    public static Configuration Config => Plugin.Config.Tweaks.EnhancedLoginLogout;

    public class Configuration
    {
        public bool ShowPets = true;
        public Vector3 PetPosition = new(-0.6f, 0f, 0f);
        public Dictionary<ulong, PetMirageSetting> PetMirageSettings = new();
        public bool PreloadTerritory = true;
        public bool ClearTellHistory = false;

        public class PetMirageSetting
        {
            public uint CarbuncleType;
            public uint FairyType;
        }
    }

    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        ImGui.TextUnformatted("Login options:");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

        // ShowPets
        if (ImGui.Checkbox($"Show pets in character selection##HaselTweaks_Config_{InternalName}_ShowPets", ref Config.ShowPets))
        {
            OnShowPetsChanged();
        }
        using (ImGuiUtils.ConfigIndent())
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiUtils.ColorGrey, "Shows Arcanist/Scholar/Summoner pets next to your character. In order to apply the pet glamor settings, you must have logged in at least once.");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
        }

        // PetPosition
        var showPetsDisabled = Config.ShowPets ? null : ImRaii.Disabled();

        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.DragFloat3($"Position##HaselTweaks_Config_{InternalName}_Position", ref Config.PetPosition, 0.01f, -10f, 10f))
            {
                UpdatePetPosition();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton($"##HaselTweaks_Config_{InternalName}_Position_Reset", FontAwesomeIcon.Undo, "Reset to Default: -1, 0, 0"))
            {
                Config.PetPosition = new(-0.6f, 0f, 0f);
                UpdatePetPosition();
            }
        }

        showPetsDisabled?.Dispose();
        // TODO: reset button

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

    public override void Enable() => UpdatePetMirageSettings();
    public override void OnLogin() => UpdatePetMirageSettings();
    public override void Disable() => Cleanup();

    public void OnShowPetsChanged()
    {
        if (!Config.ShowPets)
            Cleanup();
    }

    #region Login

    private BattleChara* pet = null;
    private short petIndex = -1;
    private ulong charaSelectCharacterContentId = 0;
    private ushort charaSelectCharacterTerritoryId = 0;

    private void UpdatePetMirageSettings()
    {
        var contentId = PlayerState.Instance()->ContentId;
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

        Debug($"PetMirageSettings updated, CarbuncleType: {petMirageSettings.CarbuncleType}, FairyType: {petMirageSettings.FairyType}");
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

    private void Cleanup()
    {
        DeleteCharaSelectPet();
        charaSelectCharacterContentId = 0;
        charaSelectCharacterTerritoryId = 0;
    }

    [Signature("E8 ?? ?? ?? ?? 48 8B 48 08 49 89 8C 24")]
    private readonly GetCharacterEntryByIndexDelegate GetCharacterEntryByIndex = null!;
    private delegate CharaSelectCharacterEntry* GetCharacterEntryByIndexDelegate(nint a1, int a2, int a3, int index);

    // called every frame
    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.UpdateCharaSelectDisplay))]
    public void UpdateCharaSelectDisplay(HaselAgentLobby* agent, sbyte index, bool a2)
    {
        UpdateCharaSelectDisplayHook.OriginalDisposeSafe(agent, index, a2);

        if (!Config.ShowPets)
            return;

        if (index < 0)
        {
            Cleanup();
            return;
        }

        if (index >= 100)
            index -= 100;

        var entry = GetCharacterEntryByIndex((nint)agent + 0x40, 0, agent->Unk10F2, index); // what a headache
        if (entry == null)
        {
            Cleanup();
            return;
        }

        if (charaSelectCharacterContentId == entry->ContentId)
            return;

        charaSelectCharacterContentId = entry->ContentId;
        charaSelectCharacterTerritoryId = entry->ParsedData.TerritoryId;

        if (!(entry->ParsedData.CurrentClassJobId is 26 or 27 or 28)) // Arcanist, Summoner, Scholar (Machinist: 31)
        {
            DeleteCharaSelectPet();
            return;
        }

        if (pet != null)
            return;

        if (!Config.PetMirageSettings.TryGetValue(charaSelectCharacterContentId, out var petMirageSettings))
            return;

        var bNpcId = entry->ParsedData.CurrentClassJobId switch
        {
            26 => 13498u + petMirageSettings.CarbuncleType, // Arcanist
            27 => 13498u + petMirageSettings.CarbuncleType, // Summoner
            28 => 1008u + petMirageSettings.FairyType, // Scholar
            _ => 0u
        };

        if (bNpcId == 0)
            return;

        var character = CharaSelect.GetCurrentCharacter();
        if (character == null)
            return;

        var clientObjectManager = ClientObjectManager.Instance();
        if (clientObjectManager == null)
            return;

        if (pet == null)
        {
            petIndex = (short)clientObjectManager->CreateBattleCharacter();
            if (petIndex == -1)
                return;

            pet = (BattleChara*)clientObjectManager->GetObjectByIndex((ushort)petIndex);

            Log($"pet with index {petIndex} @ {(nint)pet:X}");
        }

        if (pet == null)
        {
            petIndex = -1;
            return;
        }

        ((HaselCharacter*)pet)->SetupBNpc(bNpcId);

        UpdatePetPosition();

        pet->Character.GameObject.EnableDraw();
    }

    public void UpdatePetPosition()
    {
        if (pet == null)
            return;

        pet->Character.GameObject.SetDrawOffset(Config.PetPosition.X, Config.PetPosition.Y, Config.PetPosition.Z);
    }

    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.CleanupCharaSelectCharacters))]
    public void CleanupCharaSelectCharacters()
    {
        Cleanup();
        CleanupCharaSelectCharactersHook.OriginalDisposeSafe();
    }

    public void DeleteCharaSelectPet()
    {
        if (petIndex < 0)
            return;

        ClientObjectManager.Instance()->DeleteObjectByIndex((ushort)petIndex, 0);
        petIndex = -1;
        pet = null;
    }

    [AddressHook<HaselAgentLobby>(nameof(HaselAgentLobby.Addresses.OpenLoginWaitDialog))]
    public void OpenLoginWaitDialog(HaselAgentLobby* agent, int position)
    {
        OpenLoginWaitDialogHook.OriginalDisposeSafe(agent, position);

        ushort territoryId = charaSelectCharacterTerritoryId switch
        {
            282 => 339, // Private Cottage - Mist                 => Mist
            283 => 339, // Private House - Mist                   => Mist
            284 => 339, // Private Mansion - Mist                 => Mist
            342 => 340, // Private Cottage - The Lavender Beds    => The Lavender Beds
            343 => 340, // Private House - The Lavender Beds      => The Lavender Beds
            344 => 340, // Private Mansion - The Lavender Beds    => The Lavender Beds
            345 => 341, // Private Cottage - The Goblet           => The Goblet
            346 => 341, // Private House - The Goblet             => The Goblet
            347 => 341, // Private Mansion - The Goblet           => The Goblet
            384 => 339, // Private Chambers - Mist                => Mist
            385 => 340, // Private Chambers - The Lavender Beds   => The Lavender Beds
            386 => 341, // Private Chambers - The Goblet          => The Goblet
            _ => charaSelectCharacterTerritoryId
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

    #region Logout

    [VTableHook<UIModule>(110)]
    public void UIModule_vf110(nint a1, int a2, uint a3, nint a4)
    {
        if (a2 == 7)
        {
            if (Config.ClearTellHistory)
                AcquaintanceModule.Instance()->ClearTellHistory(); // this is what /cleartellhistory calls
        }

        UIModule_vf110Hook.OriginalDisposeSafe(a1, a2, a3, a4);
    }

    #endregion
}
