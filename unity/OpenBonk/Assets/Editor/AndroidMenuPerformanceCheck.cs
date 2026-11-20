using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public static class AndroidMenuPerformanceCheck
{
    static readonly CameraEvent[] EffectEvents = { CameraEvent.BeforeReflections, CameraEvent.BeforeLighting,
        CameraEvent.BeforeImageEffectsOpaque, CameraEvent.BeforeImageEffects };

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("AndroidMenuPerformanceCheck: " + message);
    }

    public static void Run()
    {
        var method = typeof(MainMenu).Assembly.GetType("AndroidPerformanceSettings")?.GetMethod(
            "ConfigureMenuCamera", BindingFlags.Static | BindingFlags.NonPublic);
        Check(method != null, "Could not find menu configuration");
        // Reload the actual menu after each gameplay scene: the shared profile
        // must remain intact, and each new menu camera must be configured.
        foreach (string gameplay in new[] { "GeneratedMap", "FinalBossMap" })
            CheckScene(method, gameplay);
        method.Invoke(null, new object[] { null });
        Debug.Log("AndroidMenuPerformanceCheck PASS: two fresh menu instances, live graphics settings, " +
            "post-processing command buffers removed, preview/gameplay cameras and shared profiles unchanged");
    }

    static void CheckScene(MethodInfo configure, string gameplay)
    {
        var menuScene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        var menu = menuScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainMenu>(true)).Single();
        Check(menu.menuCamera != null && menu.menuCamera.camera != null, "Main menu camera is missing");
        var camera = menu.menuCamera.camera;
        var volume = camera.GetComponent<PostProcessVolume>();
        Check(volume != null && volume.sharedProfile != null, "Menu profile is missing");
        Check(volume.sharedProfile.TryGetSettings(out DepthOfField shared) && shared.active && shared.enabled.value,
            "Expected enabled menu depth of field is missing");
        var sharedSettings = volume.sharedProfile.settings.Select(setting => EditorJsonUtility.ToJson(setting)).ToArray();
        var layer = camera.GetComponent<PostProcessLayer>();
        Check(layer != null && layer.enabled, "Expected enabled menu post processing is missing");
        Check(EffectEvents.Any(e => camera.GetCommandBuffers(e).Length > 0), "Post-processing buffers were not initialized");

        var gameplayScene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + gameplay + ".unity", OpenSceneMode.Additive);
        var otherCameras = menuScene.GetRootGameObjects().Concat(gameplayScene.GetRootGameObjects())
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Where(c => c != camera).ToArray();
        Check(otherCameras.Any(c => c.gameObject.scene == menuScene && c.targetTexture != null), "Enemy preview camera is missing");
        Check(otherCameras.Any(c => c.gameObject.scene == gameplayScene && c.GetComponent<PostProcessLayer>() != null),
            gameplay + ": gameplay post-processing camera is missing");
        var previousCameras = otherCameras.Select(c => new CameraState(c)).ToArray();
        var controller = menuScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PPController>(true)).Single();
        var previousSettingsUpdated = CurrentSettings.A_SettingUpdated;
        try
        {
            configure.Invoke(null, new object[] { camera });
            CheckConfigured(camera, layer);
            typeof(PPController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            typeof(PPController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            CurrentSettings.A_SettingUpdated?.Invoke("bloom", 0, 2);
            CurrentSettings.A_SettingUpdated?.Invoke("motion_blur", 0, 1);
            CurrentSettings.A_SettingUpdated?.Invoke("ambient_occlusion", 0, 1);
            var runtimeProfile = controller.GetComponent<PostProcessVolume>().profile;
            Check(runtimeProfile.TryGetSettings(out UnityEngine.Rendering.PostProcessing.Bloom bloom) && bloom.enabled.value && !bloom.fastMode.value &&
                runtimeProfile.TryGetSettings(out MotionBlur motion) && motion.enabled.value &&
                runtimeProfile.TryGetSettings(out AmbientOcclusion ao) && ao.enabled.value,
                "Live settings did not reach the menu profile");
            CheckConfigured(camera, layer);
            configure.Invoke(null, new object[] { camera });
            CheckConfigured(camera, layer);
            Check(sharedSettings.SequenceEqual(volume.sharedProfile.settings.Select(setting => EditorJsonUtility.ToJson(setting))),
                "The shared profile was changed");
            foreach (var state in previousCameras) state.CheckUnchanged();
        }
        finally
        {
            typeof(PPController).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            CurrentSettings.A_SettingUpdated = previousSettingsUpdated;
            EditorSceneManager.CloseScene(gameplayScene, true);
        }
    }

    static void CheckConfigured(Camera camera, PostProcessLayer layer)
    {
        Check(!layer.enabled && !camera.allowHDR && !camera.allowMSAA,
            "The menu camera still uses full-screen effects or extra buffers");
        Check(EffectEvents.All(e => camera.GetCommandBuffers(e).Length == 0), "Menu effect command buffers remain attached");
    }

    sealed class CameraState
    {
        readonly Camera camera;
        readonly bool hdr, msaa, enabled, postProcessing;
        readonly PostProcessLayer layer;
        readonly RenderTexture target;
        public CameraState(Camera camera)
        {
            this.camera = camera; hdr = camera.allowHDR; msaa = camera.allowMSAA; enabled = camera.enabled;
            layer = camera.GetComponent<PostProcessLayer>(); postProcessing = layer != null && layer.enabled;
            target = camera.targetTexture;
        }
        public void CheckUnchanged()
        {
            Check(camera.allowHDR == hdr && camera.allowMSAA == msaa && camera.enabled == enabled && camera.targetTexture == target &&
                (layer != null && layer.enabled) == postProcessing, camera.name + ": unrelated camera was changed");
        }
    }
}
