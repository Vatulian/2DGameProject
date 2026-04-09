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
    [SerializeField] private float aheadDistance = 2f;
    [SerializeField] private float verticalOffset = 0.75f;
    [FormerlySerializedAs("followLerpSpeed")]
    [SerializeField] private float followLerpSpeed = 5f;
    [SerializeField] private float verticalFollowLerpSpeed = 9f;
    [SerializeField] private bool snapToPlayerOnStart = true;

    [Header("Cinemachine Feel")]
    [SerializeField] private float horizontalDamping = 0.45f;
    [SerializeField] private float verticalDamping = 0.2f;
    [SerializeField] private float screenX = 0.5f;
    [SerializeField] private float screenY = 0.55f;
    [SerializeField] private float deadZoneWidth = 0.05f;
    [SerializeField] private float deadZoneHeight = 0.12f;

    [Header("Zoom")]
    [SerializeField] private float normalSize = 5f;
    [SerializeField] private float bossSize = 7f;
    [FormerlySerializedAs("zoomLerpSpeed")]
    [SerializeField] private float zoomLerpSpeed = 3f;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked;
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private bool useBossZoom;
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
        bossSize = Mathf.Max(normalSize, bossSize);
        horizontalDamping = Mathf.Max(0f, horizontalDamping);
        verticalDamping = Mathf.Max(0f, verticalDamping);
        deadZoneWidth = Mathf.Clamp01(deadZoneWidth);
        deadZoneHeight = Mathf.Clamp01(deadZoneHeight);
        screenX = Mathf.Clamp01(screenX);
        screenY = Mathf.Clamp01(screenY);

        if (!Application.isPlaying)
        {
            cam = GetComponent<Camera>();
            EnsureCameraRig();
        }

        ApplyCinemachineSettings();
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

    public void LockToPosition(Vector3 worldPos, bool zoomToBoss = false)
    {
        lockedPosition = worldPos;
        isLocked = true;
        useBossZoom = zoomToBoss;
        currentMode = CameraMode.Locked;
    }

    public void Unlock(bool smooth = true)
    {
        isLocked = false;
        useBossZoom = false;

        if (smooth && player != null && followTarget != null)
        {
            currentMode = CameraMode.Releasing;
            releaseTimer = 0f;
            releaseStartPosition = followTarget.position;
            return;
        }

        currentMode = CameraMode.Follow;

        if (followTarget != null && player != null)
        {
            followTarget.position = CalculateFollowPosition();
        }
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;

        if (snapToPlayerOnStart && followTarget != null && player != null)
        {
            followTarget.position = CalculateFollowPosition();
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
        followTarget.position = startPosition;

        if (cam != null)
        {
            cam.orthographicSize = useBossZoom ? bossSize : normalSize;
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
        framingTransposer.m_ScreenX = screenX;
        framingTransposer.m_ScreenY = screenY;
        framingTransposer.m_DeadZoneWidth = deadZoneWidth;
        framingTransposer.m_DeadZoneHeight = deadZoneHeight;
        framingTransposer.m_UnlimitedSoftZone = false;
        framingTransposer.m_BiasX = 0f;
        framingTransposer.m_BiasY = 0f;
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
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
        float smoothTime = 1f / followLerpSpeed;

        switch (currentMode)
        {
            case CameraMode.Locked:
                desiredPosition = new Vector3(lockedPosition.x, lockedPosition.y, 0f);
                followTarget.position = SmoothDampPerAxis(followTarget.position, desiredPosition, lockLerpSpeed, lockLerpSpeed);
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
                followTarget.position = Vector3.Lerp(releaseStartPosition, desiredPosition, releaseCurve.Evaluate(releaseT));

                if (releaseT >= 0.999f)
                {
                    currentMode = CameraMode.Follow;
                    followTarget.position = desiredPosition;
                }
                break;

            default:
                if (player == null)
                {
                    return;
                }

                desiredPosition = CalculateFollowPosition();
                followTarget.position = SmoothDampPerAxis(followTarget.position, desiredPosition, followLerpSpeed, verticalFollowLerpSpeed);
                break;
        }
    }

    private void UpdateZoom()
    {
        float targetSize = useBossZoom && isLocked ? bossSize : normalSize;
        float smoothTime = 1f / zoomLerpSpeed;
        float nextSize = Mathf.SmoothDamp(virtualCamera.m_Lens.OrthographicSize, targetSize, ref currentZoomVelocity, smoothTime);

        virtualCamera.m_Lens.OrthographicSize = nextSize;
        cam.orthographicSize = nextSize;
    }

    private Vector3 CalculateFollowPosition()
    {
        if (player == null)
        {
            return followTarget != null ? followTarget.position : transform.position;
        }

        float facing = ResolveFacingDirection();
        float x = player.position.x + aheadDistance * facing;
        float y = player.position.y + verticalOffset;
        return new Vector3(x, y, 0f);
    }

    private float ResolveFacingDirection()
    {
        if (player == null)
        {
            return 1f;
        }

        SpriteRenderer spriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX ? -1f : 1f;
        }

        float direction = player.lossyScale.x;
        return Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction);
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

    private Vector3 SmoothDampPerAxis(Vector3 current, Vector3 target, float horizontalSpeed, float verticalSpeed)
    {
        float horizontalSmoothTime = 1f / Mathf.Max(0.01f, horizontalSpeed);
        float verticalSmoothTime = 1f / Mathf.Max(0.01f, verticalSpeed);

        float x = Mathf.SmoothDamp(current.x, target.x, ref followVelocity.x, horizontalSmoothTime);
        float y = Mathf.SmoothDamp(current.y, target.y, ref followVelocity.y, verticalSmoothTime);
        return new Vector3(x, y, target.z);
    }
}
