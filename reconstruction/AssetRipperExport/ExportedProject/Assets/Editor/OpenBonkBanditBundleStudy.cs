#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class OpenBonkBanditBundleStudy
{
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("OPENBONK_BANDIT_BUNDLE_DIR");
        if (string.IsNullOrEmpty(output) || !System.IO.Directory.Exists(output)) throw new Exception("Missing output directory");
        bool android = Environment.GetEnvironmentVariable("OPENBONK_BANDIT_ANDROID") == "1";
        var build = new AssetBundleBuild { assetBundleName = "bandit-clip", assetNames = android
            ? new[] { "Assets/AnimationClip/Jump_1.anim", "Assets/GameObject/Bandit.prefab" }
            : new[] { "Assets/AnimationClip/Jump_1.anim" } };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.UncompressedAssetBundle,
            android ? BuildTarget.Android : BuildTarget.StandaloneLinux64);
        if (result == null) throw new Exception("Bundle build failed");
        Debug.Log("OPENBONK_BANDIT_BUNDLE_BUILD_PASS");
    }

    private struct Pose { public Vector3 position, scale; public Quaternion rotation; }

    private static Pose[][] Capture(string path, GameObject prefab)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new Exception("Cannot load study bundle: " + path);
        GameObject instance = null;
        try
        {
            var clip = bundle.LoadAllAssets<AnimationClip>().Single();
            instance = UnityEngine.Object.Instantiate(prefab);
            var bones = instance.GetComponentsInChildren<Transform>(true);
            if (bones.Length < 2 || clip.length <= 0f) throw new Exception("Invalid clip or prefab");
            var result = new Pose[21][];
            for (int i = 0; i < result.Length; i++)
            {
                clip.SampleAnimation(instance, clip.length * i / (result.Length - 1));
                result[i] = bones.Select(x => new Pose { position = x.localPosition, rotation = x.localRotation, scale = x.localScale }).ToArray();
            }
            Debug.Log($"OPENBONK_BANDIT_CAPTURE clip={clip.name} duration={clip.length:R} transforms={bones.Length}");
            return result;
        }
        finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance); bundle.Unload(true); }
    }

    public static void Compare()
    {
        string directory = Environment.GetEnvironmentVariable("OPENBONK_BANDIT_BUNDLE_DIR");
        if (string.IsNullOrEmpty(directory)) throw new Exception("Missing bundle directory");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Bandit.prefab");
        if (prefab == null) throw new Exception("Missing Bandit prefab");
        var a = Capture(System.IO.Path.Combine(directory, "bandit-clip"), prefab);
        var b = Capture(System.IO.Path.Combine(directory, "bandit-steam"), prefab);
        float position = 0f, rotation = 0f, scale = 0f, movement = 0f, rotationMovement = 0f;
        for (int t = 0; t < a.Length; t++)
        {
            if (a[t].Length != b[t].Length) throw new Exception("Hierarchy mismatch");
            for (int i = 0; i < a[t].Length; i++)
            {
                position = Math.Max(position, Vector3.Distance(a[t][i].position, b[t][i].position));
                rotation = Math.Max(rotation, Quaternion.Angle(a[t][i].rotation, b[t][i].rotation));
                scale = Math.Max(scale, Vector3.Distance(a[t][i].scale, b[t][i].scale));
                movement = Math.Max(movement, Vector3.Distance(a[t][i].position, a[0][i].position));
                rotationMovement = Math.Max(rotationMovement, Quaternion.Angle(a[t][i].rotation, a[0][i].rotation));
            }
        }
        if (movement < .0001f && rotationMovement < .01f) throw new Exception("Clip did not move prefab; comparison inconclusive");
        Debug.Log($"OPENBONK_BANDIT_POSE samples={a.Length} transforms={a[0].Length} maxPosition={position:R} maxRotationDegrees={rotation:R} maxScale={scale:R} maxMovement={movement:R} maxRotationMovementDegrees={rotationMovement:R}");
    }
}
#endif
