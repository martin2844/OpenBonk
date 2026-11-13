using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SettingsStartupPlayCheck
{
    const string Pending = "SettingsStartupPlayCheck.pending";
    static SettingsStartupPlayCheck() { EditorApplication.playModeStateChanged += State; }
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
        new GameObject("settings check runner").AddComponent<SettingsStartupPlayRunner>().Begin();
    }
}
public class SettingsStartupPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        yield return null;
        try { SettingsStartupPortCheck.Run(); Debug.Log("SettingsStartupPlayCheck passed"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
