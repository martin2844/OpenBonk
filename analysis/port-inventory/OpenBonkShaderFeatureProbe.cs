using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Local comparison only. The generated bundle contains private project material.
public static class OpenBonkShaderFeatureProbe
{
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("OPENBONK_FEATURE_PROBE_OUT");
        const string root = "Assets/OpenBonkShaderFeatureProbeTemp";
        if (String.IsNullOrEmpty(output) || !Directory.Exists(output)) throw new Exception("Missing output");
        if (AssetDatabase.IsValidFolder(root) || Directory.Exists(root)) throw new Exception("Temporary material path already exists");
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        var so = new SerializedObject(settings[0]); so.Update();
        var stripping = so.FindProperty("m_FogStripping"); var linear = so.FindProperty("m_FogKeepLinear");
        var exp = so.FindProperty("m_FogKeepExp"); var exp2 = so.FindProperty("m_FogKeepExp2");
        int oldStrip = stripping.intValue; bool oldLinear = linear.boolValue, oldExp = exp.boolValue, oldExp2 = exp2.boolValue;
        try
        {
            AssetDatabase.CreateFolder("Assets", "OpenBonkShaderFeatureProbeTemp");
            string[] paths = {"Assets/Shader/TextMeshPro_Distance Field Overlay.shader", "Assets/Shader/Native_Cubemap_Skybox.shader"};
            var shader0 = AssetDatabase.LoadAssetAtPath<Shader>(paths[0]); var shader1 = AssetDatabase.LoadAssetAtPath<Shader>(paths[1]);
            if (shader0 == null || shader1 == null) throw new Exception("Missing shader");
            var underlay = new Material(shader0); underlay.EnableKeyword("UNDERLAY_ON");
            var sky = new Material(shader1); sky.EnableKeyword("_ENABLEROTATION_ON"); sky.EnableKeyword("_ENABLEFOG_ON");
            string underlayPath = root + "/underlay.mat", skyPath = root + "/sky.mat";
            AssetDatabase.CreateAsset(underlay, underlayPath); AssetDatabase.CreateAsset(sky, skyPath); AssetDatabase.SaveAssets();
            stripping.intValue = 1; linear.boolValue = false; exp.boolValue = false; exp2.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
            var build = new AssetBundleBuild { assetBundleName = "feature-probe", assetNames = new[] { paths[0], paths[1], underlayPath, skyPath } };
            var result = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
            if (result == null || result.GetAllAssetBundles().Length != 1) throw new Exception("Feature probe failed");
            Debug.Log("OPENBONK_SHADER_FEATURE_PROBE_OK");
        }
        finally
        {
            so.Update(); stripping.intValue = oldStrip; linear.boolValue = oldLinear; exp.boolValue = oldExp; exp2.boolValue = oldExp2; so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.DeleteAsset(root);
        }
    }
}
