using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LoadingScreenPlayCheck
{
    const string Pending = "LoadingScreenPlayCheck.pending";
    static LoadingScreenPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        var runner = new GameObject("loading screen test").AddComponent<LoadingScreenPlayRunner>();
        UnityEngine.Object.DontDestroyOnLoad(runner.gameObject); runner.Begin();
    }
}

public class LoadingScreenPlayRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Run()); }
    IEnumerator Run()
    {
        typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
        // Use an actual build scene while avoiding generation of a full run.
        typeof(LoadingScreen).GetField("sceneName", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, "MainMenu");
        SceneManager.LoadScene("LoadingScreen");
        yield return null;
        var loading = FindFirstObjectByType<LoadingScreen>();
        if (loading == null || !LoadingScreen.isLoading || loading.loadingBar == null || loading.t_loading == null)
        { Debug.LogError("LoadingScreenPlayCheck FAIL: loading scene did not initialize its UI/state"); EditorApplication.Exit(1); yield break; }
        float deadline = Time.realtimeSinceStartup + 60f;
        while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < deadline) yield return null;
        if (SceneManager.GetActiveScene().name != "MainMenu" || LoadingScreen.isLoading)
        { Debug.LogError("LoadingScreenPlayCheck FAIL: async scene activation or cleanup"); EditorApplication.Exit(1); yield break; }
        Debug.Log("LoadingScreenPlayCheck passed: real loading scene, async scene activation and loading state cleanup");
        EditorApplication.Exit(0);
    }
}
