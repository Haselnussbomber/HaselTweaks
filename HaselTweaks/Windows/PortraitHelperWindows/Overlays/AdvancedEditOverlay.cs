using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Havok.Animation.Animation;
using HaselTweaks.Enums.PortraitHelper;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterTransient, AutoConstruct]
public unsafe partial class AdvancedEditOverlay : Overlay
{
    private const float ThirtyFps = 30f;

    private readonly TextService _textService;
    private readonly ExcelService _excelService;

    private float _timestamp;
    private float _duration;
    private int _frameCount;
    private bool _isDragging;
    private bool _scrolledBackwards;

    public override OverlayType Type => OverlayType.LeftPane;

    private AgentBannerEditorState* EditorState => AgentBannerEditor.Instance()->EditorState;
    private CharaViewPortrait* CharaView => EditorState != null ? EditorState->CharaView : null;
    private Character* Character => CharaView != null ? CharaView->GetCharacter() : null;

    private hkaAnimation* GetBaseAnimation()
    {
        if (Character == null)
            return null;

        var characterBase = (CharacterBase*)Character->GameObject.DrawObject;
        if (characterBase == null || characterBase->Skeleton == null || characterBase->Skeleton->PartialSkeletonCount == 0 || characterBase->Skeleton->PartialSkeletons == null)
            return null;

        var partialSkeleton = characterBase->Skeleton->PartialSkeletons[0].GetHavokAnimatedSkeleton(0);
        if (partialSkeleton == null || partialSkeleton->AnimationControls.Length == 0 || partialSkeleton->AnimationControls[0].Value == null)
            return null;

        var animationControl = partialSkeleton->AnimationControls[0].Value;
        if (animationControl == null || animationControl->hkaAnimationControl.Binding.ptr == null)
            return null;

        return animationControl->hkaAnimationControl.Binding.ptr->Animation.ptr;
    }

    public override void Update()
    {
        base.Update();

        if (_isDragging || Character == null)
            return;

        _timestamp = CharaView->GetAnimationTime();
        UpdateDuration();
    }

    private void UpdateDuration()
    {
        var animation = GetBaseAnimation();
        if (animation == null)
            return;

        var baseTimeline = Character->Timeline.TimelineSequencer.GetSchedulerTimeline(0);
        if (baseTimeline == null || baseTimeline->ActionTimelineKey.ToString() == "normal/idle")
            return;

        _duration = animation->Duration - 0.5f;
        _frameCount = (int)Math.Round(ThirtyFps * _duration);
    }

    public override void Draw()
    {
        base.Draw();

        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        if (addon == null || EditorState == null || CharaView == null || Character == null)
            return;

        if (!IsWindow)
        {
            ImGuiUtils.DrawSection(
                _textService.Translate("PortraitHelperWindows.AdvancedEditOverlay.Title.Inner"),
                pushDown: false,
                respectUiTheme: true,
                _excelService.CreateRef<UIColor>(2));
        }

        using (var table = ImRaii.Table("##Table", 2))
        {
            if (table)
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                DrawCameraOrientation();
                DrawCameraPosition();
                DrawZoomRotation(addon);
                DrawEyeDirection();
                DrawHeadDirection();
                DrawAnimationControl(addon);
            }
        }

