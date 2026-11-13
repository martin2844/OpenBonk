using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class OpenBonkRenderStudy {
 public static void Build() {
  string dir=Environment.GetEnvironmentVariable("OPENBONK_RENDER_STUDY_OUT");
  if (String.IsNullOrEmpty(dir) || !Directory.Exists(dir)) throw new Exception("Missing output directory");
  string[] paths={
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
  foreach(string path in paths) if(AssetDatabase.LoadAssetAtPath<Shader>(path)==null) throw new Exception("Missing shader: "+path);
  var build=new AssetBundleBuild {assetBundleName="postprocess-study",assetNames=paths};
  var result=BuildPipeline.BuildAssetBundles(dir,new[]{build},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.Android);
  if(result==null || result.GetAllAssetBundles().Length!=1) throw new Exception("Shader study failed");
  Debug.Log("OPENBONK_RENDER_STUDY_OK "+paths.Length);
 }
}
