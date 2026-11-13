using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ScoreUiPlayCheck
{
    const string Pending = "ScoreUiPlayCheck.pending";
    static ScoreUiPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("score test runner").AddComponent<ScoreUiPlayRunner>().Begin(); } }
}
public class ScoreUiPlayRunner : MonoBehaviour
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ScoreUiPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        ScoreUi ui = null; List<ScoreUiPrefab> pool = null;
        try
        {
            MyTime.time = 0f; var canvas = new GameObject("canvas", typeof(RectTransform), typeof(Canvas));
            ui = canvas.AddComponent<ScoreUi>();
            var template = new GameObject("template", typeof(RectTransform)); template.transform.SetParent(canvas.transform); template.SetActive(false);
            var popup = template.AddComponent<ScoreUiPrefab>();
            popup.t_score = new GameObject("score", typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<TextMeshProUGUI>(); popup.t_score.transform.SetParent(template.transform);
            popup.t_desc = new GameObject("description", typeof(RectTransform), typeof(CanvasRenderer)).AddComponent<TextMeshProUGUI>(); popup.t_desc.transform.SetParent(template.transform); popup.t_desc.rectTransform.sizeDelta = new Vector2(200f, 30f);
            ui.prefab = template;
            pool = (List<ScoreUiPrefab>)typeof(ScoreUi).GetField("prefabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
            ui.AddScore("first description", "+1", true, false, 1.2f); ui.AddScore("second description", "-2", false, false, 1f);
            Require(ui.GetQueueCount() == 2, "both rewards enter queue"); MyTime.time = 1f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(ui.GetQueueCount() == 1 && pool.Count == 1, "first queued reward displayed");
            Require(pool[0].gameObject.activeSelf && pool[0].t_score.text == "+1" && pool[0].t_desc.text == "first description" && Mathf.Abs(pool[0].transform.localScale.x - 1.2f) < .001f, "first popup content and scale");
            MyTime.time = 1.5f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try { Require(ui.GetQueueCount() == 0 && pool.Count == 2 && pool[1].t_score.text == "-2", "second reward waits for interval and uses another popup"); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(2.8f);
        try
        {
            Require(!pool[0].gameObject.activeSelf && !pool[1].gameObject.activeSelf, "original timed fade returns popups to pool");
            MyTime.time = 3f; ui.AddScore("third description", "+3", true, false); MyTime.time = 4f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(pool.Count == 2 && pool[0].gameObject.activeSelf && pool[0].t_score.text == "+3" && pool[0].t_desc.text == "third description", "reuses faded popup");
            Debug.Log("ScoreUiPlayCheck passed: queued rewards, timing, popup content, fade and reuse"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
