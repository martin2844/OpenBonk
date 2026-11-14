using System;
using UnityEditor;
using UnityEngine;
public static class OpenBonkPostprocessStudy {
 public static void Build() {
PlayerSettings.colorSpace = ColorSpace.Linear;
        string[] renderPaths = {
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/CopyStdFromTexArray.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/GrainBaker.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/DepthOfField.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/MotionBlur.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Debug/Waveform.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Texture2DLerp.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Uber.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/DeferredFog.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/CopyStd.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/TemporalAntialiasing.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/DiscardAlpha.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/MultiScaleVO.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Lut2DBaker.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/SubpixelMorphologicalAntialiasing.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Debug/Vectorscope.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Debug/LightMeter.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Debug/Overlays.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Bloom.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Copy.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/ScalableAO.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/CopyStdFromDoubleWide.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/FinalPass.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Debug/Histogram.shader",
            "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/ScreenSpaceReflections.shader"
        };

var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
var so = new SerializedObject(settings[0]); so.Update();
so.FindProperty("m_FogStripping").intValue=1;
so.FindProperty("m_FogKeepLinear").boolValue=false;
so.FindProperty("m_FogKeepExp").boolValue=false;
so.FindProperty("m_FogKeepExp2").boolValue=true;
so.ApplyModifiedPropertiesWithoutUndo();
var build=new AssetBundleBuild { assetBundleName="postprocess-study", assetNames=renderPaths };
var output=Environment.GetEnvironmentVariable("OPENBONK_POSTPROCESS_OUTPUT");
if (String.IsNullOrEmpty(output) || !System.IO.Directory.Exists(output)) throw new Exception("Missing output directory");
var result=BuildPipeline.BuildAssetBundles(output,new[]{build},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.Android);
if(result==null) throw new Exception("build failed");
Debug.Log("OPENBONK_MINIMAL_POSTPROCESS_SUCCESS");
 }
}
