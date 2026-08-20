using System.Collections;
using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private enum CameraMode
    {
        Follow,
        Locked,
        Releasing
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform followTarget;

    [Header("Pixel Art")]
    [SerializeField] private bool snapCameraToPixelGrid = true;
    [SerializeField] private int pixelsPerUnit = 16;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private float lockLerpSpeed = 2f;

    [Header("Release From Lock")]
    [SerializeField] private float releaseDuration = 1f;
    [SerializeField] private AnimationCurve releaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CameraMode currentMode;
    private Camera cam;
    private Vector3 followVelocity;
    private Vector3 releaseStartPosition;
    private float releaseTimer;
    private float normalSize;
    private float currentZoomVelocity;
    private float lockedOrthographicSize;
    private Coroutine shakeRoutine;
    private Vector3 shakeOffset;
    private PixelPerfectCinemachineExtension pixelPerfectExtension;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        EnsureCameraRig();
        ResolvePlayerReference();
        InitializeFollowTarget();
    }

    private void OnEnable()
    {
        EnsureCameraRig();
    }

    private void OnValidate()
    {
        lockLerpSpeed = Mathf.Max(0.01f, lockLerpSpeed);
        releaseDuration = Mathf.Max(0.01f, releaseDuration);
        pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);

        if (!Application.isPlaying)
        {
            cam = GetComponent<Camera>();
            RefreshEditorReferences();
        }

        ApplyPixelPerfectSettings();
    }

    private void LateUpdate()
    {
        ResolvePlayerReference();

        if (followTarget == null || virtualCamera == null || cam == null)
        {
            return;
        }

        UpdateModeState();
        UpdateFollowTarget();
        UpdateZoom();
    }

    public void LockToPosition(Vector3 worldPos, float lockedOrthographicSize)
    {
        if (virtualCamera != null && currentMode == CameraMode.Follow)
        {
            normalSize = virtualCamera.m_Lens.OrthographicSize;

            if (player != null && followTarget != null)
            {
                followTarget.position = player.position;
            }
        }

        lockedPosition = worldPos;
        isLocked = true;
        this.lockedOrthographicSize = Mathf.Max(0.1f, lockedOrthographicSize);
        currentMode = CameraMode.Locked;
        virtualCamera.Follow = followTarget;
    }

    public void Unlock(bool smooth = true)
    {
        isLocked = false;

        if (smooth && player != null && followTarget != null)
        {
            currentMode = CameraMode.Releasing;
            releaseTimer = 0f;
            releaseStartPosition = followTarget.position - shakeOffset;
            return;
        }

        currentMode = CameraMode.Follow;

        SetPlayerFollowTarget();
        SetNormalOrthographicSize();
    }

    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f || currentMode == CameraMode.Follow)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (currentMode == CameraMode.Follow)
        {
            SetPlayerFollowTarget();
        }
    }

    private void EnsureCameraRig()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        CinemachineBrain brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = gameObject.AddComponent<CinemachineBrain>();
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
            brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.35f);
        }

        Transform rigParent = transform.parent;

        if (followTarget == null)
        {
            followTarget = FindSiblingTransform("CM Follow Target");
            if (followTarget == null)
            {
                GameObject followObject = new GameObject("CM Follow Target");
                followTarget = followObject.transform;
                followTarget.SetParent(rigParent, false);
            }
        }

        if (virtualCamera == null)
        {
            virtualCamera = FindSiblingVirtualCamera();
        }

        if (virtualCamera == null)
        {
            GameObject vcamObject = new GameObject("CM vcam");
            Transform vcamTransform = vcamObject.transform;
            vcamTransform.SetParent(rigParent, false);
            virtualCamera = vcamObject.AddComponent<CinemachineVirtualCamera>();
        }

        virtualCamera.Priority = 100;
        if (currentMode != CameraMode.Follow || player == null)
        {
            virtualCamera.Follow = followTarget;
        }
        virtualCamera.LookAt = null;
        virtualCamera.m_Lens.Orthographic = true;
        ApplyPixelPerfectSettings();

        if (virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() == null)
        {
            virtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
        }
    }

    private void InitializeFollowTarget()
    {
        currentMode = isLocked ? CameraMode.Locked : CameraMode.Follow;

        if (followTarget == null)
        {
            return;
        }

        normalSize = virtualCamera.m_Lens.OrthographicSize;

        if (isLocked)
        {
            SetFollowTargetPosition(lockedPosition);
            virtualCamera.Follow = followTarget;
        }
        else
        {
            SetPlayerFollowTarget();
        }

    }

    private void ApplyPixelPerfectSettings()
    {
        if (virtualCamera == null)
        {
            return;
        }

        if (pixelPerfectExtension == null)
        {
            pixelPerfectExtension = virtualCamera.GetComponent<PixelPerfectCinemachineExtension>();
        }

        if (pixelPerfectExtension == null)
        {
            pixelPerfectExtension = virtualCamera.gameObject.AddComponent<PixelPerfectCinemachineExtension>();
        }

        pixelPerfectExtension.SnapToPixelGrid = snapCameraToPixelGrid;
        pixelPerfectExtension.PixelsPerUnit = pixelsPerUnit;
        pixelPerfectExtension.enabled = snapCameraToPixelGrid;
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            return;
        }

        if (PlayerReference.IsAvailable)
        {
            player = PlayerReference.Player;
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void UpdateModeState()
    {
        if (isLocked)
        {
            currentMode = CameraMode.Locked;
            virtualCamera.Follow = followTarget;
            return;
        }

        if (currentMode == CameraMode.Locked)
        {
            currentMode = CameraMode.Follow;
            SetPlayerFollowTarget();
            SetNormalOrthographicSize();
        }
    }

    private void UpdateFollowTarget()
    {
        Vector3 desiredPosition;

        switch (currentMode)
        {
            case CameraMode.Locked:
                desiredPosition = new Vector3(lockedPosition.x, lockedPosition.y, 0f);
                desiredPosition = GetSafeFollowPosition(desiredPosition);
                SetFollowTargetPosition(SmoothDampPerAxis(followTarget.position - shakeOffset, desiredPosition, lockLerpSpeed, lockLerpSpeed));
                break;

            case CameraMode.Releasing:
                if (player == null)
                {
                    currentMode = CameraMode.Follow;
                    return;
                }

                releaseTimer += Time.deltaTime;
                float releaseT = Mathf.Clamp01(releaseTimer / releaseDuration);
                desiredPosition = player.position;
                releaseStartPosition = GetSafeFollowPosition(releaseStartPosition);
                desiredPosition = GetSafeFollowPosition(desiredPosition);
                SetFollowTargetPosition(Vector3.Lerp(releaseStartPosition, desiredPosition, releaseCurve.Evaluate(releaseT)));

                if (releaseT >= 0.999f)
                {
                    currentMode = CameraMode.Follow;
                    SetPlayerFollowTarget();
                    SetNormalOrthographicSize();
                }
                break;

            default:
                SetPlayerFollowTarget();
                break;
        }
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * magnitude * fade,
                Random.Range(-1f, 1f) * magnitude * fade,
                0f);
            yield return null;
        }

        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }

    private void SetFollowTargetPosition(Vector3 position)
    {
        if (followTarget != null)
            followTarget.position = position + shakeOffset;
    }

    private void SetPlayerFollowTarget()
    {
        if (virtualCamera != null && player != null)
        {
            virtualCamera.Follow = player;
        }
    }

    private void UpdateZoom()
    {
        if (currentMode == CameraMode.Follow)
        {
            return;
        }

        float targetSize = GetTargetOrthographicSize();
        float smoothTime = 1f / lockLerpSpeed;
        float nextSize = Mathf.SmoothDamp(virtualCamera.m_Lens.OrthographicSize, targetSize, ref currentZoomVelocity, smoothTime);

        virtualCamera.m_Lens.OrthographicSize = nextSize;
        cam.orthographicSize = nextSize;
    }

    private void SetNormalOrthographicSize()
    {
        if (virtualCamera == null || normalSize <= 0f)
        {
            return;
        }

        virtualCamera.m_Lens.OrthographicSize = normalSize;

        if (cam != null)
        {
            cam.orthographicSize = normalSize;
        }
    }

    private float GetTargetOrthographicSize()
    {
        if (!isLocked)
            return normalSize;

        return lockedOrthographicSize > 0f ? lockedOrthographicSize : normalSize;
    }


    private Transform FindSiblingTransform(string objectName)
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            return null;
        }

        Transform sibling = parent.Find(objectName);
        return sibling != null && sibling != transform ? sibling : null;
    }

    private CinemachineVirtualCamera FindSiblingVirtualCamera()
    {
        Transform sibling = FindSiblingTransform("CM vcam");
        return sibling != null ? sibling.GetComponent<CinemachineVirtualCamera>() : null;
    }

    private void RefreshEditorReferences()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindSiblingVirtualCamera();
        }

        if (followTarget == null)
        {
            followTarget = FindSiblingTransform("CM Follow Target");
        }

    }

    private Vector3 SmoothDampPerAxis(Vector3 current, Vector3 target, float horizontalSpeed, float verticalSpeed)
    {
        if (!IsFinite(current))
        {
            current = GetSafeFollowPosition(target);
            followVelocity = Vector3.zero;
        }

        target = GetSafeFollowPosition(target);

        if (!IsFinite(followVelocity))
        {
            followVelocity = Vector3.zero;
        }

        float horizontalSmoothTime = 1f / Mathf.Max(0.01f, horizontalSpeed);
        float verticalSmoothTime = 1f / Mathf.Max(0.01f, verticalSpeed);

        float x = Mathf.SmoothDamp(current.x, target.x, ref followVelocity.x, horizontalSmoothTime);
        float y = Mathf.SmoothDamp(current.y, target.y, ref followVelocity.y, verticalSmoothTime);
        Vector3 result = new Vector3(x, y, target.z);
        return IsFinite(result) ? result : target;
    }

    private Vector3 GetSafeFollowPosition(Vector3 candidate)
    {
        if (IsFinite(candidate))
            return candidate;

        if (followTarget != null && IsFinite(followTarget.position))
            return followTarget.position;

        if (player != null && IsFinite(player.position))
            return new Vector3(player.position.x, player.position.y, 0f);

        return new Vector3(transform.position.x, transform.position.y, 0f);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
