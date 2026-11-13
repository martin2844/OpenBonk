using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CinematicBarsPlayCheck {
    const string Pending = "CinematicBarsPlayCheck.pending", Path = "Assets/CinematicBarsCheckTemporary.unity";
    static CinematicBarsPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/FinalBossMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<CinematicBars>(FindObjectsInactive.Include);
        if (original == null || original.topBar == null || original.bottomBar == null || original.canvasGroup == null)
            throw new Exception("Original cinematic bars references missing");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = UnityEngine.Object.Instantiate(original.gameObject); root.SetActive(false); SceneManager.MoveGameObjectToScene(root, scene);
        var bars = root.GetComponent<CinematicBars>();
        if (!bars.topBar.transform.IsChildOf(root.transform) || !bars.bottomBar.transform.IsChildOf(root.transform) || !bars.canvasGroup.transform.IsChildOf(root.transform) && bars.canvasGroup.gameObject != root)
            throw new Exception("Original cinematic bars references are outside cloned hierarchy");
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, Path);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("bars test runner").AddComponent<CinematicBarsPlayRunner>().Begin(); } }
}
public class CinematicBarsPlayRunner : MonoBehaviour {
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("CinematicBarsPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run() {
        Application.logMessageReceived += Capture;
        var bars = UnityEngine.Object.FindAnyObjectByType<CinematicBars>(FindObjectsInactive.Include); Check(bars != null, "bars clone");
        bars.gameObject.SetActive(true); yield return null;
        bars.Show(); float height = bars.topBar.rect.height + 1f;
        Check(height > 1 && bars.IsShowing() && bars.bottomBar.gameObject.activeSelf && Mathf.Abs(bars.topBar.anchoredPosition.y-height)<.01f && Mathf.Abs(bars.bottomBar.anchoredPosition.y+height)<.01f, "show starts at scene bar height");
        yield return new WaitForSeconds(.25f);
        Check(bars.topBar.anchoredPosition.y > 0 && bars.topBar.anchoredPosition.y < height && bars.canvasGroup.alpha > 0 && bars.canvasGroup.alpha < 1, "show animates position and alpha");
        yield return new WaitForSeconds(1.1f);
        Check(Mathf.Abs(bars.topBar.anchoredPosition.y)<.01f && Mathf.Abs(bars.bottomBar.anchoredPosition.y)<.01f && Mathf.Abs(bars.canvasGroup.alpha-1)<.01f, "show completes");
        bars.Hide(); Check(bars.IsShowing(), "bars remain active during hide");
        yield return new WaitForSeconds(.25f);
        Check(bars.topBar.anchoredPosition.y > 0 && bars.topBar.anchoredPosition.y < height && bars.canvasGroup.alpha > 0 && bars.canvasGroup.alpha < 1, "hide animates position and alpha");
        yield return new WaitForSeconds(1.1f);
        Check(!bars.IsShowing() && !bars.bottomBar.gameObject.activeSelf && bars.canvasGroup.alpha < .01f, "hide completes and disables bars");
        bars.InstaShow(); Check(bars.IsShowing() && bars.bottomBar.gameObject.activeSelf && bars.topBar.anchoredPosition == Vector2.zero && bars.bottomBar.anchoredPosition == Vector2.zero, "instant show restores bars");
        yield return null; Check(Mathf.Abs(bars.canvasGroup.alpha-1)<.01f, "instant show restores alpha on update");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("CinematicBarsPlayCheck PASSED: original scene bars show, hide and instant show"); EditorApplication.Exit(0);
    }
}
