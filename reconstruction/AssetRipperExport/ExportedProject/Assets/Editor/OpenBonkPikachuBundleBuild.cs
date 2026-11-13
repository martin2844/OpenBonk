#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Authoring experiment: produces an external Android model bundle. It is not
// included in the game scenes and is not yet an installable mod package.
public static class OpenBonkPikachuBundleBuild
{
    private const string Asset = "Assets/OpenBonkModAuthoring/Pikachu/pikachu-study.fbx";
    private const string Zap = "Assets/OpenBonkModAuthoring/Pikachu/thunderbolt-zap.wav";
    public static void Build()
    {
        AssetDatabase.ImportAsset(Asset, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(Zap, ImportAssetOptions.ForceSynchronousImport);
        if (AssetDatabase.LoadAssetAtPath<AudioClip>(Zap) == null) throw new InvalidDataException("Thunderbolt audio is missing");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Asset);
        if (model == null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
            throw new InvalidDataException("Pikachu model has no skinned renderer");
        var clips = AssetDatabase.LoadAllAssetsAtPath(Asset);
        bool walk = false;
        foreach (var clip in clips) if (clip is AnimationClip c) { Debug.Log("Pikachu movement clip: " + c.name); if (c.name.Contains("OpenBonk walk")) walk = true; }
        if (!walk) throw new InvalidDataException("Walk animation was not imported");
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../mods/pikachu/study/android-bundle"));
        Directory.CreateDirectory(root);
        var build = new AssetBundleBuild { assetBundleName = "pikachu-model", assetNames = new[] { Asset, Zap } };
        var manifest = BuildPipeline.BuildAssetBundles(root, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.Android);
        if (manifest == null || !File.Exists(Path.Combine(root, "pikachu-model"))) throw new InvalidDataException("Android bundle build failed");
        Debug.Log("OpenBonk Pikachu bundle created: " + root);
    }
}
#endif
