using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSettingsTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class SettingsStartupPortCheck
{
    static void Require(bool ok, string why) { if (!ok) throw new Exception("SettingsStartupPortCheck: " + why); }
    public static void Run()
    {
        int oldFps = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount, oldQueue = QualitySettings.maxQueuedFrames, oldAa = QualitySettings.antiAliasing, oldCascades = QualitySettings.shadowCascades;
        var oldShadow = QualitySettings.shadows; var oldRes = QualitySettings.shadowResolution; float oldDistance = QualitySettings.shadowDistance; bool oldSoft = QualitySettings.softParticles; var oldMode = Screen.fullScreenMode;
        var saveObj = new GameObject("test save"); saveObj.SetActive(false); var save = saveObj.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile();
        var settingsObj = new GameObject("test settings"); settingsObj.SetActive(false); var settings = settingsObj.AddComponent<CurrentSettings>(); settings.audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/AudioMixerController/GameAudioMixer.mixer");
        var cameraObj = new GameObject("test camera"); cameraObj.SetActive(false); var camera = cameraObj.AddComponent<PlayerCamera>(); camera.camera = cameraObj.AddComponent<Camera>(); camera.deathCamera = new GameObject("death camera").AddComponent<Camera>();
        int events = 0; Action<string,object,object> listener = (_, __, ___) => events++; CurrentSettings.A_SettingUpdated += listener;
        try
        {
            var defaults = new CFAudioSettings(); Require(defaults.master_volume == .69f && defaults.music == .48f && defaults.game_sfx == 1f && defaults.ambience == 1f && defaults.ui == 1f && defaults.xp_and_gold == 1f, "original new-save volumes");
            Require(settings.audioMixer != null, "original audio mixer loaded");
            save.config.cfVideoSettings.fps_limit = 72; save.config.cfVideoSettings.vsync = 0; save.config.cfVideoSettings.anti_aliasing = 1; save.config.cfGameSettings.input_delay = 3;
            settings.UpdateSave();
            foreach (var entry in new[] { ("MasterVolume", .69f), ("MusicVolume", .48f), ("GameSfxVolume", 1f), ("AmbienceVolume", 1f), ("UiVolume", 1f) })
            {
                Require(settings.audioMixer.GetFloat(entry.Item1, out float actual), "exposed mixer parameter " + entry.Item1);
                if (EditorApplication.isPlaying)
                    Require(Mathf.Abs(actual - 20f * Mathf.Log10(entry.Item2)) < .01f, "applied mixer volume " + entry.Item1 + ": " + actual);
            }
            Require(Application.targetFrameRate == 72 && QualitySettings.vSyncCount == 0 && QualitySettings.maxQueuedFrames == 3 && QualitySettings.antiAliasing == 2, "saved video and latency values applied");
            Require(events > 30, "saved settings delivered change notifications");
            var changed = typeof(PlayerCamera).GetMethod("OnSettingUpdated", BindingFlags.Instance | BindingFlags.NonPublic);
            changed.Invoke(camera, new object[] { "fov", 85f, 101.9f });
            Require(camera.camera.fieldOfView == 101 && camera.deathCamera.fieldOfView == 101, "field of view updates both original cameras");
            Debug.Log("SettingsStartupPortCheck passed: defaults, applying saved settings, notifications and camera FOV");
        }
        finally
        {
            CurrentSettings.A_SettingUpdated -= listener; Application.targetFrameRate = oldFps; QualitySettings.vSyncCount = oldVsync; QualitySettings.maxQueuedFrames = oldQueue; QualitySettings.antiAliasing = oldAa; QualitySettings.shadows = oldShadow; QualitySettings.shadowCascades = oldCascades; QualitySettings.shadowResolution = oldRes; QualitySettings.shadowDistance = oldDistance; QualitySettings.softParticles = oldSoft; Screen.fullScreenMode = oldMode;
            UnityEngine.Object.DestroyImmediate(camera.deathCamera.gameObject); UnityEngine.Object.DestroyImmediate(cameraObj); UnityEngine.Object.DestroyImmediate(settingsObj); UnityEngine.Object.DestroyImmediate(saveObj);
        }
    }
}
