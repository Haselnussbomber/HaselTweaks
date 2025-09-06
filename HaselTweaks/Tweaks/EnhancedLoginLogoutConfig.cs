using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
    private EnhancedLoginLogoutConfiguration Config => _pluginConfig.Tweaks.EnhancedLoginLogout;

    private bool _isRecordingEmote;
    private readonly uint[] _changePoseEmoteIds = [91, 92, 93, 107, 108, 218, 219,];
    private List<uint>? _excludedEmotes;

    public override void OnConfigClose()
    {
        _isRecordingEmote = false;
    }

    public override void OnConfigChange(string fieldName)
    {
        if (Status != TweakStatus.Enabled)
            return;

        switch (fieldName)
        {
            case nameof(Config.ShowPets):
                if (!Config.ShowPets)
                    DespawnPet();
                else
                    SpawnPet();
                break;

            case nameof(Config.EnableCharaSelectEmote):
                if (!Config.EnableCharaSelectEmote && _currentEntry != null && _currentEntry.Character != null)
                {
                    ResetEmoteMode();
                    _currentEntry.Character->Timeline.TimelineSequencer.PlayTimeline(3);
                }
                break;
        }
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader("EnhancedLoginLogout.Config.LoginOptions.Title");

        // SkipLogo
        _configGui.DrawBool("SkipLogo", ref Config.SkipLogo);

        // ShowPets
        _configGui.DrawBool("ShowPets", ref Config.ShowPets, drawAfterDescription: () =>
        {
            if (ActiveContentId != 0 && !Config.PetMirageSettings.ContainsKey(ActiveContentId))
            {
                ImGui.TextColoredWrapped(Color.Red, _textService.Translate("EnhancedLoginLogout.Config.ShowPets.Error.MissingPetMirageSettings"));
            }

            // PetPosition
            using (ImRaii.Disabled(!Config.ShowPets))
            {
                ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemInnerSpacing.Y);

                if (ImGui.DragFloat3(_textService.Translate("EnhancedLoginLogout.Config.PetPosition.Label"), ref Config.PetPosition, 0.01f, -10f, 10f))
                {
                    ApplyPetPosition();
                    _pluginConfig.Save();
                }
                ImGui.SameLine();
                if (ImGuiUtils.IconButton("##PetPositionReset", FontAwesomeIcon.Undo, _textService.Translate("HaselTweaks.Config.ResetToDefault", "-0.6, 0, 0")))
                {
                    Config.PetPosition = new(-0.6f, 0f, 0f);
                    ApplyPetPosition();
                    _pluginConfig.Save();
                }
            }
        });

        // PlayEmote
        _configGui.DrawBool("PlayEmote", ref Config.EnableCharaSelectEmote, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.EnableCharaSelectEmote);

            ImGuiUtils.PushCursorY(-ImGui.GetStyle().ItemInnerSpacing.Y);
            ImGui.TextColoredWrapped(Color.Grey, _textService.Translate("EnhancedLoginLogout.Config.PlayEmote.Note"));
            ImGuiUtils.PushCursorY(3);

            if (!Config.EnableCharaSelectEmote || ActiveContentId == 0)
                return;

            ImGuiUtils.PushCursorY(3);
            ImGui.Text(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote"));
            ImGui.SameLine();

            if (!Config.SelectedEmotes.TryGetValue(ActiveContentId, out var selectedEmoteId) || selectedEmoteId == 0)
            {
                ImGui.Text(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.None"));
            }
            else
            {
                _excelService.TryGetRow<Emote>(90, out var defaultIdlePoseEmote); // first "Change Pose"
                var changePoseIndex = 1;

                var entry = _excelService.GetSheet<Emote>()
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
                    _textureProvider.DrawIcon((uint)(isChangePose ? defaultIdlePoseEmote.Icon : emote.Icon), 24 * ImGuiHelpers.GlobalScale);
                    ImGui.SameLine();
                    ImGui.Text(name);
                }
                else
                {
                    ImGui.Text(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.CurrentEmote.Unknown"));
                }
            }

            if (!_clientState.IsLoggedIn)
            {
                ImGui.Text(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.NotLoggedIn"));
                return;
            }

            ImGui.SameLine();

            ImGuiUtils.PushCursorY(-3);

            if (_isRecordingEmote)
            {
                if (ImGui.Button(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.StopRecordingButton.Label")))
                {
                    _isRecordingEmote = false;
                }
            }
            else
            {
                if (ImGui.Button(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.ChangeButton.Label")))
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
                if (ImGui.Button(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.UnsetButton.Label")))
                {
                    SaveEmote(0);
                }
            }

            if (_isRecordingEmote)
            {
                using (Color.Gold.Push(ImGuiCol.Text))
                    ImGui.Text(_textService.Translate("EnhancedLoginLogout.Config.PlayEmote.RecordingInfo"));
                ImGuiUtils.PushCursorY(3);
            }
        });


        // PreloadTerritory
        if (_configGui.DrawBool("PreloadTerritory", ref Config.PreloadTerritory))
        {
            if (Config.PreloadTerritory)
                _openLoginWaitDialogHook?.Enable();
            else
                _openLoginWaitDialogHook?.Disable();
        }

        _configGui.DrawConfigurationHeader("EnhancedLoginLogout.Config.LogoutOptions.Title");

        // ClearTellHistory
        _configGui.DrawBool("ClearTellHistory", ref Config.ClearTellHistory, noFixSpaceAfter: true);
    }
}
