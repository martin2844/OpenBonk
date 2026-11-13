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
    private static readonly (string field, string path)[] ComputeShaders = {
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
    private static readonly (string field, string path)[] RasterShaders = {
        ("bloom", "Builtins/Bloom.shader"),
        ("copy", "Builtins/Copy.shader"),
        ("copyStd", "Builtins/CopyStd.shader"),
        ("copyStdFromTexArray", "Builtins/CopyStdFromTexArray.shader"),
        ("copyStdFromDoubleWide", "Builtins/CopyStdFromDoubleWide.shader"),
        ("discardAlpha", "Builtins/DiscardAlpha.shader"),
        ("depthOfField", "Builtins/DepthOfField.shader"),
        ("finalPass", "Builtins/FinalPass.shader"),
        ("grainBaker", "Builtins/GrainBaker.shader"),
        ("motionBlur", "Builtins/MotionBlur.shader"),
        ("temporalAntialiasing", "Builtins/TemporalAntialiasing.shader"),
        ("subpixelMorphologicalAntialiasing", "Builtins/SubpixelMorphologicalAntialiasing.shader"),
        ("texture2dLerp", "Builtins/Texture2DLerp.shader"),
        ("uber", "Builtins/Uber.shader"),
        ("lut2DBaker", "Builtins/Lut2DBaker.shader"),
        ("lightMeter", "Debug/LightMeter.shader"),
        ("gammaHistogram", "Debug/Histogram.shader"),
        ("waveform", "Debug/Waveform.shader"),
        ("vectorscope", "Debug/Vectorscope.shader"),
        ("debugOverlays", "Debug/Overlays.shader"),
        ("deferredFog", "Builtins/DeferredFog.shader"),
        ("scalableAO", "Builtins/ScalableAO.shader"),
        ("multiScaleAO", "Builtins/MultiScaleVO.shader"),
        ("screenSpaceReflections", "Builtins/ScreenSpaceReflections.shader")
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
            var compute = new ComputeShader[ComputeShaders.Length];
            for (int i = 0; i < ComputeShaders.Length; i++)
            {
                compute[i] = AssetDatabase.LoadAssetAtPath<ComputeShader>(Package + ComputeShaders[i].path);
                if (compute[i] == null || serialized.FindProperty("computeShaders." + ComputeShaders[i].field) == null)
                    throw new InvalidDataException("Missing post-processing compute shader or field: " + ComputeShaders[i].field);
            }
            var raster = new Shader[RasterShaders.Length];
            for (int i = 0; i < RasterShaders.Length; i++)
            {
                raster[i] = AssetDatabase.LoadAssetAtPath<Shader>(Package + RasterShaders[i].path);
                if (raster[i] == null || serialized.FindProperty("shaders." + RasterShaders[i].field) == null)
                    throw new InvalidDataException("Missing post-processing shader or field: " + RasterShaders[i].field);
            }
            Directory.CreateDirectory(backup);
            File.Copy(AssetPath, Path.Combine(backup, "PostProcessResources.asset"), false);
            File.Copy(AssetPath + ".meta", Path.Combine(backup, "PostProcessResources.asset.meta"), false);
            changed = true;
            for (int i = 0; i < ComputeShaders.Length; i++)
                serialized.FindProperty("computeShaders." + ComputeShaders[i].field).objectReferenceValue = compute[i];
            for (int i = 0; i < RasterShaders.Length; i++)
                serialized.FindProperty("shaders." + RasterShaders[i].field).objectReferenceValue = raster[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(resource);
            AssetDatabase.SaveAssets();
            serialized.Update();
            for (int i = 0; i < ComputeShaders.Length; i++)
                if (serialized.FindProperty("computeShaders." + ComputeShaders[i].field).objectReferenceValue != compute[i])
                    throw new InvalidDataException("Post-processing compute reference did not persist: " + ComputeShaders[i].field);
            for (int i = 0; i < RasterShaders.Length; i++)
                if (serialized.FindProperty("shaders." + RasterShaders[i].field).objectReferenceValue != raster[i])
                    throw new InvalidDataException("Post-processing shader reference did not persist: " + RasterShaders[i].field);
            Debug.Log("Restored 24 post-processing shaders and 12 compute shaders from the local Unity package.");
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