        using ((Misc.IsLightTheme && !IsWindow && _excelService.TryGetRow<UIColor>(3, out var noteColor) ? Color.FromABGR(noteColor.Dark) : Color.Grey).Push(ImGuiCol.Text))
        {
            ImGui.Text(_textService.Translate("PortraitHelperWindows.AdvancedEditOverlay.Note.Label"));
            ImGui.TextWrapped(_textService.Translate("PortraitHelperWindows.AdvancedEditOverlay.Note.Text"));
        }
    }

    private void DrawCameraOrientation()
    {
        using (ImRaii.PushId("CameraOrientation"))
        {
            var yaw = CharaView->CameraYaw;
            var pitch = CharaView->CameraPitch;
            var distance = CharaView->CameraDistance;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraYaw.Label"));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraYaw.Tooltip"));
                ImGui.EndTooltip();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatYaw", ref yaw, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                CharaView->SetCameraYawAndPitch(
                    scale * (yaw - CharaView->CameraYaw),
                    scale * (pitch - CharaView->CameraPitch)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraPitch.Label"));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraPitch.Tooltip"));
                ImGui.EndTooltip();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatPitch", ref pitch, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 100f;

                CharaView->SetCameraYawAndPitch(
                    scale * (yaw - CharaView->CameraYaw),
                    scale * (pitch - CharaView->CameraPitch)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraDistance.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloatDistance", ref distance, 0.001f, 0.5f, 2f))
            {
                var scale = 100f;

                CharaView->SetCameraDistance(
                    scale * (distance - CharaView->CameraDistance)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawCameraPosition()
    {
        using (ImRaii.PushId("CameraPosition"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.CameraTarget.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var pos = new Vector2(
                CharaView->CameraTarget.X,
                CharaView->CameraTarget.Y
            );
            if (ImGui.DragFloat2($"##DragFloat2", ref pos, 0.001f, 0f, 0f, "%.3f", ImGuiSliderFlags.NoInput))
            {
                var scale = 1000f;
                CharaView->SetCameraXAndY(
                    scale * (pos.X - CharaView->CameraTarget.X),
                    scale * (pos.Y - CharaView->CameraTarget.Y)
                );

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawZoomRotation(AddonBannerEditor* addon)
    {
        using (ImRaii.PushId("ZoomRotation"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.ZoomRotation.Label"));

            ImGui.TableNextColumn();

            var itemWidth = (ImGui.GetColumnWidth() - ImGui.GetStyle().ItemInnerSpacing.X) / 2f - 0.5f;
            ImGui.SetNextItemWidth(itemWidth);

            var zoom = (int)CharaView->CameraZoom;
            if (ImGui.DragInt($"##DragFloatZoom", ref zoom, 1, 0, 200))
            {
                CharaView->SetCameraZoom((byte)zoom);
                addon->CameraZoomSlider->SetValue(zoom);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetNextItemWidth(itemWidth + 0.5f);

            var rotation = (int)CharaView->ImageRotation;
            if (ImGui.DragInt($"##DragFloatRotation", ref rotation, 1, -90, 90))
            {
                CharaView->ImageRotation = (short)rotation;
                addon->ImageRotation->SetValue(rotation);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawEyeDirection()
    {
        using (ImRaii.PushId("EyeDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.EyeDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var eyeDirection = new Vector2(
                Character->LookAt.BannerEyeDirection.X,
                Character->LookAt.BannerEyeDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref eyeDirection, 0.001f))
            {
                CharaView->SetEyeDirection(eyeDirection.X, eyeDirection.Y);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawHeadDirection()
    {
        using (ImRaii.PushId("HeadDirection"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.HeadDirection.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            var headDirection = new Vector2(
                Character->LookAt.BannerHeadDirection.X,
                Character->LookAt.BannerHeadDirection.Y
            );

            if (ImGui.DragFloat2($"##DragFloat2", ref headDirection, 0.001f))
            {
                CharaView->SetHeadDirection(headDirection.X, headDirection.Y);

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }
        }
    }

    private void DrawAnimationControl(AddonBannerEditor* addon)
    {
        using (ImRaii.PushId("AnimationTimestamp"))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.Setting.AnimationTimestamp.Label"));

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.DragFloat($"##DragFloat", ref _timestamp, _frameCount < 100 ? 0.001f : 0.01f, 0f, _frameCount, _frameCount < 100 ? $"%.3f / {_frameCount}" : $"%.2f / {_frameCount}"))
            {
                var baseTimeline = Character->Timeline.TimelineSequencer.GetSchedulerTimeline(0);
                if (baseTimeline == null)
                    return;

                var delta = _timestamp - baseTimeline->TimelineController.CurrentTimestamp;
                if (delta < 0)
                {
                    CharaView->SetPoseTimed(Character->Timeline.BannerTimelineRowId, _timestamp);
                    _scrolledBackwards = false;
                }
                else
                {
                    baseTimeline->UpdateBanner(delta, 0);
                    _scrolledBackwards = true;
                }

                CharaView->ToggleAnimationPlayback(true);
                addon->PlayAnimationCheckbox->AtkComponentButton.IsChecked = false;

                if (!EditorState->HasDataChanged)
                    EditorState->SetHasChanged(true);
            }

            _isDragging = ImGui.IsItemActive();

            if (!_isDragging && _scrolledBackwards && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                CharaView->SetPoseTimed(Character->Timeline.BannerTimelineRowId, _timestamp);
                _scrolledBackwards = false;
            }
        }
    }
}
