using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AdvancedEditOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    private float _lastTimestamp;

    public AdvancedEditOverlay(PortraitHelper tweak) : base(t("PortraitHelperWindows.AdvancedEditOverlay.Title"), tweak)
    {
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        var state = AgentBannerEditor->EditorState;
        var character = state->CharaView->Base.GetCharacter();
        if (character == null)
            return;

        var style = ImGui.GetStyle();
        ImGuiUtils.TextUnformattedColored(Colors.Gold, "Advanced Edit");
        ImGuiUtils.PushCursorY(-style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGuiUtils.PushCursorY(style.ItemSpacing.Y);

        using var table = ImRaii.Table("##Table", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 128 * ImGui.GetIO().FontGlobalScale);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushId("Camera"))
        {
            var yaw = state->CharaView->CameraYaw;
            var pitch = state->CharaView->CameraPitch;
            var distance = state->CharaView->CameraDistance;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.CameraYaw.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatYaw", ref yaw, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                state->CharaView->SetCameraYawAndPitch(
                    scale * (yaw - state->CharaView->CameraYaw),
                    scale * (pitch - state->CharaView->CameraPitch)
                );

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.CameraPitch.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatPitch", ref pitch, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                state->CharaView->SetCameraYawAndPitch(
                    scale * (yaw - state->CharaView->CameraYaw),
                    scale * (pitch - state->CharaView->CameraPitch)
                );

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.CameraDistance.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatDistance", ref distance, 0.001f, 0.5f, 2f))
            {
                var scale = 100f;

                state->CharaView->SetCameraDistance(
                    scale * (distance - state->CharaView->CameraDistance)
                );

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("CameraPosition"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.CameraXY.Label"));

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

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("ZoomRotation"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.ZoomRotation.Label"));

            ImGui.TableNextColumn();

            var itemWidth = (ImGui.GetColumnWidth() - style.ItemInnerSpacing.X) / 2f - 0.5f;
            ImGui.SetNextItemWidth(itemWidth);

            var zoom = (int)state->CharaView->CameraZoom;
            if (ImGui.DragInt($"##DragFloatZoom", ref zoom, 1, 0, 200))
            {
                state->CharaView->SetCameraZoom((byte)zoom);
                AddonBannerEditor->CameraZoomSlider->SetValue(zoom);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.SameLine(0, style.ItemInnerSpacing.X);
            ImGui.SetNextItemWidth(itemWidth + 0.5f);

            var rotation = (int)state->CharaView->ImageRotation;
            if (ImGui.DragInt($"##DragFloatRotation", ref rotation, 1, -90, 90))
            {
                state->CharaView->ImageRotation = (short)rotation;
                AddonBannerEditor->ImageRotation->SetValue(rotation);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("EyeDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.EyeDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var eyeDirection = new Vector2(
                character->FacialAnimationManager.EyeDirection.X,
                character->FacialAnimationManager.EyeDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref eyeDirection, 0.001f))
            {
                state->CharaView->SetEyeDirection(eyeDirection.X, eyeDirection.Y);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("HeadDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.HeadDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var headDirection = new Vector2(
                character->FacialAnimationManager.HeadDirection.X,
                character->FacialAnimationManager.HeadDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref headDirection, 0.001f))
            {
                state->CharaView->SetHeadDirection(headDirection.X, headDirection.Y);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("AnimationTimestamp"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.AnimationTimestamp.Label"));

            ImGui.TableNextColumn();

            var animation = character->ActionTimelineManager.BaseAnimation;
            var timeline = (animation != null && *animation != null) ? *animation : null;
            var timestamp = timeline == null ? _lastTimestamp : state->CharaView->GetAnimationTime();

            if (timeline == null)
                ImGui.BeginDisabled();

            void SetTimestamp(float timestamp)
            {
                if (timestamp < 0)
                    timestamp = 0;

                timeline->CurrentTimestamp = timestamp;
                _lastTimestamp = timestamp;
                state->CharaView->SetPoseTimed(character->ActionTimelineManager.BannerTimelineRowId, timestamp);
                state->CharaView->Base.ToggleAnimationPaused(true);
                AddonBannerEditor->PlayAnimationCheckbox->SetValue(false);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat($"##DragFloat", ref timestamp, 0.1f, 1f, "%.01f") && _lastTimestamp != timestamp)
            {
                SetTimestamp(timestamp);
            }

            if (timeline == null)
                ImGui.EndDisabled();
        }

        table?.Dispose();

        using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Grey))
        {
            ImGui.TextUnformatted(t("PortraitHelperWindows.AdvancedEditOverlay.Note.Label"));
            ImGuiHelpers.SafeTextWrapped("PortraitHelperWindows.AdvancedEditOverlay.Note.1");
            ImGuiHelpers.SafeTextWrapped("PortraitHelperWindows.AdvancedEditOverlay.Note.2");
        }
    }
}
