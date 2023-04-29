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

    private float lastTimestamp;

    public AdvancedEditOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper AdvancedEdit", tweak)
    {
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        var state = AgentBannerEditor->EditorState;
        var gameObject = state->CharaView->Base.GetGameObject();
        if (gameObject == null)
            return;

        var style = ImGui.GetStyle();
        ImGuiUtils.TextUnformattedColored(ImGuiUtils.ColorGold, "Advanced Edit");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);

        using var table = ImRaii.Table("##Table", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 128);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushId("Camera"))
        {
            var yaw = state->CharaView->CameraYaw;
            var pitch = state->CharaView->CameraPitch;
            var distance = state->CharaView->CameraDistance;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Camera Yaw");

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
            ImGui.TextUnformatted("Camera Pitch");

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
            ImGui.TextUnformatted("Camera Distance");

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
            ImGui.TextUnformatted("Camera X / Y");

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
            ImGui.TextUnformatted("Zoom / Rotation");

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
            ImGui.TextUnformatted("Eye Direction");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var eyeDirection = new Vector2(
                gameObject->FacialAnimationManager.EyeDirection.X,
                gameObject->FacialAnimationManager.EyeDirection.Y
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
            ImGui.TextUnformatted("Head Direction");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var headDirection = new Vector2(
                gameObject->FacialAnimationManager.HeadDirection.X,
                gameObject->FacialAnimationManager.HeadDirection.Y
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
            ImGui.TextUnformatted("Animation Timestamp");

            ImGui.TableNextColumn();

            var animation = gameObject->ActionTimelineManager.BaseAnimation;
            var timeline = (animation != null && *animation != null) ? *animation : null;
            var timestamp = timeline == null ? lastTimestamp : state->CharaView->GetAnimationTime();

            if (timeline == null)
                ImGui.BeginDisabled();

            void SetTimestamp(float timestamp)
            {
                if (timestamp < 0)
                    timestamp = 0;

                timeline->CurrentTimestamp = timestamp;
                lastTimestamp = timestamp;
                state->CharaView->SetPoseTimed(gameObject->ActionTimelineManager.BannerTimelineRowId, timestamp);
                state->CharaView->Base.ToggleAnimationPaused(true);
                AddonBannerEditor->PlayAnimationCheckbox->SetValue(false);

                if (!AgentBannerEditor->EditorState->HasDataChanged)
                    AgentBannerEditor->EditorState->SetHasChanged(true);
            }

            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat($"##DragFloat", ref timestamp, 0.1f, 1f, "%.01f") && lastTimestamp != timestamp)
            {
                SetTimestamp(timestamp);
            }

            if (timeline == null)
                ImGui.EndDisabled();
        }

        table?.Dispose();

        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiUtils.ColorGrey)))
        {
            ImGui.TextUnformatted("Please note:");
            ImGuiHelpers.SafeTextWrapped("The game may verify the values on setting them and/or saving the portrait. If possible, it will automatically adjust them so that they are within a valid range. If not, it will throw an error and you have to fix the values yourself. In any case, if the game adjusts the values, the adjusted values will not be reflected here unless you reopen the window.");
            ImGuiHelpers.SafeTextWrapped("Also, setting the Animation Timestamp will restart the animation at the given timestamp, hence the flickering. If the Animation Timestamp is too high, the game will start the next loop, resetting the timestamp to 0.");
        }
    }
}
