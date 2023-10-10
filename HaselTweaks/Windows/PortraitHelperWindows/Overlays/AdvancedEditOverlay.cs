using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedEditOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    private float _timestamp;
    private float _timelineLength;

    public AdvancedEditOverlay() : base(t("PortraitHelperWindows.AdvancedEditOverlay.Title"))
    {
    }

    public override void OnOpen()
    {
        var state = GetAgent<AgentBannerEditor>()->EditorState;
        var character = state->CharaView->Base.GetCharacter();
        if (character == null)
            return;

        var timelinePtr = ((ActionTimelineManager*)(nint)(&character->ActionTimelineManager))->BaseAnimation;
        if (timelinePtr == null)
            return;

        var timeline = *timelinePtr;
        if (timeline == null)
            return;

        _timestamp = (float)Math.Round(GetAgent<AgentBannerEditor>()->EditorState->CharaView->GetAnimationTime(), 1);
        _timelineLength = timeline->AnimationLength;
    }

    public override void Update()
    {
        base.Update();

        var charaViewTimestamp = (float)Math.Round(GetAgent<AgentBannerEditor>()->EditorState->CharaView->GetAnimationTime(), 1);
        if (charaViewTimestamp != 0 && _timestamp != charaViewTimestamp)
        {
            _timestamp = charaViewTimestamp;
        }
    }

    public override void Draw()
    {
        base.Draw();

        var agentBannerEditor = GetAgent<AgentBannerEditor>();
        var addonBannerEditor = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        var state = agentBannerEditor->EditorState;
        var character = state->CharaView->Base.GetCharacter();
        if (character == null)
            return;

        if (!IsWindow)
        {
            ImGuiUtils.DrawSection(
                t("PortraitHelperWindows.AdvancedEditOverlay.Title.Inner"),
                PushDown: false,
                RespectUiTheme: true,
                UIColor: 2);
        }

        using var table = ImRaii.Table("##Table", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushId("Camera"))
        {
            var yaw = state->CharaView->CameraYaw;
            var pitch = state->CharaView->CameraPitch;
            var distance = state->CharaView->CameraDistance;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraYaw.Label"));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(t("PortraitHelperWindows.Setting.CameraYaw.Tooltip"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatYaw", ref yaw, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                state->CharaView->SetCameraYawAndPitch(
                    scale * (yaw - state->CharaView->CameraYaw),
                    scale * (pitch - state->CharaView->CameraPitch)
                );

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraPitch.Label"));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(t("PortraitHelperWindows.Setting.CameraPitch.Tooltip"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatPitch", ref pitch, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                state->CharaView->SetCameraYawAndPitch(
                    scale * (yaw - state->CharaView->CameraYaw),
                    scale * (pitch - state->CharaView->CameraPitch)
                );

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraDistance.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatDistance", ref distance, 0.001f, 0.5f, 2f))
            {
                var scale = 100f;

                state->CharaView->SetCameraDistance(
                    scale * (distance - state->CharaView->CameraDistance)
                );

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("CameraPosition"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.CameraTarget.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var pos = new Vector2(
                state->CharaView->CameraTarget.X,
                state->CharaView->CameraTarget.Y
            );
            if (ImGui.DragFloat2($"##DragFloat2", ref pos, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 1000f;
                state->CharaView->SetCameraXAndY(
                    scale * (pos.X - state->CharaView->CameraTarget.X),
                    scale * (pos.Y - state->CharaView->CameraTarget.Y)
                );

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("ZoomRotation"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.ZoomRotation.Label"));

            ImGui.TableNextColumn();

            var itemWidth = (ImGui.GetColumnWidth() - ImGui.GetStyle().ItemInnerSpacing.X) / 2f - 0.5f;
            ImGui.SetNextItemWidth(itemWidth);

            var zoom = (int)state->CharaView->CameraZoom;
            if (ImGui.DragInt($"##DragFloatZoom", ref zoom, 1, 0, 200))
            {
                state->CharaView->SetCameraZoom((byte)zoom);
                addonBannerEditor->CameraZoomSlider->SetValue(zoom);

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetNextItemWidth(itemWidth + 0.5f);

            var rotation = (int)state->CharaView->ImageRotation;
            if (ImGui.DragInt($"##DragFloatRotation", ref rotation, 1, -90, 90))
            {
                state->CharaView->ImageRotation = (short)rotation;
                addonBannerEditor->ImageRotation->SetValue(rotation);

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("EyeDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.EyeDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var cb0 = (CharacterCB0*)((nint)character + 0xCB0);
            var eyeDirection = new Vector2(
                cb0->EyeDirection.X,
                cb0->EyeDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref eyeDirection, 0.001f))
            {
                state->CharaView->SetEyeDirection(eyeDirection.X, eyeDirection.Y);

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("HeadDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.HeadDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var cb0 = (CharacterCB0*)((nint)character + 0xCB0);
            var headDirection = new Vector2(
                cb0->HeadDirection.X,
                cb0->HeadDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref headDirection, 0.001f))
            {
                state->CharaView->SetHeadDirection(headDirection.X, headDirection.Y);

                if (!agentBannerEditor->EditorState->HasDataChanged)
                    agentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("AnimationTimestamp"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.Setting.AnimationTimestamp.Label"));

            ImGui.TableNextColumn();

            var region = ImGui.GetContentRegionAvail();
            ImGui.SetNextItemWidth(region.X - (ImGui.GetStyle().ItemInnerSpacing.X + ImGuiUtils.GetIconSize(FontAwesomeIcon.InfoCircle).X));

            var timestampBefore = _timestamp;
            var timestampAfter = timestampBefore;
            if (ImGui.DragFloat($"##DragFloat", ref timestampAfter, 0.1f, 0f, _timelineLength, "%.1f") && timestampAfter != timestampBefore)
            {
                var clampedValue = Math.Clamp(timestampAfter, 0, _timelineLength);
                _timestamp = (float)Math.Round(clampedValue, 1);

                var timelinePtr = ((ActionTimelineManager*)(nint)(&character->ActionTimelineManager))->BaseAnimation;
                if (timelinePtr != null)
                {
                    var timeline = *timelinePtr;
                    if (timeline != null)
                    {
                        timeline->CurrentTimestamp = clampedValue;
                        state->CharaView->SetPoseTimed(character->ActionTimelineManager.BannerTimelineRowId, clampedValue);
                        state->CharaView->Base.ToggleAnimationPlayback(true);
                        addonBannerEditor->PlayAnimationCheckbox->SetValue(false);

                        if (!agentBannerEditor->EditorState->HasDataChanged)
                            agentBannerEditor->EditorState->SetHasChanged(true);
                    }
                }
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGuiUtils.Icon(FontAwesomeIcon.InfoCircle);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetNextWindowSize(new Vector2(300, -1));
                using (ImRaii.Tooltip())
                {
                    ImGui.TextWrapped(t("PortraitHelperWindows.Setting.AnimationTimestamp.Info"));
                }
            }
        }

        table?.Dispose();

        using (ImRaii.PushColor(ImGuiCol.Text, (uint)(Colors.IsLightTheme && !IsWindow ? Colors.GetUIColor(3) : Colors.Grey)))
        {
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.Note.Label"));
            ImGuiHelpers.SafeTextWrapped(t("PortraitHelperWindows.AdvancedEditOverlay.Note.Text"));
        }
    }
}
