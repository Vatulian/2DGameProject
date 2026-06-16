using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class PixelPerfectCinemachineExtension : CinemachineExtension
{
    public bool SnapToPixelGrid { get; set; } = true;
    public int PixelsPerUnit { get; set; } = 16;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (!SnapToPixelGrid || PixelsPerUnit <= 0 || stage != CinemachineCore.Stage.Body)
        {
            return;
        }

        float unitsPerPixel = 1f / PixelsPerUnit;
        Vector3 position = state.RawPosition;
        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        state.RawPosition = position;
    }
}
