using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using HaselTweaks.Structs;
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
        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.NoMove;
        base.IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        var state = AgentBannerEditor->EditorState;
        var gameObject = state->CharaView->Base.GetGameObject();
        if (gameObject == null)
            return;

        var style = ImGui.GetStyle();
        ImGui.TextColored(ImGuiUtils.ColorGold, "Advanced Edit");
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + style.ItemSpacing.Y);

        using var table = ImRaii.Table("##Table", 2);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 128);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

        using (ImRaii.PushId("CameraPosition"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text("Camera Position");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var vec3 = new Vector3(
                state->CharaView->CameraPosition.X,
                state->CharaView->CameraPosition.Y,
                state->CharaView->CameraPosition.Z
            );
            if (ImGui.DragFloat3($"##DragFloat3", ref vec3, 0.001f))
            {
                var halfVecPosition = (HalfVector4*)IMemorySpace.GetDefaultSpace()->Malloc<HalfVector4>();
                var halfVecTarget = (HalfVector4*)IMemorySpace.GetDefaultSpace()->Malloc<HalfVector4>();

                halfVecPosition->X = (Half)vec3.X;
                halfVecPosition->Y = (Half)vec3.Y;
                halfVecPosition->Z = (Half)vec3.Z;
                halfVecPosition->W = (Half)state->CharaView->CameraPosition.W;

                halfVecTarget->X = (Half)state->CharaView->CameraTarget.X;
                halfVecTarget->Y = (Half)state->CharaView->CameraTarget.Y;
                halfVecTarget->Z = (Half)state->CharaView->CameraTarget.Z;
                halfVecTarget->W = (Half)state->CharaView->CameraTarget.W;

                state->CharaView->SetCameraPosition(halfVecPosition, halfVecTarget);

                IMemorySpace.Free(halfVecPosition);
                IMemorySpace.Free(halfVecTarget);

                AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("CameraTarget"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text("Camera Target");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var vec3 = new Vector3(
                state->CharaView->CameraTarget.X,
                state->CharaView->CameraTarget.Y,
                state->CharaView->CameraTarget.Z
            );
            if (ImGui.DragFloat3($"##DragFloat3", ref vec3, 0.001f))
            {
                var halfVecPosition = (HalfVector4*)IMemorySpace.GetDefaultSpace()->Malloc<HalfVector4>();
                var halfVecTarget = (HalfVector4*)IMemorySpace.GetDefaultSpace()->Malloc<HalfVector4>();

                halfVecPosition->X = (Half)state->CharaView->CameraPosition.X;
                halfVecPosition->Y = (Half)state->CharaView->CameraPosition.Y;
                halfVecPosition->Z = (Half)state->CharaView->CameraPosition.Z;
                halfVecPosition->W = (Half)state->CharaView->CameraPosition.W;

                halfVecTarget->X = (Half)vec3.X;
                halfVecTarget->Y = (Half)vec3.Y;
                halfVecTarget->Z = (Half)vec3.Z;
                halfVecTarget->W = (Half)state->CharaView->CameraTarget.W;

                state->CharaView->SetCameraPosition(halfVecPosition, halfVecTarget);

                IMemorySpace.Free(halfVecPosition);
                IMemorySpace.Free(halfVecTarget);

                AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("EyeDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text("Eye Direction");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var eyeDirection = new Vector2(
                gameObject->FacialAnimationManager.EyeDirection.X,
                gameObject->FacialAnimationManager.EyeDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref eyeDirection, 0.001f))
            {
                state->CharaView->SetEyeDirection(eyeDirection.X, eyeDirection.Y);

                AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("HeadDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text("Head Direction");

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var headDirection = new Vector2(
                gameObject->FacialAnimationManager.HeadDirection.X,
                gameObject->FacialAnimationManager.HeadDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref headDirection, 0.001f))
            {
                state->CharaView->SetHeadDirection(headDirection.X, headDirection.Y);

                AgentBannerEditor->EditorState->SetHasChanged(true);
            }
        }

        using (ImRaii.PushId("AnimationTimestamp"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text("Animation Timestamp");

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
            ImGui.Text("Please note:");
            ImGui.TextWrapped("The game may verify the values on setting them and/or saving the portrait. If possible, it will automatically adjust them so that they are within a valid range. If not, it will throw an error and you have to fix the values yourself. In any case, if the game adjusts the values, the adjusted values will not be reflected here unless you reopen the window.");
            ImGui.TextWrapped("Also, setting the Animation Timestamp will restart the animation at the given timestamp, hence the flickering. If the Animation Timestamp is too high, the game will start the next loop, resetting the timestamp to 0.");
        }
    }
}
