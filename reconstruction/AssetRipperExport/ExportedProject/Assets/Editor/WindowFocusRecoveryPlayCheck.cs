using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class WindowFocusRecoveryPlayCheck {
    const string Pending = "WindowFocusRecoveryPlayCheck.pending";
    static WindowFocusRecoveryPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("focus recovery runner").AddComponent<WindowFocusRecoveryPlayRunner>().Begin(); } }
}
public class FocusRecoveryButton : MyButton { public override void StartHover() {} public override void StopHover() {} protected override void OnClick() {} }
public class WindowFocusRecoveryPlayRunner : MonoBehaviour {
    static void Check(bool ok, string message) { if (!ok) throw new Exception("WindowFocusRecoveryPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    static FocusRecoveryButton MakeButton(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent); return go.AddComponent<FocusRecoveryButton>(); }
    IEnumerator Run() {
        new GameObject("event system").AddComponent<EventSystem>();
        var host = new GameObject("window"); host.SetActive(false); var window = host.AddComponent<Window>();
        var first = MakeButton("first", host.transform); var last = MakeButton("last", host.transform); window.startBtn = first; host.SetActive(true); yield return null;
        var recover = typeof(Window).GetMethod("RecoverButtonFocus", BindingFlags.Instance | BindingFlags.NonPublic); Check(recover != null, "recovery method exists");
        ButtonManager.SetNull(); recover.Invoke(window, null); Check(ButtonManager.GetCurrentButton() == first && EventSystem.current.currentSelectedGameObject == first.gameObject, "start button restored");
        ButtonManager.ForceHoverButton(last); recover.Invoke(window, null); Check(ButtonManager.GetCurrentButton() == last, "existing window selection preserved");
        ButtonManager.SetNull(); first.gameObject.SetActive(false); recover.Invoke(window, null); Check(ButtonManager.GetCurrentButton() == last, "inactive start button falls back to active window button");
        ButtonManager.SetNull(); last.gameObject.SetActive(false); recover.Invoke(window, null); Check(ButtonManager.GetCurrentButton() == null, "no selection when no active buttons");
        Debug.Log("WindowFocusRecoveryPlayCheck PASSED: start, existing selection and fallback"); EditorApplication.Exit(0);
    }
}
