using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MenuVisitTransitionPlayCheck
{
    const string Pending = "MenuVisitTransitionPlayCheck.pending", Path = "Assets/MenuVisitTransitionTemporary.unity";
    static MenuVisitTransitionPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var menu = UnityEngine.Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        var alerts = UnityEngine.Object.FindAnyObjectByType<MenuAlerts>(FindObjectsInactive.Include);
        var unlocks = UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include);
        if (menu == null || alerts == null || unlocks == null || !alerts.transform.IsChildOf(menu.tabMenu.transform) || !unlocks.transform.IsChildOf(menu.tabUnlocks.transform))
            throw new Exception("Unexpected real menu hierarchy");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var menuTab = UnityEngine.Object.Instantiate(menu.tabMenu); menuTab.name = "TestMenuTab"; menuTab.SetActive(false);
        var unlockTab = UnityEngine.Object.Instantiate(menu.tabUnlocks); unlockTab.name = "TestUnlockTab"; unlockTab.SetActive(false);
        SceneManager.MoveGameObjectToScene(menuTab, scene); SceneManager.MoveGameObjectToScene(unlockTab, scene);
        foreach (var script in menuTab.GetComponentsInChildren<MonoBehaviour>(true)) if (script != null && !(script is MenuAlerts)) script.enabled = false;
        foreach (var script in unlockTab.GetComponentsInChildren<MonoBehaviour>(true)) if (script != null && !(script is UnlocksUi)) script.enabled = false;
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, Path);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("runner").AddComponent<MenuVisitTransitionRunner>().Begin(); } }
}
public class MenuVisitTransitionRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("MenuVisitTransition: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var state = new GameObject("fixture state"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var ui = new GameObject("menu controller"); ui.SetActive(false); var menu = ui.AddComponent<MainMenu>();
        menu.tabMenu = GameObject.Find("TestMenuTab"); menu.tabUnlocks = GameObject.Find("TestUnlockTab");
        // The cloned tabs start inactive, so Find cannot locate them.
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "TestMenuTab") menu.tabMenu = root;
            if (root.name == "TestUnlockTab") menu.tabUnlocks = root;
        }
        Check(menu.tabMenu != null && menu.tabUnlocks != null, "cloned menu tabs");
        var alerts = menu.tabMenu.GetComponentInChildren<MenuAlerts>(true);
        Check(alerts != null && alerts.alertUnlocks != null, "real unlock alert binding");
        typeof(MainMenu).GetField("currentTab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, menu.tabMenu);
        menu.tabMenu.SetActive(true); yield return null;
        Check(alerts.alertUnlocks.activeSelf && !save.progression.menuMeta.hasVisitedUnlocks, "unvisited alert visible");
        menu.SetWindow(menu.tabUnlocks); yield return null;
        Check(save.progression.menuMeta.hasVisitedUnlocks, "real UnlocksUi activation records visit");
        menu.SetWindow(menu.tabMenu); yield return null;
        Check(!alerts.alertUnlocks.activeSelf, "return to main menu refreshes and clears alert");
        save.progression.newUnlockables.Add("synthetic-pending");
        menu.SetWindow(menu.tabUnlocks); yield return null; menu.SetWindow(menu.tabMenu); yield return null;
        Check(alerts.alertUnlocks.activeSelf, "genuinely new unlock still alerts after visit");
        save.progression.newUnlockables.Clear();
        menu.SetWindow(menu.tabUnlocks); yield return null; menu.SetWindow(menu.tabMenu); yield return null;
        Check(!alerts.alertUnlocks.activeSelf, "return after clearing new unlock dismisses alert");
        Debug.Log("MenuVisitTransition passed: original tabs, MainMenu.SetWindow, visit and pending-unlock alert refresh"); EditorApplication.Exit(0);
    }
}
