using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MusicZonePlayCheck
{
    const string Pending = "MusicZonePlayCheck.pending";
    static MusicZonePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("music zone runner").AddComponent<MusicZonePlayRunner>().Begin();
    }
}
public class MusicZonePlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        var musicGo = new GameObject("music");
        var source = musicGo.AddComponent<AudioSource>();
        var controller = musicGo.AddComponent<MusicController>();
        controller.musicSource = source;
        var playerGo = new GameObject("test player"); playerGo.SetActive(false);
        var player = playerGo.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var zoneGo = new GameObject("test music zone");
        var zone = zoneGo.AddComponent<MusicPauseZone>(); zone.radius = 10f;
        playerGo.transform.position = new Vector3(2, 0, 0);
        yield return new WaitForSeconds(.3f);
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = (Dictionary<MusicPauseZone, float>)typeof(MusicController).GetField("zoneInfluences", flags).GetValue(controller);
            if (!entries.TryGetValue(zone, out float value) || Mathf.Abs(value - .12f) > .001f) throw new Exception("Inside zone did not register 12% influence");
            float volume = (float)typeof(MusicController).GetField("zoneMultiplier", flags).GetValue(controller);
            if (volume >= .8f) throw new Exception("Music volume did not fade down inside zone: " + volume);
            ChestWindowUi.A_Open?.Invoke();
            if (Mathf.Abs((float)typeof(MusicController).GetField("desiredVolumeMultiplier", flags).GetValue(controller) - .2f) > .001f) throw new Exception("Chest opening did not lower volume");
            ChestWindowUi.A_Close?.Invoke();
            if ((float)typeof(MusicController).GetField("desiredVolumeMultiplier", flags).GetValue(controller) != 1f) throw new Exception("Chest closing did not restore volume");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        playerGo.transform.position = new Vector3(10, 0, 0);
        yield return new WaitForSeconds(.3f);
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = (Dictionary<MusicPauseZone, float>)typeof(MusicController).GetField("zoneInfluences", flags).GetValue(controller);
            if (!entries.TryGetValue(zone, out float value) || value != 1f) throw new Exception("Radius boundary did not restore influence");
            float volume = (float)typeof(MusicController).GetField("zoneMultiplier", flags).GetValue(controller);
            if (volume <= .25f) throw new Exception("Music volume did not begin recovering after exit: " + volume);
            Debug.Log("MusicZonePlayCheck passed"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
