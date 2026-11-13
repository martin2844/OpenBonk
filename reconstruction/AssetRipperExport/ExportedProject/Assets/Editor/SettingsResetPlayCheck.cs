using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.UI.DynamicWindows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SettingsResetPlayCheck
{
    const string Pending = "SettingsResetPlayCheck.pending";
    static SettingsResetPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) {
            SessionState.SetBool(Pending, false);
            new GameObject("settings reset check").AddComponent<SettingsResetPlayRunner>().Begin();
        }
    }
}

public class SettingsResetPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Check());
    IEnumerator Check()
    {
        SaveManager.loaded = false; // No writes to a real save.
        var save = new GameObject("temporary save").AddComponent<SaveManager>();
        save.config = new ConfigSaveFile(); save.config.Init();
        save.progression = new ProgressionSaveFile();
        var originalProgression = save.progression;
        var oldConfig = save.config; oldConfig.cfVideoSettings.fov = 120f;
        var runtimeObject = new GameObject("temporary current settings"); runtimeObject.SetActive(false);
        var runtime = runtimeObject.AddComponent<CurrentSettings>();
        runtime.audioMixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/AudioMixerController/GameAudioMixer.mixer");
        runtimeObject.SetActive(true);
        var audioObject = new GameObject("temporary audio manager"); audioObject.SetActive(false);
        var audioManager = audioObject.AddComponent<AudioManager>(); AudioManager.Instance = audioManager;
        var soundObject = new GameObject("reset sound");
        var audioSource = soundObject.AddComponent<AudioSource>(); var sound = soundObject.AddComponent<RandomSfx>();
        sound.s = audioSource; sound.sounds = new[] { AudioClip.Create("reset sound", 4410, 1, 44100, false) };
        sound.minPitch = sound.maxPitch = 1f; audioManager.uiAbort = sound;
        var ownerObject = new GameObject("settings owner"); ownerObject.SetActive(false);
        var owner = ownerObject.AddComponent<Settings>(); owner.settings = new List<BetterSetting>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SliderSetting.prefab");
        var widgetObject = Instantiate(prefab); var widget = widgetObject.GetComponent<SliderSetting>();
        widget.SetSetting(runtime.BetterUpdateCfSettings, "fov", 120f, owner, oldConfig.cfVideoSettings);
        owner.settings.Add(widget);
        var uiObject = new GameObject("temporary always UI"); uiObject.SetActive(false);
        var ui = uiObject.AddComponent<AlwaysUi>(); AlwaysUi.Instance = ui;
        var windowsObject = new GameObject("temporary windows", typeof(RectTransform)); windowsObject.SetActive(false);
        var windows = windowsObject.AddComponent<DynamicWindows>(); windows.contentParent = windowsObject.transform;
        windows.overlay = new GameObject("overlay", typeof(RectTransform)).transform;
        windows.dynamicWindowPromptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DynamicWindowPrompt.prefab");
        ui.dynamicWindows = windows;
        yield return null;
        owner.TryResetSaveFile();
        var prompt = windowsObject.GetComponentInChildren<DWindowPrompt>(true);
        bool shown = prompt != null && !string.IsNullOrEmpty(prompt.t_content.text);
        if (prompt != null) prompt.Close();
        yield return null;
        bool cancelled = save.config == oldConfig && oldConfig.cfVideoSettings.fov == 120f && audioSource.clip == null;
        owner.TryResetSaveFile();
        prompt = windowsObject.GetComponentInChildren<DWindowPrompt>(true);
        if (prompt != null) prompt.Accept();
        float defaultFov = new ConfigSaveFile().cfVideoSettings.fov;
        bool reset = save.config != oldConfig && save.config.cfVideoSettings.fov == defaultFov && widget.slider.value == defaultFov;
        bool progressKept = save.progression == originalProgression;
        widget.ControllerInputDir(1, 1f);
        bool rebound = save.config.cfVideoSettings.fov == defaultFov + 1f && oldConfig.cfVideoSettings.fov == 120f;
        bool soundPlayed = audioSource.clip == sound.sounds[0];
        bool ok = shown && cancelled && reset && progressKept && rebound && soundPlayed;
        Debug.Log($"SettingsResetPlayCheck {(ok ? "PASS" : "FAIL")}: shown={shown} cancelled={cancelled} reset={reset} progressKept={progressKept} rebound={rebound} soundSelected={soundPlayed} defaultFov={defaultFov}");
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
