using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Follow")]
    [FormerlySerializedAs("aheadDistance")]
    [Range(0f, 0.25f)]
    [SerializeField] private float lookAheadScreenOffset = 0.12f;
    [SerializeField] private float verticalOffset = 0.75f;
    [FormerlySerializedAs("followLerpSpeed")]
    [SerializeField] private float followLerpSpeed = 5f;
    [SerializeField] private float verticalFollowLerpSpeed = 9f;
    [SerializeField] private bool snapToPlayerOnStart = true;
    [SerializeField] private float screenOffsetLerpSpeed = 6f;

    [Header("Cinemachine Feel")]
    [SerializeField] private float horizontalDamping = 0.45f;
    [SerializeField] private float verticalDamping = 0.2f;
    [SerializeField] private float screenX = 0.5f;
    [SerializeField] private float screenY = 0.55f;
    [SerializeField] private float deadZoneWidth = 0.05f;
    [SerializeField] private float deadZoneHeight = 0.12f;

    [Header("Zoom")]
    [SerializeField] private float normalSize = 5f;
    [FormerlySerializedAs("zoomLerpSpeed")]
    [SerializeField] private float zoomLerpSpeed = 3f;

    [Header("Pixel Art")]
    [SerializeField] private bool snapCameraToPixelGrid = true;
    [SerializeField] private bool snapOrthographicSizeToPixelRatio = true;
    [SerializeField] private int pixelsPerUnit = 16;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;
    [SerializeField] private Vector3 lockedPosition;
    [FormerlySerializedAs("lockLerpSpeed")]
    [SerializeField] private float lockLerpSpeed = 2f;

    [Header("Release From Lock")]
    [SerializeField] private float releaseDuration = 1f;
    [SerializeField] private AnimationCurve releaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CameraMode currentMode;
    private Camera cam;
    private CinemachineFramingTransposer framingTransposer;
    private Vector3 followVelocity;
    private Vector3 releaseStartPosition;
    private float releaseTimer;
    private float currentZoomVelocity;
    private float currentScreenX;
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
        ApplyCinemachineSettings();
    }

    private void OnEnable()
    {
        EnsureCameraRig();
        ApplyCinemachineSettings();
    }

    private void OnValidate()
    {
        followLerpSpeed = Mathf.Max(0.01f, followLerpSpeed);
        verticalFollowLerpSpeed = Mathf.Max(0.01f, verticalFollowLerpSpeed);
        lockLerpSpeed = Mathf.Max(0.01f, lockLerpSpeed);
        zoomLerpSpeed = Mathf.Max(0.01f, zoomLerpSpeed);
        releaseDuration = Mathf.Max(0.01f, releaseDuration);
        normalSize = Mathf.Max(0.1f, normalSize);
        pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
        horizontalDamping = Mathf.Max(0f, horizontalDamping);
        verticalDamping = Mathf.Max(0f, verticalDamping);
        deadZoneWidth = Mathf.Clamp01(deadZoneWidth);
        deadZoneHeight = Mathf.Clamp01(deadZoneHeight);
        screenX = Mathf.Clamp01(screenX);
        screenY = Mathf.Clamp01(screenY);
        lookAheadScreenOffset = Mathf.Clamp(lookAheadScreenOffset, 0f, 0.25f);
        screenOffsetLerpSpeed = Mathf.Max(0.01f, screenOffsetLerpSpeed);

        if (!Application.isPlaying)
        {
            cam = GetComponent<Camera>();
            RefreshEditorReferences();
        }

        ApplyCinemachineSettings();
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
        UpdateScreenComposition();
        UpdateZoom();
    }

    public void LockToPosition(Vector3 worldPos, float lockedOrthographicSize)
    {
        lockedPosition = worldPos;
        isLocked = true;
        this.lockedOrthographicSize = Mathf.Max(0.1f, lockedOrthographicSize);
        currentMode = CameraMode.Locked;
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

        if (followTarget != null && player != null)
        {
            SetFollowTargetPosition(CalculateFollowPosition());
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (snapToPlayerOnStart && followTarget != null && player != null)
        {
            SetFollowTargetPosition(CalculateFollowPosition());
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
        virtualCamera.Follow = followTarget;
        virtualCamera.LookAt = null;
        virtualCamera.m_Lens.Orthographic = true;
        ApplyPixelPerfectSettings();

        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer == null)
        {
            virtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
            framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }
    }

    private void InitializeFollowTarget()
    {
        currentMode = isLocked ? CameraMode.Locked : CameraMode.Follow;

        if (followTarget == null)
        {
            return;
        }

        Vector3 startPosition = isLocked ? lockedPosition : CalculateFollowPosition();
        startPosition.z = 0f;
        SetFollowTargetPosition(startPosition);
        currentScreenX = GetTargetScreenX();

        if (cam != null)
        {
            cam.orthographicSize = GetTargetOrthographicSize();
        }
    }

    private void ApplyCinemachineSettings()
    {
        if (virtualCamera == null)
        {
            return;
        }

        virtualCamera.m_Lens.Orthographic = true;
        virtualCamera.m_Lens.OrthographicSize = cam != null ? cam.orthographicSize : normalSize;

        if (followTarget != null)
        {
            virtualCamera.Follow = followTarget;
        }

        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer == null)
        {
            return;
        }

        framingTransposer.m_XDamping = horizontalDamping;
        framingTransposer.m_YDamping = verticalDamping;
        framingTransposer.m_ZDamping = 0f;
        framingTransposer.m_ScreenX = Application.isPlaying ? currentScreenX : GetTargetScreenX();
        framingTransposer.m_ScreenY = screenY;
        framingTransposer.m_DeadZoneWidth = deadZoneWidth;
        framingTransposer.m_DeadZoneHeight = deadZoneHeight;
        framingTransposer.m_UnlimitedSoftZone = false;
        framingTransposer.m_BiasX = 0f;
        framingTransposer.m_BiasY = 0f;
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
            return;
        }

        if (currentMode == CameraMode.Locked)
        {
            currentMode = CameraMode.Follow;
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
                desiredPosition = CalculateFollowPosition();
                releaseStartPosition = GetSafeFollowPosition(releaseStartPosition);
                desiredPosition = GetSafeFollowPosition(desiredPosition);
                SetFollowTargetPosition(Vector3.Lerp(releaseStartPosition, desiredPosition, releaseCurve.Evaluate(releaseT)));

                if (releaseT >= 0.999f)
                {
                    currentMode = CameraMode.Follow;
                    SetFollowTargetPosition(desiredPosition);
                }
                break;

            default:
                if (player == null)
                {
                    return;
                }

                desiredPosition = CalculateFollowPosition();
                desiredPosition = GetSafeFollowPosition(desiredPosition);
                SetFollowTargetPosition(SmoothDampPerAxis(followTarget.position - shakeOffset, desiredPosition, followLerpSpeed, verticalFollowLerpSpeed));
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

    private void UpdateZoom()
    {
        float targetSize = GetTargetOrthographicSize();
        float smoothTime = 1f / zoomLerpSpeed;
        float nextSize = Mathf.SmoothDamp(virtualCamera.m_Lens.OrthographicSize, targetSize, ref currentZoomVelocity, smoothTime);

        nextSize = GetPixelPerfectOrthographicSize(nextSize);
        virtualCamera.m_Lens.OrthographicSize = nextSize;
        cam.orthographicSize = nextSize;
    }

    private float GetTargetOrthographicSize()
    {
        if (!isLocked)
            return normalSize;

        return lockedOrthographicSize > 0f ? lockedOrthographicSize : normalSize;
    }

    private float GetPixelPerfectOrthographicSize(float size)
    {
        if (!snapOrthographicSizeToPixelRatio || pixelsPerUnit <= 0 || !Application.isPlaying || Screen.height <= 0)
        {
            return size;
        }

        float assetPixelsPerScreenPixel = Screen.height / (2f * size * pixelsPerUnit);
        int pixelRatio = Mathf.Max(1, Mathf.RoundToInt(assetPixelsPerScreenPixel));
        return Screen.height / (2f * pixelsPerUnit * pixelRatio);
    }

    private void UpdateScreenComposition()
    {
        if (framingTransposer == null)
        {
            return;
        }

        float targetScreenX = GetTargetScreenX();
        float lerpT = 1f - Mathf.Exp(-screenOffsetLerpSpeed * Time.deltaTime);
        currentScreenX = Mathf.Lerp(currentScreenX, targetScreenX, lerpT);
        framingTransposer.m_ScreenX = currentScreenX;
    }

    private Vector3 CalculateFollowPosition()
    {
        if (player == null)
        {
            return followTarget != null ? followTarget.position : transform.position;
        }

        float x = player.position.x;
        float y = player.position.y + verticalOffset;
        return GetSafeFollowPosition(new Vector3(x, y, 0f));
    }

    private float ResolveFacingDirection()
    {
        if (player == null)
        {
            return 1f;
        }

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            return playerMovement.IsFacingRight ? 1f : -1f;
        }

        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX ? -1f : 1f;
        }

        float direction = player.lossyScale.x;
        return Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction);
    }

    private float GetTargetScreenX()
    {
        if (currentMode == CameraMode.Locked)
        {
            return screenX;
        }

        float facing = ResolveFacingDirection();
        return Mathf.Clamp01(screenX - (facing * lookAheadScreenOffset));
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

        if (virtualCamera != null)
        {
            framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
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
            return new Vector3(player.position.x, player.position.y + verticalOffset, 0f);

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
