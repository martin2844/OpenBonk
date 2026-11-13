using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class PostProcessingSettingsPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Invoke(PPController controller, string name) => typeof(PPController).GetMethod(name, Private).Invoke(controller, null);
    public static void Run()
    {
        var previous = CurrentSettings.A_SettingUpdated;
        var instance = typeof(SaveManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        var oldManager = SaveManager.Instance;
        var managerObject = new GameObject("Postprocessing settings test save");
        managerObject.SetActive(false);
        var manager = managerObject.AddComponent<SaveManager>();
        manager.config = new ConfigSaveFile();
        manager.config.cfVideoSettings.bloom = 2;
        manager.config.cfVideoSettings.motion_blur = 1;
        manager.config.cfVideoSettings.ambient_occlusion = 0;
        instance.GetSetMethod(true).Invoke(null, new object[] { manager });
        try
        {
            foreach (string name in new[] { "MainMenu", "GeneratedMap", "FinalBossMap" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
                var controller = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PPController>(true)).Single();
                var volume = controller.GetComponent<PostProcessVolume>();
                Check(volume != null && volume.profile != null, name + ": missing profile");
                Check(volume.profile.TryGetSettings(out UnityEngine.Rendering.PostProcessing.Bloom bloom), name + ": missing bloom");
                Check(volume.profile.TryGetSettings(out MotionBlur motion), name + ": missing motion blur");
                Check(volume.profile.TryGetSettings(out AmbientOcclusion ao), name + ": missing AO");
                Invoke(controller, "Awake");
                Invoke(controller, "Start");
                Check(bloom.enabled.value && !bloom.fastMode.value && motion.enabled.value && !ao.enabled.value, name + ": saved settings");
                CurrentSettings.A_SettingUpdated?.Invoke("bloom", 2, 1);
                CurrentSettings.A_SettingUpdated?.Invoke("motion_blur", 1, 0);
                CurrentSettings.A_SettingUpdated?.Invoke("ambient_occlusion", 0, 1);
                Check(bloom.enabled.value && bloom.fastMode.value && !motion.enabled.value && ao.enabled.value, name + ": setting events");
                CurrentSettings.A_SettingUpdated?.Invoke("bloom", 1, 0);
                Check(!bloom.enabled.value, name + ": bloom off");
                CurrentSettings.A_SettingUpdated?.Invoke("bloom", 0, 2);
                Check(bloom.enabled.value && !bloom.fastMode.value, name + ": bloom fancy");
                Invoke(controller, "OnDestroy");
                CurrentSettings.A_SettingUpdated?.Invoke("bloom", 2, 0);
                Check(bloom.enabled.value, name + ": unsubscribe");
            }
            Debug.Log("PostProcessingSettingsPortCheck passed: three scene profiles, saved settings, live changes, bloom modes, unsubscribe");
        }
        finally
        {
            CurrentSettings.A_SettingUpdated = previous;
            instance.GetSetMethod(true).Invoke(null, new object[] { oldManager });
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
