#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        // Match the game's linear color space before reimporting FBX materials.
        PlayerSettings.colorSpace = ColorSpace.Linear;
        AssetDatabase.ImportAsset(Asset, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Zap, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(Asset) as ModelImporter;
        if (importer == null) throw new InvalidDataException("Pikachu model importer is missing");
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }
        if (AssetDatabase.LoadAssetAtPath<AudioClip>(Zap) == null) throw new InvalidDataException("Thunderbolt audio is missing");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Asset);
        if (model == null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
            throw new InvalidDataException("Pikachu model has no skinned renderer");
        var clips = AssetDatabase.LoadAllAssetsAtPath(Asset);
        var required = new HashSet<string> { "Armature|OpenBonk walk study", "Armature|OpenBonk idle", "Armature|OpenBonk run", "Armature|OpenBonk jump", "Armature|OpenBonk surf" };
        foreach (var clip in clips) if (clip is AnimationClip c) { Debug.Log("Pikachu movement clip: " + c.name); required.Remove(c.name); }
        if (required.Count != 0) throw new InvalidDataException("Pikachu movement clips are missing: " + string.Join(", ", required));
        bool board = false;
        foreach (var transform in model.GetComponentsInChildren<Transform>(true)) if (transform.name == "OpenBonkSurfboard") board = true;
        if (!board) throw new InvalidDataException("Pikachu surfboard is missing");
        string configured = Environment.GetEnvironmentVariable("OPENBONK_PIKACHU_BUNDLE_OUTPUT");
        string root = Path.GetFullPath(string.IsNullOrEmpty(configured) ? Path.Combine(Application.dataPath, "../../../../mods/pikachu/study/android-bundle") : configured);
        Directory.CreateDirectory(root);
        var build = new AssetBundleBuild { assetBundleName = "pikachu-model", assetNames = new[] { Asset, Zap } };
        var manifest = BuildPipeline.BuildAssetBundles(root, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.Android);
        if (manifest == null || !File.Exists(Path.Combine(root, "pikachu-model"))) throw new InvalidDataException("Android bundle build failed");
        var bundle = AssetBundle.LoadFromFile(Path.Combine(root, "pikachu-model"));
        if (bundle == null) throw new InvalidDataException("Built Pikachu bundle cannot be loaded");
        try
        {
            var bundledModel = bundle.LoadAsset<GameObject>("pikachu-study");
            if (bundledModel == null || bundle.LoadAsset<AudioClip>("thunderbolt-zap") == null)
                throw new InvalidDataException("Built Pikachu bundle is missing its model or audio");
            bool bundledBoard = false;
            foreach (var transform in bundledModel.GetComponentsInChildren<Transform>(true)) if (transform.name == "OpenBonkSurfboard") bundledBoard = true;
            if (!bundledBoard) throw new InvalidDataException("Built Pikachu bundle is missing its surfboard");
            var bundledClips = new HashSet<string> { "Armature|OpenBonk walk study", "Armature|OpenBonk idle", "Armature|OpenBonk run", "Armature|OpenBonk jump", "Armature|OpenBonk surf" };
            foreach (var clip in bundle.LoadAllAssets<AnimationClip>()) bundledClips.Remove(clip.name);
            if (bundledClips.Count != 0) throw new InvalidDataException("Built Pikachu bundle is missing clips: " + string.Join(", ", bundledClips));
        }
        finally { bundle.Unload(true); }
        Debug.Log("OpenBonk Pikachu bundle created: " + root);
    }
}
#endif
