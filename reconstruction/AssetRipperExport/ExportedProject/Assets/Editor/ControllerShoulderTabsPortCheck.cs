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
public static class ControllerShoulderTabsPortCheck
{
    const string Pending = "ControllerShoulderTabsPortCheck.pending";
    static ControllerShoulderTabsPortCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("tabs runner").AddComponent<ControllerShoulderTabsRunner>().Begin(); } }
}
public class ControllerShoulderTabsRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Nav(ControllerShoulderTabs component, int direction) => typeof(ControllerShoulderTabs).GetMethod("ShoulderNav", Private).Invoke(component, new object[] { direction });
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        new GameObject("tabs event system", typeof(EventSystem));
        var root = new GameObject("tabs root"); root.SetActive(false);
        var navigation = root.AddComponent<ButtonNavigationSelectionOnly>();
        var shoulder = root.AddComponent<ControllerShoulderTabs>(); shoulder.tabs = navigation;
        for (int i = 0; i < 3; i++)
        {
            var obj = new GameObject("tab" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MyButtonTabs));
            obj.transform.SetParent(root.transform, false);
            var tab = obj.GetComponent<MyButtonTabs>(); tab.background = obj.GetComponent<Image>();
        }
        root.SetActive(true); yield return null;
        int callbacks = 0; navigation.A_ButtonSelected += _ => callbacks++;
        Nav(shoulder, -1); Check(navigation.current == 0 && callbacks == 0, "left boundary");
        Nav(shoulder, 1); Check(navigation.current == 1 && callbacks == 1 && EventSystem.current.currentSelectedGameObject.name == "tab1", "right changes tab and focus");
        Nav(shoulder, 1); Nav(shoulder, 1); Check(navigation.current == 2 && callbacks == 2, "right boundary");
        Nav(shoulder, -1); Check(navigation.current == 1 && callbacks == 3 && EventSystem.current.currentSelectedGameObject.name == "tab1", "left changes tab and focus");
        ButtonManager.SetNull();
        Debug.Log("ControllerShoulderTabsPortCheck passed: bounded navigation, selection events and focus"); EditorApplication.Exit(0);
    }
}
