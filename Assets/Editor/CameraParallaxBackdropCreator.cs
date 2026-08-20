using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;

public static class CameraParallaxBackdropCreator
{
    private const string SourceFolder = "Assets/Sprites/56 -Mountain Pass Parallax Backgrounds/Mountain Villagers/Color 2";
    private const string SortingLayerName = "Default";
    private const int FirstSortingOrder = -12;

    private readonly struct LayerDefinition
    {
        public readonly string Name;
        public readonly string SpriteFile;
        public readonly float CameraFollowX;
        public readonly float VerticalFollowY;

        public LayerDefinition(string name, string spriteFile, float cameraFollowX, float verticalFollowY)
        {
            Name = name;
            SpriteFile = spriteFile;
            CameraFollowX = cameraFollowX;
            VerticalFollowY = verticalFollowY;
        }
    }

    private static readonly LayerDefinition[] LayerDefinitions =
    {
        new LayerDefinition("BG2", "BG2.png", 0.96f, 0.01f),
        new LayerDefinition("Sun2", "Sun2.png", 0.88f, 0.015f),
        new LayerDefinition("Fog2", "Fog2.png", 0.72f, 0.02f),
        new LayerDefinition("Far2", "Far2.png", 0.55f, 0.025f),
        new LayerDefinition("Mid2", "Mid2.png", 0.38f, 0.03f),
        new LayerDefinition("Dense Atmosphere2", "dense atmostphere2.png", 0.24f, 0.035f),
        new LayerDefinition("FG2", "FG2.png", 0.12f, 0.04f)
    };

    [MenuItem("Tools/Parallax/Create Mountain Villagers Camera Backdrop")]
    public static void CreateMountainVillagersBackdrop()
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
        CinemachineVirtualCamera virtualCamera = FindVirtualCamera(camera);
        Transform player = FindPlayer();
        GameObject root = new GameObject("MountainVillagersCameraBackdrop");
        Undo.RegisterCreatedObjectUndo(root, "Create Camera Parallax Backdrop");

        if (player != null)
        {
            Vector3 playerPosition = player.position;
            root.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
        }
        else if (camera != null)
        {
            Vector3 cameraPosition = camera.transform.position;
            root.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
        }

        CameraParallaxBackdrop backdrop = root.AddComponent<CameraParallaxBackdrop>();
        List<CameraParallaxBackdrop.Layer> layers = new List<CameraParallaxBackdrop.Layer>();

        for (int i = 0; i < LayerDefinitions.Length; i++)
        {
            LayerDefinition definition = LayerDefinitions[i];
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SourceFolder}/{definition.SpriteFile}");
            if (sprite == null)
            {
                Debug.LogWarning($"Missing parallax sprite: {definition.SpriteFile}");
                continue;
            }

            GameObject layerObject = new GameObject(definition.Name);
            Undo.RegisterCreatedObjectUndo(layerObject, "Create Camera Parallax Layer");
            layerObject.transform.SetParent(root.transform, false);

            SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = FirstSortingOrder + i;

            layers.Add(new CameraParallaxBackdrop.Layer(
                layerObject.transform,
                new Vector2(definition.CameraFollowX, definition.VerticalFollowY)));
        }

        backdrop.Configure(camera, virtualCamera, layers, 1.18f, 1.06f);
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
    }

    private static CinemachineVirtualCamera FindVirtualCamera(Camera camera)
    {
        if (camera != null && camera.transform.parent != null)
        {
            Transform sibling = camera.transform.parent.Find("CM vcam");
            CinemachineVirtualCamera siblingCamera = sibling != null ? sibling.GetComponent<CinemachineVirtualCamera>() : null;
            if (siblingCamera != null)
            {
                return siblingCamera;
            }
        }

        return Object.FindObjectOfType<CinemachineVirtualCamera>();
    }

    private static Transform FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }
}
