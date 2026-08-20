using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class CameraParallaxBackdrop : MonoBehaviour
{
    [Serializable]
    public sealed class Layer
    {
        [SerializeField] private Transform transform;
        [Tooltip("X controls horizontal parallax. Y should usually stay 1 so vertical movement does not detach the backdrop.")]
        [SerializeField] private Vector2 parallax = new Vector2(0.5f, 1f);
        [SerializeField] private bool repeatX = true;

        private SpriteRenderer centerRenderer;
        private SpriteRenderer leftRenderer;
        private SpriteRenderer rightRenderer;
        private Vector3 startPosition;
        private Vector3 sourceOrigin;
        private float movementOriginX;

        public Layer(Transform transform, Vector2 parallax)
        {
            this.transform = transform;
            this.parallax = parallax;
        }

        public void Initialize(Vector3 sourcePosition, bool createSideCopies)
        {
            if (transform == null)
            {
                return;
            }

            EnsureRenderer();
            if (centerRenderer == null || centerRenderer.sprite == null)
            {
                return;
            }

            startPosition = transform.position;
            sourceOrigin = sourcePosition;
            movementOriginX = 0f;

            if (createSideCopies)
            {
                EnsureSideCopies();
                SyncSideCopies();
                PositionSideCopies();
            }
        }

        public void Tick(
            Vector3 sourcePosition,
            float viewWidth,
            float viewHeight,
            float widthOverscan,
            float heightOverscan,
            bool createSideCopies)
        {
            if (transform == null)
            {
                return;
            }

            EnsureRenderer();
            if (centerRenderer == null || centerRenderer.sprite == null)
            {
                return;
            }

            ScaleToCoverView(viewWidth, viewHeight, widthOverscan, heightOverscan);

            if (createSideCopies)
            {
                EnsureSideCopies();
                SyncSideCopies();
                PositionSideCopies();
            }

            Vector3 sourceDelta = sourcePosition - sourceOrigin;
            Vector3 position = transform.position;

            float distanceX = sourceDelta.x * parallax.x;
            float movementX = sourceDelta.x * (1f - parallax.x);

            if (repeatX)
            {
                WrapStartPosition(ref startPosition.x, ref movementOriginX, movementX, centerRenderer.bounds.size.x);
            }

            position.x = startPosition.x + distanceX;
            position.y = startPosition.y + sourceDelta.y * parallax.y;
            transform.position = position;
        }

        private void EnsureRenderer()
        {
            if (centerRenderer == null && transform != null)
            {
                centerRenderer = transform.GetComponent<SpriteRenderer>();
            }
        }

        private void ScaleToCoverView(float viewWidth, float viewHeight, float widthOverscan, float heightOverscan)
        {
            Vector2 spriteSize = centerRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float requiredWidth = viewWidth * Mathf.Max(1f, widthOverscan);
            float requiredHeight = viewHeight * Mathf.Max(1f, heightOverscan);
            float scale = Mathf.Max(requiredWidth / spriteSize.x, requiredHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, transform.localScale.z);
        }

        private void EnsureSideCopies()
        {
            leftRenderer = EnsureSideCopy("Left", leftRenderer);
            rightRenderer = EnsureSideCopy("Right", rightRenderer);
        }

        private SpriteRenderer EnsureSideCopy(string objectName, SpriteRenderer cachedRenderer)
        {
            if (cachedRenderer != null)
            {
                return cachedRenderer;
            }

            Transform child = transform.Find(objectName);
            SpriteRenderer renderer = child != null ? child.GetComponent<SpriteRenderer>() : null;

            if (renderer == null)
            {
                GameObject sideObject = new GameObject(objectName);
                sideObject.transform.SetParent(transform, false);
                renderer = sideObject.AddComponent<SpriteRenderer>();
            }

            return renderer;
        }

        private void SyncSideCopies()
        {
            SyncRenderer(leftRenderer);
            SyncRenderer(rightRenderer);
        }

        private void SyncRenderer(SpriteRenderer copy)
        {
            if (centerRenderer == null || copy == null)
            {
                return;
            }

            copy.sprite = centerRenderer.sprite;
            copy.color = centerRenderer.color;
            copy.flipX = centerRenderer.flipX;
            copy.flipY = centerRenderer.flipY;
            copy.drawMode = centerRenderer.drawMode;
            copy.sortingLayerID = centerRenderer.sortingLayerID;
            copy.sortingOrder = centerRenderer.sortingOrder;
            copy.sharedMaterial = centerRenderer.sharedMaterial;
            copy.maskInteraction = centerRenderer.maskInteraction;
        }

        private void PositionSideCopies()
        {
            if (centerRenderer == null || centerRenderer.sprite == null)
            {
                return;
            }

            float localLength = centerRenderer.sprite.bounds.size.x;

            if (leftRenderer != null)
            {
                leftRenderer.transform.localPosition = new Vector3(-localLength, 0f, 0f);
            }

            if (rightRenderer != null)
            {
                rightRenderer.transform.localPosition = new Vector3(localLength, 0f, 0f);
            }
        }

        private static void WrapStartPosition(ref float startPosition, ref float movementOrigin, float movement, float length)
        {
            if (length <= 0f)
            {
                return;
            }

            while (movement > movementOrigin + length)
            {
                startPosition += length;
                movementOrigin += length;
            }

            while (movement < movementOrigin - length)
            {
                startPosition -= length;
                movementOrigin -= length;
            }
        }
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CinemachineVirtualCamera targetVirtualCamera;
    [SerializeField] private Transform player;
    [SerializeField] private bool useMainCameraWhenEmpty = true;

    [Header("Behavior")]
    [SerializeField] private bool createSideCopies = true;
    [SerializeField] private bool rebaseOnPlayerTeleport = true;
    [SerializeField] private float playerTeleportDistance = 6f;

    [Header("Coverage")]
    [SerializeField] private float widthOverscan = 1.18f;
    [SerializeField] private float heightOverscan = 1.06f;

    [Header("Layers")]
    [SerializeField] private List<Layer> layers = new List<Layer>();

    private Camera resolvedCamera;
    private CinemachineVirtualCamera resolvedVirtualCamera;
    private Vector3 lastPlayerPosition;
    private bool hasLastPlayerPosition;
    private bool initialized;

    public void Configure(
        Camera camera,
        CinemachineVirtualCamera virtualCamera,
        IEnumerable<Layer> configuredLayers,
        float horizontalOverscan,
        float verticalOverscan)
    {
        targetCamera = camera;
        targetVirtualCamera = virtualCamera;
        layers.Clear();

        if (configuredLayers != null)
        {
            layers.AddRange(configuredLayers);
        }

        widthOverscan = horizontalOverscan;
        heightOverscan = verticalOverscan;
        Rebase();
    }

    public void Rebase()
    {
        if (!TryResolveCamera(out Camera camera))
        {
            return;
        }

        Vector3 source = ResolveParallaxSource(camera);
        RefreshPlayerTracking(true);

        for (int i = 0; i < layers.Count; i++)
        {
            layers[i]?.Initialize(source, createSideCopies);
        }

        initialized = true;
        Tick(camera);
    }

    private void OnEnable()
    {
        Rebase();
    }

    private void LateUpdate()
    {
        if (!TryResolveCamera(out Camera camera))
        {
            return;
        }

        if (!initialized || ShouldRebaseForPlayerTeleport())
        {
            Rebase();
            return;
        }

        Tick(camera);
    }

    private void Tick(Camera camera)
    {
        if (camera == null || !camera.orthographic)
        {
            return;
        }

        float orthographicSize = ResolveOrthographicSize(camera);
        float viewWidth = orthographicSize * 2f * camera.aspect;
        float viewHeight = orthographicSize * 2f;
        Vector3 source = ResolveParallaxSource(camera);

        for (int i = 0; i < layers.Count; i++)
        {
            layers[i]?.Tick(source, viewWidth, viewHeight, widthOverscan, heightOverscan, createSideCopies);
        }
    }

    private Vector3 ResolveParallaxSource(Camera camera)
    {
        CinemachineVirtualCamera virtualCamera = ResolveVirtualCamera(camera);
        if (virtualCamera != null && virtualCamera.Follow != null)
        {
            return virtualCamera.Follow.position;
        }

        Transform currentPlayer = ResolvePlayer();
        if (currentPlayer != null)
        {
            return currentPlayer.position;
        }

        return camera != null ? camera.transform.position : transform.position;
    }

    private float ResolveOrthographicSize(Camera camera)
    {
        CinemachineVirtualCamera virtualCamera = ResolveVirtualCamera(camera);
        if (virtualCamera != null && virtualCamera.m_Lens.Orthographic)
        {
            return Mathf.Max(0.01f, virtualCamera.m_Lens.OrthographicSize);
        }

        return camera != null ? Mathf.Max(0.01f, camera.orthographicSize) : 5f;
    }

    private CinemachineVirtualCamera ResolveVirtualCamera(Camera camera)
    {
        if (targetVirtualCamera != null)
        {
            resolvedVirtualCamera = targetVirtualCamera;
            return resolvedVirtualCamera;
        }

        if (resolvedVirtualCamera != null)
        {
            return resolvedVirtualCamera;
        }

        if (camera == null)
        {
            return null;
        }

        CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            resolvedVirtualCamera = brain.ActiveVirtualCamera as CinemachineVirtualCamera;
            if (resolvedVirtualCamera != null)
            {
                return resolvedVirtualCamera;
            }
        }

        CameraController cameraController = camera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            Transform rigParent = cameraController.transform.parent;
            Transform sibling = rigParent != null ? rigParent.Find("CM vcam") : null;
            resolvedVirtualCamera = sibling != null ? sibling.GetComponent<CinemachineVirtualCamera>() : null;
        }

        return resolvedVirtualCamera;
    }

    private bool TryResolveCamera(out Camera camera)
    {
        if (targetCamera != null)
        {
            camera = targetCamera;
            resolvedCamera = camera;
            return true;
        }

        if (resolvedCamera != null)
        {
            camera = resolvedCamera;
            return true;
        }

        resolvedCamera = useMainCameraWhenEmpty ? Camera.main : null;
        camera = resolvedCamera;
        return camera != null;
    }

    private bool ShouldRebaseForPlayerTeleport()
    {
        if (!rebaseOnPlayerTeleport || playerTeleportDistance <= 0f)
        {
            RefreshPlayerTracking(false);
            return false;
        }

        Transform currentPlayer = ResolvePlayer();
        if (currentPlayer == null)
        {
            hasLastPlayerPosition = false;
            return false;
        }

        if (!hasLastPlayerPosition || currentPlayer != player)
        {
            player = currentPlayer;
            lastPlayerPosition = currentPlayer.position;
            hasLastPlayerPosition = true;
            return false;
        }

        Vector3 playerDelta = currentPlayer.position - lastPlayerPosition;
        lastPlayerPosition = currentPlayer.position;
        return playerDelta.sqrMagnitude >= playerTeleportDistance * playerTeleportDistance;
    }

    private void RefreshPlayerTracking(bool force)
    {
        Transform currentPlayer = ResolvePlayer();
        if (currentPlayer == null)
        {
            hasLastPlayerPosition = false;
            return;
        }

        if (force || !hasLastPlayerPosition || currentPlayer != player)
        {
            player = currentPlayer;
            lastPlayerPosition = currentPlayer.position;
            hasLastPlayerPosition = true;
        }
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
        {
            return player;
        }

        if (PlayerReference.Player != null)
        {
            player = PlayerReference.Player;
            return player;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        return player;
    }
}