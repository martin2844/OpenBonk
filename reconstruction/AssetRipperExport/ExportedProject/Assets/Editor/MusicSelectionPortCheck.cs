using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Audio.Music;
using Assets.Scripts.Game.Other;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MusicSelectionPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static MusicTrack Track(string name, bool enabled, bool jukebox)
    {
        var track = ScriptableObject.CreateInstance<MusicTrack>();
        track.trackName = name; track.isEnabled = enabled; track.isInJukebox = jukebox;
        track.loop = AudioClip.Create(name, 1000, 1, 1000, false); return track;
    }
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
            var ui = UnityEngine.Object.FindAnyObjectByType<MusicSelectionUi>(FindObjectsInactive.Include);
            Check(ui != null && ui.t_trackName != null && ui.mapSelection != null, "original selector references");
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int bindings = 0;
            foreach (var b in buttons) for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentTarget(i) == ui && b.onClick.GetPersistentMethodName(i) == "Flip") bindings++;
            Check(bindings == 2, "original left and right button bindings");
            var dataObj = new GameObject("music data"); dataObj.SetActive(false);
            var data = dataObj.AddComponent<DataManager>(); DataManager.Instance = data;
            var first = Track("first", true, true); var second = Track("second", true, true); var hidden = Track("hidden", true, false);
            data.unsortedMusic = new List<MusicTrack> { first, second, hidden };
            var audioObj = new GameObject("music audio"); audioObj.SetActive(false);
            var controller = audioObj.AddComponent<MusicController>();
            controller.musicSource = audioObj.AddComponent<AudioSource>(); controller.menuMusicTrack = Track("menu", true, false); MusicController.Instance = controller;
            ui.mapSelection.runConfig = new RunConfig();
            var t = typeof(MusicSelectionUi);
            t.GetField("index", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1);
            t.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            t.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Check(ui.t_trackName.text == "Random" && ui.mapSelection.runConfig.musicTrackIndex == -1 && controller.musicSource.clip == controller.menuMusicTrack.loop, "initial random and menu music");
            ui.Flip(-1); Check(ui.mapSelection.runConfig.musicTrackIndex == -1, "lower bound");
            ui.Flip(1); Check(ui.t_trackName.text == "first" && ui.mapSelection.runConfig.musicTrackIndex == 0 && controller.musicSource.clip == first.loop, "first track preview");
            ui.Flip(1); Check(ui.t_trackName.text == "second" && ui.mapSelection.runConfig.musicTrackIndex == 1 && controller.musicSource.clip == second.loop, "second track preview");
            ui.Flip(1); Check(ui.mapSelection.runConfig.musicTrackIndex == 1, "upper bound and excluded track");
            ui.mapSelection.runConfig = new RunConfig(); MapSelectionUi.A_MapSelected?.Invoke(null, null);
            Check(ui.mapSelection.runConfig.musicTrackIndex == 1 && controller.musicSource.clip == controller.menuMusicTrack.loop, "map selection retains choice and resumes menu music");
            ui.Flip(-1); ui.Flip(-1); Check(ui.t_trackName.text == "Random" && ui.mapSelection.runConfig.musicTrackIndex == -1, "return to random");
            t.GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            ui.mapSelection.runConfig.musicTrackIndex = 22; MapSelectionUi.A_MapSelected?.Invoke(null, null);
            Check(ui.mapSelection.runConfig.musicTrackIndex == 22, "map listener cleanup");
            Debug.Log("MusicSelectionPortCheck passed: actual menu bindings, random, filtered track previews, boundaries, map change and cleanup");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
