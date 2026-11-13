using System;
using System.IO;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine;
public static class OpenBonkComputeStudy {
 public static void Build() {
  string dir=Environment.GetEnvironmentVariable("OPENBONK_INSTANCE_STUDY_OUT");
  if (String.IsNullOrEmpty(dir) || !Directory.Exists(dir)) throw new Exception("Missing output directory");
  string[] paths={
            "Assets/Shader/Projector_AdditiveTint.shader",
            "Assets/Shader/Toon_Lit Tri Planar Noise Blend.shader",
            "Assets/Shader/Toon_Lit Tri Planar Normal.shader",
            "Assets/Shader/Archanor VFX_Retro Arsenal_BasicGlowUI.shader",
            "Assets/Shader/Custom_UIRainbowShader.shader",
            "Assets/Shader/Custom_MinimapIconInstancedCutoutColor.shader",
            "Assets/Shader/BadDog_BGForceField.shader",
            "Assets/Shader/Custom_UnlitTransparentBlended.shader",
            "Assets/Shader/IL3DN_WaterOptimized.shader",
            "Assets/Shader/Custom_GrassShaderOptimized.shader",
            "Assets/Shader/Custom_WhiteSilhouette.shader",
            "Assets/Shader/TextMeshPro_Distance Field Overlay.shader",
            "Assets/Shader/Custom_WorldIconInstancedCutoutColor.shader",
            "Assets/Shader/Sprites_Diffuse Emit.shader",
            "Assets/Shader/Unlit_AlwaysOnTopUI.shader",
            "Assets/Shader/Native_Cubemap_Skybox.shader",
            "Assets/Shader/Projector_Multiply.shader"
  };
  foreach(string path in paths) if(AssetDatabase.LoadAssetAtPath<Shader>(path)==null) throw new Exception("Missing shader: "+path);
  var build=new AssetBundleBuild {assetBundleName="postprocess-study",assetNames=paths};
  var assets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
  if(assets.Length==0) throw new Exception("Graphics settings asset missing");
  var so=new SerializedObject(assets[0]);so.Update();
  var stripping=so.FindProperty("m_FogStripping");var linear=so.FindProperty("m_FogKeepLinear");var exp=so.FindProperty("m_FogKeepExp");var exp2=so.FindProperty("m_FogKeepExp2");
  var instancing=so.FindProperty("m_InstancingStripping");
  if(instancing==null || stripping==null || linear==null || exp==null || exp2==null) throw new Exception("Fog settings missing");
  int oldInstancing=instancing.intValue;int oldStrip=stripping.intValue;bool oldLinear=linear.boolValue,oldExp=exp.boolValue,oldExp2=exp2.boolValue;
  try {
   instancing.intValue=2;stripping.intValue=1;linear.boolValue=false;exp.boolValue=false;exp2.boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
   var result=BuildPipeline.BuildAssetBundles(dir,new[]{build},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.Android);
   if(result==null || result.GetAllAssetBundles().Length!=1) throw new Exception("Shader study failed");
  } finally {
   so.Update();instancing.intValue=oldInstancing;stripping.intValue=oldStrip;linear.boolValue=oldLinear;exp.boolValue=oldExp;exp2.boolValue=oldExp2;so.ApplyModifiedPropertiesWithoutUndo();
  }
  Debug.Log("OPENBONK_INSTANCE_STUDY_OK "+paths.Length);
 }
}
