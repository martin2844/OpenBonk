using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OpenBonkBuiltinShaderProbe {
 public static void Build() {
  string output = Environment.GetEnvironmentVariable("OPENBONK_BUILTIN_PROBE_OUT");
  const string root = "Assets/OpenBonkBuiltinProbeTemp";
  if (String.IsNullOrEmpty(output) || !Directory.Exists(output)) throw new Exception("Missing output");
  if (AssetDatabase.IsValidFolder(root) || Directory.Exists(root)) throw new Exception("Temporary folder exists");
  try {
   AssetDatabase.CreateFolder("Assets", "OpenBonkBuiltinProbeTemp");
   string[] names = { "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Legacy Shaders/Particles/Alpha Blended", "Unlit/Texture" };
   string[][] keywords = { Array.Empty<string>(), new[]{"_ALPHATEST_ON"}, new[]{"_ALPHABLEND_ON"}, new[]{"_ALPHAPREMULTIPLY_ON"}, new[]{"_SPECULARHIGHLIGHTS_OFF"}, new[]{"_NORMALMAP","_PARALLAXMAP","_SPECULARHIGHLIGHTS_OFF"}, new[]{"_ALPHAPREMULTIPLY_ON","_SPECULARHIGHLIGHTS_OFF","_GLOSSYREFLECTIONS_OFF"}, new[]{"_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A","_SPECULARHIGHLIGHTS_OFF","_GLOSSYREFLECTIONS_OFF"}, Array.Empty<string>(), Array.Empty<string>() };
   string[] files = new string[names.Length];
   for (int i=0; i<names.Length; i++) {
    var shader=Shader.Find(names[i]); if(shader==null)throw new Exception("Missing shader: "+names[i]);
    var material=new Material(shader);files[i]=root+"/builtin"+i+".mat";AssetDatabase.CreateAsset(material,files[i]);
    foreach(var keyword in keywords[i])material.EnableKeyword(keyword);
    EditorUtility.SetDirty(material);
    Debug.Log("OPENBONK_BUILTIN_SOURCE "+names[i]+" "+AssetDatabase.GetAssetPath(shader));
   }
   AssetDatabase.SaveAssets();
   var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");var so=new SerializedObject(settings[0]);so.Update();
   var stripping=so.FindProperty("m_FogStripping");var linear=so.FindProperty("m_FogKeepLinear");var exp=so.FindProperty("m_FogKeepExp");var exp2=so.FindProperty("m_FogKeepExp2");var instance=so.FindProperty("m_InstancingStripping");
   int oldStrip=stripping.intValue,oldInstance=instance.intValue;bool oldLinear=linear.boolValue,oldExp=exp.boolValue,oldExp2=exp2.boolValue;
   try {
    stripping.intValue=1;linear.boolValue=false;exp.boolValue=false;exp2.boolValue=true;instance.intValue=2;so.ApplyModifiedPropertiesWithoutUndo();
    var bundle=new AssetBundleBuild { assetBundleName="builtin-probe",assetNames=files };
    var result=BuildPipeline.BuildAssetBundles(output,new[]{bundle},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.Android);
    if(result==null || result.GetAllAssetBundles().Length!=1)throw new Exception("Build failed");
   } finally {
    so.Update();stripping.intValue=oldStrip;linear.boolValue=oldLinear;exp.boolValue=oldExp;exp2.boolValue=oldExp2;instance.intValue=oldInstance;so.ApplyModifiedPropertiesWithoutUndo();
   }
   Debug.Log("OPENBONK_BUILTIN_PROBE_OK");
  } finally { AssetDatabase.DeleteAsset(root); }
 }
}
