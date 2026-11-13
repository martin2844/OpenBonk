using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OpenBonkComputeStudy {
    public static void Build() {
        string dir = Environment.GetEnvironmentVariable("OPENBONK_COMPUTE_STUDY_OUT");
        if (String.IsNullOrEmpty(dir) || !Directory.Exists(dir)) throw new Exception("Missing isolated output directory");
        string prefix = "Packages/com.unity.postprocessing/PostProcessing/Shaders/";
        string[] names = {"AutoExposure", "MultiScaleVORender", "MultiScaleVOUpsample", "MultiScaleVODownsample1", "MultiScaleVODownsample2", "Lut3DBaker", "Histogram", "Texture3DLerp", "GaussianDownsample", "ExposureHistogram", "Waveform", "Vectorscope"};
        var builds = new AssetBundleBuild[names.Length];
        for (int i = 0; i < names.Length; i++) {
            string category = names[i] == "Histogram" || names[i] == "Waveform" || names[i] == "Vectorscope" ? "Debug/" : "Builtins/";
            string path = prefix + category + names[i] + ".compute";
            if (AssetDatabase.LoadAssetAtPath<ComputeShader>(path) == null) throw new Exception("Missing package shader: " + path);
            builds[i] = new AssetBundleBuild { assetBundleName = names[i].ToLowerInvariant(), assetNames = new[] { path } };
        }
        var manifest = BuildPipeline.BuildAssetBundles(dir, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
        if (manifest == null || manifest.GetAllAssetBundles().Length != names.Length) throw new Exception("Compute study build failed");
        Debug.Log("OPENBONK_COMPUTE_STUDY_OK " + names.Length);
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
        foreach (string path in renderPaths) if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null) throw new Exception("Missing package shader: " + path);
        var renderBuild = new AssetBundleBuild { assetBundleName = "postprocess-study", assetNames = renderPaths };
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (settings.Length == 0) throw new Exception("Graphics settings asset missing");
        var so = new SerializedObject(settings[0]); so.Update();
        var stripping = so.FindProperty("m_FogStripping"); var linear = so.FindProperty("m_FogKeepLinear");
        var exp = so.FindProperty("m_FogKeepExp"); var exp2 = so.FindProperty("m_FogKeepExp2");
        if (stripping == null || linear == null || exp == null || exp2 == null) throw new Exception("Fog settings missing");
        int oldStrip = stripping.intValue; bool oldLinear = linear.boolValue, oldExp = exp.boolValue, oldExp2 = exp2.boolValue;
        try {
            stripping.intValue = 1; linear.boolValue = false; exp.boolValue = false; exp2.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
            var renderManifest = BuildPipeline.BuildAssetBundles(dir, new[] { renderBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
            if (renderManifest == null || renderManifest.GetAllAssetBundles().Length != 1) throw new Exception("Render study build failed");
            string[] localPaths = {
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
            foreach (string path in localPaths) if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null) throw new Exception("Missing local shader: " + path);
            var localBuild = new AssetBundleBuild { assetBundleName = "localshader-study", assetNames = localPaths };
            var localManifest = BuildPipeline.BuildAssetBundles(dir, new[] { localBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
            if (localManifest == null || localManifest.GetAllAssetBundles().Length != 1) throw new Exception("Local shader study failed");
            Debug.Log("OPENBONK_LOCAL_STUDY_OK " + localPaths.Length);
            var instancing = so.FindProperty("m_InstancingStripping");
            if (instancing == null) throw new Exception("Instancing setting missing");
            int oldInstancing = instancing.intValue;
            try {
                instancing.intValue = 2; so.ApplyModifiedPropertiesWithoutUndo();
                var instanceBuild = new AssetBundleBuild { assetBundleName = "localshader-instance", assetNames = localPaths };
                var instanceManifest = BuildPipeline.BuildAssetBundles(dir, new[] { instanceBuild }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
                if (instanceManifest == null || instanceManifest.GetAllAssetBundles().Length != 1) throw new Exception("Instancing shader study failed");
                Debug.Log("OPENBONK_INSTANCE_STUDY_OK " + localPaths.Length);
                BuildBuiltinVariants(dir);
            } finally {
                so.Update(); instancing.intValue = oldInstancing; so.ApplyModifiedPropertiesWithoutUndo();
            }
            BuildFeatureVariants(dir);


        } finally {
            so.Update(); stripping.intValue = oldStrip; linear.boolValue = oldLinear; exp.boolValue = oldExp; exp2.boolValue = oldExp2; so.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("OPENBONK_RENDER_STUDY_OK " + renderPaths.Length);

    }

    private static void BuildFeatureVariants(string output) {
        const string root = "Assets/OpenBonkShaderFeatureProbeTemp";
        if (AssetDatabase.IsValidFolder(root) || Directory.Exists(root)) throw new Exception("Temporary material path already exists");
        try {
            AssetDatabase.CreateFolder("Assets", "OpenBonkShaderFeatureProbeTemp");
            string a = "Assets/Shader/TextMeshPro_Distance Field Overlay.shader", b = "Assets/Shader/Native_Cubemap_Skybox.shader";
            var text = AssetDatabase.LoadAssetAtPath<Shader>(a); var skyShader = AssetDatabase.LoadAssetAtPath<Shader>(b);
            if (text == null || skyShader == null) throw new Exception("Missing feature shader");
            var underlay = new Material(text); underlay.EnableKeyword("UNDERLAY_ON");
            var sky = new Material(skyShader); sky.EnableKeyword("_ENABLEROTATION_ON"); sky.EnableKeyword("_ENABLEFOG_ON");
            string ma = root + "/underlay.mat", mb = root + "/sky.mat";
            AssetDatabase.CreateAsset(underlay, ma); AssetDatabase.CreateAsset(sky, mb);
            var build = new AssetBundleBuild { assetBundleName = "feature-probe", assetNames = new[] { a, b, ma, mb } };
            var result = BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.Android);
            if (result == null || result.GetAllAssetBundles().Length != 1) throw new Exception("Feature shader build failed");
            Debug.Log("OPENBONK_SHADER_FEATURE_PROBE_OK");
        } finally { AssetDatabase.DeleteAsset(root); }
    }

    private static void BuildBuiltinVariants(string output) {
        const string root = "Assets/OpenBonkBuiltinProbeTemp";
        if (AssetDatabase.IsValidFolder(root) || Directory.Exists(root)) throw new Exception("Temporary built-in material path already exists");
        try {
            AssetDatabase.CreateFolder("Assets", "OpenBonkBuiltinProbeTemp");
            string[] names = { "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", "Legacy Shaders/Particles/Alpha Blended", "Unlit/Texture" };
            string[][] keywords = { Array.Empty<string>(), new[]{"_ALPHATEST_ON"}, new[]{"_ALPHABLEND_ON"}, new[]{"_ALPHAPREMULTIPLY_ON"}, new[]{"_SPECULARHIGHLIGHTS_OFF"}, new[]{"_NORMALMAP","_PARALLAXMAP","_SPECULARHIGHLIGHTS_OFF"}, new[]{"_ALPHAPREMULTIPLY_ON","_SPECULARHIGHLIGHTS_OFF","_GLOSSYREFLECTIONS_OFF"}, new[]{"_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A","_SPECULARHIGHLIGHTS_OFF","_GLOSSYREFLECTIONS_OFF"}, Array.Empty<string>(), Array.Empty<string>() };
            string[] files = new string[names.Length];
            for (int i=0;i<names.Length;i++) {
                var shader=Shader.Find(names[i]); if(shader==null)throw new Exception("Missing built-in shader: "+names[i]);
                var material=new Material(shader); foreach(var keyword in keywords[i])material.EnableKeyword(keyword);
                files[i]=root+"/builtin"+i+".mat"; AssetDatabase.CreateAsset(material,files[i]);
            }
            var build=new AssetBundleBuild { assetBundleName="builtin-probe",assetNames=files };
            var result=BuildPipeline.BuildAssetBundles(output,new[]{build},BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.Android);
            if(result==null || result.GetAllAssetBundles().Length!=1)throw new Exception("Built-in shader build failed");
            Debug.Log("OPENBONK_BUILTIN_PROBE_OK");
        } finally { AssetDatabase.DeleteAsset(root); }
    }
}
