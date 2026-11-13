using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

[InitializeOnLoad]
public static class AudioSettingsDeferredPlayCheck
{
    const string Pending = "AudioSettingsDeferredPlayCheck.pending";
    static AudioSettingsDeferredPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("audio settings check").AddComponent<AudioSettingsDeferredPlayRunner>().Begin();
        }
    }
}
public class AudioSettingsDeferredPlayRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Check());
    IEnumerator Check()
    {
        SaveManager.loaded = false;
        var saveObject = new GameObject("temporary save"); saveObject.SetActive(false);
        var save = saveObject.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile();
        var values = save.config.cfAudioSettings;
        values.master_volume = .25f; values.music = .5f; values.game_sfx = .75f; values.ambience = .4f; values.ui = .8f; values.xp_and_gold = .3f;
        var settingsObject = new GameObject("temporary settings"); settingsObject.SetActive(false);
        var settings = settingsObject.AddComponent<CurrentSettings>();
        settings.audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/AudioMixerController/GameAudioMixer.mixer");
        var seen = new HashSet<string>();
        Action<string, object, object> listener = (name, _, __) => seen.Add(name);
        CurrentSettings.A_SettingUpdated += listener;
        yield return null;
        foreach (var entry in new[] { "MasterVolume", "MusicVolume", "GameSfxVolume", "AmbienceVolume", "UiVolume" }) settings.audioMixer.SetFloat(entry, -70f);
        settingsObject.SetActive(true);
        yield return null;
        yield return null;
        bool pass = seen.SetEquals(new[] { "master_volume", "music", "game_sfx", "ambience", "ui", "xp_and_gold" });
        foreach (var entry in new[] { ("MasterVolume", .25f), ("MusicVolume", .5f), ("GameSfxVolume", .75f), ("AmbienceVolume", .4f), ("UiVolume", .8f) })
        {
            bool ok = settings.audioMixer.GetFloat(entry.Item1, out float actual) && Mathf.Abs(actual - 20f * Mathf.Log10(entry.Item2)) < .01f;
            if (!ok) Debug.LogError($"AudioSettingsDeferredPlayCheck: {entry.Item1} was {actual}");
            pass &= ok;
        }
        CurrentSettings.A_SettingUpdated -= listener;
        if (!pass) { Debug.LogError("AudioSettingsDeferredPlayCheck FAIL: saved audio was not reapplied after startup"); EditorApplication.Exit(1); yield break; }
        Debug.Log("AudioSettingsDeferredPlayCheck PASS: deferred mixer values and six audio notifications");
        EditorApplication.Exit(0);
    }
}
