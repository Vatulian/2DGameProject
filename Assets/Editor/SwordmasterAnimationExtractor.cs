using System.IO;
using UnityEditor;
using UnityEngine;

public static class SwordmasterAnimationExtractor
{
    private const string SwordmasterSource = "Assets/Sprites/The SwordMaster/Sword Master Sprite Sheet.aseprite";
    private const string LedgeClimbSource = "Assets/Sprites/The SwordMaster/Sword Master Sprite Sheet Ledge Climb.aseprite";
    private const string OutputFolder = "Assets/Animation/For Player/SwordmasterExtracted";

    [MenuItem("Tools/Swordmaster/Extract Generated Animation Clips")]
    public static void ExtractGeneratedAnimationClips()
    {
        ExtractFromSource(SwordmasterSource, "Swordmaster");
        ExtractFromSource(LedgeClimbSource, "Swordmaster_LedgeClimb");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Swordmaster Extract",
            "Read-only Aseprite animation clips were copied to standalone .anim files.",
            "OK");
    }

    private static void ExtractFromSource(string sourcePath, string prefix)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"Swordmaster extractor could not find source: {sourcePath}");
            return;
        }

        EnsureFolderExists(OutputFolder);

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
        int createdCount = 0;

        foreach (Object asset in subAssets)
        {
            if (asset is not AnimationClip sourceClip)
                continue;

            // Unity can expose helper clips; skip empty/internal entries.
            if (sourceClip.name.StartsWith("__preview__"))
                continue;

            string safeName = SanitizeFileName($"{prefix}_{sourceClip.name}.anim");
            string targetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(OutputFolder, safeName));

            AnimationClip clipCopy = Object.Instantiate(sourceClip);
            AssetDatabase.CreateAsset(clipCopy, targetPath);
            createdCount++;
        }

        Debug.Log($"Swordmaster extractor created {createdCount} clips from {sourcePath}");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidChar, '_');

        return fileName.Replace(' ', '_');
    }
}
