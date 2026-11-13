using System;
using System.Collections;
using Assets.Scripts.UI.Menu.Windows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AdvancedSettingsNavPlayCheck
{
    const string Pending = "AdvancedSettingsNavPlayCheck.pending";
    static AdvancedSettingsNavPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("navigation test runner").AddComponent<AdvancedSettingsNavPlayRunner>().Begin(); } }
}

public class AdvancedSettingsNavPlayRunner : MonoBehaviour
{
    static void Check(bool condition, string reason) { if (!condition) throw new Exception("AdvancedSettingsNavPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Test(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Test()
    {
        var canvas = new GameObject("canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var viewport = new GameObject("viewport", typeof(RectTransform)).GetComponent<RectTransform>(); viewport.SetParent(canvas.transform, false); viewport.sizeDelta = new Vector2(200, 200); viewport.gameObject.SetActive(false);
        var content = new GameObject("content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport, false); content.sizeDelta = new Vector2(200, 600);
        var top = new GameObject("tab", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(); top.transform.SetParent(canvas.transform, false);
        Button Make(string name, float y) { var b = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(); b.transform.SetParent(content, false); var r = (RectTransform)b.transform; r.sizeDelta = new Vector2(100, 40); r.anchoredPosition = new Vector2(0, y); return b; }
        var first = Make("first", 0); var advanced = Make("advanced", -75); var last = Make("last", -150); advanced.gameObject.SetActive(false);
        var navigation = viewport.gameObject.AddComponent<TabsExplicitNavigation>(); navigation.topButton = top; navigation.content = content; navigation.manualRefresh = true;
        var scroll = viewport.gameObject.AddComponent<AutoScrollRect>(); scroll.enabled = false; scroll.viewport = viewport; scroll.content = content; scroll.lastSelected = last.gameObject;
        var updater = viewport.gameObject.AddComponent<UpdateNavOnSettingChanged>(); updater.navigation = navigation; updater.autoScrollRect = scroll;
        var previous = CurrentSettings.A_SettingUpdated; CurrentSettings.A_SettingUpdated = null;
        viewport.gameObject.SetActive(true);
        Check(top.navigation.selectOnDown == first && first.navigation.selectOnDown == last, "enabling sets navigation around hidden row");
        Check(Mathf.Abs(content.anchoredPosition.y - 90f) < .01f, "enabling refreshes scrolling");
        advanced.gameObject.SetActive(true);
        CurrentSettings.A_SettingUpdated?.Invoke("unrelated", null, null);
        yield return null; yield return null;
        Check(first.navigation.selectOnDown == last, "unrelated settings do not rebuild navigation");
        content.anchoredPosition = Vector2.zero;
        CurrentSettings.A_SettingUpdated?.Invoke("show_advanced_settings", 0, 1);
        yield return null; yield return null;
        Check(first.navigation.selectOnDown == advanced && advanced.navigation.selectOnDown == last, "advanced row included after delayed refresh");
        Check(Mathf.Abs(content.anchoredPosition.y - 90f) < .01f, "advanced setting refreshes scroll");
        Destroy(viewport.gameObject); yield return null;
        Check(CurrentSettings.A_SettingUpdated == null, "destroy unsubscribes settings listener");
        CurrentSettings.A_SettingUpdated = previous;
        Debug.Log("AdvancedSettingsNavPlayCheck passed: enable, delayed advanced refresh, scrolling and cleanup");
        EditorApplication.Exit(0);
    }
}
