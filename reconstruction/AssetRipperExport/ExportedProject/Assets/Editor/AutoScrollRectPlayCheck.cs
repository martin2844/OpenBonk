using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AutoScrollRectPlayCheck
{
    const string Pending = "AutoScrollRectPlayCheck.pending";
    static AutoScrollRectPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("scroll test runner").AddComponent<AutoScrollRectPlayRunner>().Begin(); } }
}

public class AutoScrollRectPlayRunner : MonoBehaviour
{
    static void Check(bool condition, string reason) { if (!condition) throw new Exception("AutoScrollRectPlayCheck: " + reason); }
    static void UpdateScroll(AutoScrollRect scroll) => typeof(AutoScrollRect).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Test(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Test()
    {
        var eventSystem = new GameObject("event system", typeof(EventSystem)).GetComponent<EventSystem>();
        var canvas = new GameObject("canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var viewport = new GameObject("viewport", typeof(RectTransform)).GetComponent<RectTransform>(); viewport.SetParent(canvas.transform, false); viewport.sizeDelta = new Vector2(200, 200);
        var content = new GameObject("content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport, false); content.sizeDelta = new Vector2(200, 600);
        var upper = new GameObject("upper", typeof(RectTransform)).GetComponent<RectTransform>(); upper.SetParent(content, false); upper.sizeDelta = new Vector2(100, 40); upper.anchoredPosition = new Vector2(0, 150);
        var lower = new GameObject("lower", typeof(RectTransform)).GetComponent<RectTransform>(); lower.SetParent(content, false); lower.sizeDelta = new Vector2(100, 40); lower.anchoredPosition = new Vector2(0, -150);
        var outside = new GameObject("outside", typeof(RectTransform)).GetComponent<RectTransform>(); outside.SetParent(canvas.transform, false);
        var scroll = viewport.gameObject.AddComponent<AutoScrollRect>(); scroll.enabled = false; scroll.viewport = viewport; scroll.content = content;
        yield return null;
        Canvas.ForceUpdateCanvases();
        eventSystem.SetSelectedGameObject(upper.gameObject); UpdateScroll(scroll);
        Check(Mathf.Abs(content.anchoredPosition.y + 90f) < .01f && scroll.lastSelected == upper.gameObject, "selected item above viewport and padding");
        content.anchoredPosition = Vector2.zero; UpdateScroll(scroll);
        Check(content.anchoredPosition == Vector2.zero, "same selection does not scroll again");
        scroll.ManualRefresh(); Check(Mathf.Abs(content.anchoredPosition.y + 90f) < .01f, "manual refresh uses last selection");
        content.anchoredPosition = Vector2.zero;
        eventSystem.SetSelectedGameObject(lower.gameObject); UpdateScroll(scroll);
        Check(Mathf.Abs(content.anchoredPosition.y - 90f) < .01f && scroll.lastSelected == lower.gameObject, "selected item below viewport and padding");
        eventSystem.SetSelectedGameObject(outside.gameObject); content.anchoredPosition = Vector2.zero; UpdateScroll(scroll);
        Check(content.anchoredPosition == Vector2.zero && scroll.lastSelected == lower.gameObject, "outside selection ignored");
        Debug.Log("AutoScrollRectPlayCheck passed: EventSystem selection, top and bottom scroll, padding, unchanged selection and manual refresh");
        EditorApplication.Exit(0);
    }
}
