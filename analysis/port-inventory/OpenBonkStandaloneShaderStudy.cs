using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Local diagnostic: compile two standalone shaders without modifying graphics settings.
public static class OpenBonkStandaloneShaderStudy {
    public static void Build() {
        string output = Environment.GetEnvironmentVariable("OPENBONK_STANDALONE_SHADER_OUT");
        if (String.IsNullOrEmpty(output) || !Directory.Exists(output))
            throw new Exception("Missing isolated output directory");
        string probuilder = Environment.GetEnvironmentVariable("OPENBONK_PACKAGE_PROBUILDER") == "1"
            ? "Packages/com.unity.probuilder/Content/Shader/StandardVertexColor.shader"
            : "Assets/Shader/ProBuilder_Standard Vertex Color.shader";
        string[] paths = {
            probuilder,
            "Assets/Shader/MK_Toon_Built-in_Standard_Simple.shader"
        };
        foreach (string path in paths)
            if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null)
                throw new Exception("Missing shader: " + path);
        var builds = new[] {
            new AssetBundleBuild { assetBundleName = "standalone-shaders", assetNames = paths }
        };
        var graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (graphics.Length == 0) throw new Exception("Graphics settings asset missing");
        var settings = new SerializedObject(graphics[0]); settings.Update();
        var instancing = settings.FindProperty("m_InstancingStripping");
        var fog = settings.FindProperty("m_FogStripping");
        var linear = settings.FindProperty("m_FogKeepLinear");
        var exp = settings.FindProperty("m_FogKeepExp");
        var exp2 = settings.FindProperty("m_FogKeepExp2");
        if (instancing == null || fog == null || linear == null || exp == null || exp2 == null)
            throw new Exception("Graphics settings missing");
        int oldInstancing = instancing.intValue;
        int oldFog = fog.intValue;
        bool oldLinear = linear.boolValue, oldExp = exp.boolValue, oldExp2 = exp2.boolValue;
        try {
            if (Environment.GetEnvironmentVariable("OPENBONK_KEEP_INSTANCING") == "1") {
                instancing.intValue = 2;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            if (Environment.GetEnvironmentVariable("OPENBONK_FOG_EXP2") == "1") {
                fog.intValue = 1; linear.boolValue = false; exp.boolValue = false; exp2.boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            var result = BuildPipeline.BuildAssetBundles(output, builds,
                BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
            if (result == null || result.GetAllAssetBundles().Length != 1)
                throw new Exception("Standalone shader build failed");
        } finally {
            settings.Update(); instancing.intValue = oldInstancing;
            fog.intValue = oldFog; linear.boolValue = oldLinear; exp.boolValue = oldExp; exp2.boolValue = oldExp2;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("OPENBONK_STANDALONE_SHADER_STUDY_OK");
    }
}
