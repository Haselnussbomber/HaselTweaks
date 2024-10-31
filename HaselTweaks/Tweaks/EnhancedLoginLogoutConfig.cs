using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Config;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Tweaks;

public class EnhancedLoginLogoutConfiguration
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

public unsafe partial class EnhancedLoginLogout
{
    private EnhancedLoginLogoutConfiguration Config => PluginConfig.Tweaks.EnhancedLoginLogout;

    private bool _isRecordingEmote;
    private readonly uint[] _changePoseEmoteIds = [91, 92, 93, 107, 108, 218, 219,];
    private List<uint>? _excludedEmotes;

    public void OnConfigOpen() { }
    public void OnConfigClose() => _isRecordingEmote = false;

    public void OnConfigChange(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(Config.ShowPets):
                if (!Config.ShowPets)
                    DespawnPet();
                else
                    SpawnPet();
                break;

            case nameof(Config.EnableCharaSelectEmote):
                if (!Config.EnableCharaSelectEmote && CurrentEntry != null && CurrentEntry.Character != null)
                {
                    ResetEmoteMode();
                    CurrentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(3);
                }
                break;
        }
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader("EnhancedLoginLogout.Config.LoginOptions.Title");

        // SkipLogo
        ConfigGui.DrawBool("SkipLogo", ref Config.SkipLogo);

        // ShowPets
        ConfigGui.DrawBool("ShowPets", ref Config.ShowPets, drawAfterDescription: () =>
        {
            if (ActiveContentId != 0 && !Config.PetMirageSettings.ContainsKey(ActiveContentId))
            {
                TextService.DrawWrapped(Color.Red, "EnhancedLoginLogout.Config.ShowPets.Error.MissingPetMirageSettings");
            }

            // PetPosition
            using (ImRaii.Disabled(!Config.ShowPets))
            {
                ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemInnerSpacing.Y);

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
        });

        // PlayEmote
        ConfigGui.DrawBool("PlayEmote", ref Config.EnableCharaSelectEmote, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.EnableCharaSelectEmote);

            ImGuiUtils.PushCursorY(-ImGui.GetStyle().ItemInnerSpacing.Y);
            TextService.DrawWrapped(Color.Grey, "EnhancedLoginLogout.Config.PlayEmote.Note");
            ImGuiUtils.PushCursorY(3);

            if (!Config.EnableCharaSelectEmote || ActiveContentId == 0)
                return;

            ImGuiUtils.PushCursorY(3);
            TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote");
            ImGui.SameLine();

            if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
            {
                TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.None");
            }
            else
            {
                ExcelService.TryGetRow<Emote>(90, out var defaultIdlePoseEmote); // first "Change Pose"
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
                    TextureService.DrawIcon(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon, 24 * ImGuiHelpers.GlobalScale);
                    ImGui.SameLine();
                    ImGui.TextUnformatted(name);
                }
                else
                {
                    TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.Unknown");
                }
            }

            if (!ClientState.IsLoggedIn)
            {
                TextService.Draw("EnhancedLoginLogout.Config.PlayEmote.NotLoggedIn");
                return;
            }

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
                TextService.Draw(Color.Gold, "EnhancedLoginLogout.Config.PlayEmote.RecordingInfo");
                ImGuiUtils.PushCursorY(3);
            }
        });


        // PreloadTerritory
        ConfigGui.DrawBool("PreloadTerritory", ref Config.PreloadTerritory);

        ConfigGui.DrawConfigurationHeader("EnhancedLoginLogout.Config.LogoutOptions.Title");

        // ClearTellHistory
        ConfigGui.DrawBool("ClearTellHistory", ref Config.ClearTellHistory, noFixSpaceAfter: true);
    }
}
