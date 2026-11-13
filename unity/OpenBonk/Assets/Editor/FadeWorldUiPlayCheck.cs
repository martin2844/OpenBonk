using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FadeWorldUiPlayCheck
{
    const string Pending = "FadeWorldUiPlayCheck.pending";
    static FadeWorldUiPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("fade test").AddComponent<FadeWorldUiPlayRunner>().Begin();
        }
    }
}

public class FadeWorldUiPlayRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("FadeWorldUiPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BanishItemWui.prefab");
        if (prefab == null) { Debug.LogError("FadeWorldUiPlayCheck: prefab missing"); EditorApplication.Exit(1); yield break; }
        var obj = Instantiate(prefab); var fade = obj.GetComponentInChildren<FadeWorldUi>(true);
        try
        {
            Require(fade != null && fade.damageText != null && fade.otherText != null, "prefab bindings");
            Require(Mathf.Approximately(fade.startFadeoutTime, .5f), "serialized fade delay");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield return null;
        Vector3 firstPosition = fade.transform.position;
        yield return new WaitForSeconds(.2f);
        try
        {
            Require(fade != null && Vector3.Distance(firstPosition, fade.transform.position) > .001f, "text did not move");
            var defaultScale = (Vector3)typeof(FadeWorldUi).GetField("defaultScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fade);
            Require(fade.transform.localScale.magnitude > defaultScale.magnitude * .1f, "text did not grow beyond initial scale");
            var speed = (float)typeof(FadeWorldUi).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fade);
            Require(speed < 8f && speed > 0f, "speed did not decay");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield return new WaitForSeconds(1.2f);
        if (fade != null) { Debug.LogError("FadeWorldUiPlayCheck: object not destroyed"); EditorApplication.Exit(1); yield break; }
        Debug.Log("FadeWorldUiPlayCheck passed: original prefab starts, moves, scales and expires");
        EditorApplication.Exit(0);
    }
}
