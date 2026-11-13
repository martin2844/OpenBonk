#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Connects exported post-processing settings to local Unity package shaders.</summary>
public static class OpenBonkRestorePostProcessing
{
    private const string AssetPath = "Assets/MonoBehaviour/PostProcessResources.asset";
    private const string Package = "Packages/com.unity.postprocessing/PostProcessing/Shaders/";
    private static readonly (string field, string path)[] Shaders = {
        ("autoExposure", "Builtins/AutoExposure.compute"),
        ("exposureHistogram", "Builtins/ExposureHistogram.compute"),
        ("lut3DBaker", "Builtins/Lut3DBaker.compute"),
        ("texture3dLerp", "Builtins/Texture3DLerp.compute"),
        ("gammaHistogram", "Debug/Histogram.compute"),
        ("waveform", "Debug/Waveform.compute"),
        ("vectorscope", "Debug/Vectorscope.compute"),
        ("multiScaleAODownsample1", "Builtins/MultiScaleVODownsample1.compute"),
        ("multiScaleAODownsample2", "Builtins/MultiScaleVODownsample2.compute"),
        ("multiScaleAORender", "Builtins/MultiScaleVORender.compute"),
        ("multiScaleAOUpsample", "Builtins/MultiScaleVOUpsample.compute"),
        ("gaussianDownsample", "Builtins/GaussianDownsample.compute")
    };

    public static void Run()
    {
        string backup = null;
        bool changed = false;
        try
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (!File.Exists(Path.Combine(root, ".openbonk-workspace.json")))
                throw new InvalidOperationException("This step requires an isolated OpenBonk workspace.");
            backup = Environment.GetEnvironmentVariable("OPENBONK_POSTPROCESS_BACKUP");
            if (string.IsNullOrEmpty(backup) || !Path.IsPathRooted(backup) || Directory.Exists(backup) || File.Exists(backup))
                throw new InvalidOperationException("A new absolute backup directory is required.");
            var resource = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetPath);
            if (resource == null) throw new InvalidDataException("Post-processing resource is missing.");
            var serialized = new SerializedObject(resource);
            var shaders = new ComputeShader[Shaders.Length];
            for (int i = 0; i < Shaders.Length; i++)
            {
                shaders[i] = AssetDatabase.LoadAssetAtPath<ComputeShader>(Package + Shaders[i].path);
                if (shaders[i] == null || serialized.FindProperty("computeShaders." + Shaders[i].field) == null)
                    throw new InvalidDataException("Missing post-processing compute shader or field: " + Shaders[i].field);
            }
            Directory.CreateDirectory(backup);
            File.Copy(AssetPath, Path.Combine(backup, "PostProcessResources.asset"), false);
            File.Copy(AssetPath + ".meta", Path.Combine(backup, "PostProcessResources.asset.meta"), false);
            changed = true;
            for (int i = 0; i < Shaders.Length; i++)
                serialized.FindProperty("computeShaders." + Shaders[i].field).objectReferenceValue = shaders[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(resource);
            AssetDatabase.SaveAssets();
            serialized.Update();
            for (int i = 0; i < Shaders.Length; i++)
                if (serialized.FindProperty("computeShaders." + Shaders[i].field).objectReferenceValue != shaders[i])
                    throw new InvalidDataException("Post-processing shader reference did not persist: " + Shaders[i].field);
            Debug.Log("Restored 12 post-processing compute shader references from the local Unity package.");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            if (changed)
            {
                File.Copy(Path.Combine(backup, "PostProcessResources.asset"), AssetPath, true);
                AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            }
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
