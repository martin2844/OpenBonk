using System;
using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MenuUnlocksPlayCheck
{
    const string Pending = "MenuUnlocksPlayCheck.pending";
    static MenuUnlocksPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("menu check").AddComponent<MenuUnlocksRunner>().Begin(); } }
}
public class MenuUnlocksRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("MenuUnlocksPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guarded()); }
    IEnumerator Guarded() {
        var test = Run();
        while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    IEnumerator Run() {
        SaveManager.loaded = false;
        var state = new GameObject("save state"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.stats = new StatsSaveFile(); save.progression = new ProgressionSaveFile(); save.config = new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var go = new GameObject("menu"); go.SetActive(false); var menu = go.AddComponent<MainMenu>();
        menu.btnUnlocks = new GameObject("unlocks"); menu.btnQuests = new GameObject("quests"); menu.btnShop = new GameObject("shop"); menu.leaderboards = new GameObject("leaderboards"); menu.quickQuests = new GameObject("quick quests"); menu.blockRaycastOverlay = new GameObject("blocker"); menu.blockRaycastOverlay.SetActive(false);
        go.SetActive(true); yield return null;
        SaveManager.loaded = true;
        var prefs = save.config.preferences; prefs.hasShownUnlocks = prefs.hasShownQuests = prefs.hasShownShop = prefs.hasShownLeaderboards = prefs.hasShownQuickQuests = true;
        for (int runs = 0; runs <= 7; runs++) {
            MyStats.SetValueForce("runs", runs); SaveManager.A_SavesLoaded?.Invoke();
            Check(menu.btnUnlocks.activeSelf == (runs >= 1) && menu.leaderboards.activeSelf == (runs >= 2) && menu.btnQuests.activeSelf == (runs >= 4) && menu.quickQuests.activeSelf == (runs >= 5) && menu.btnShop.activeSelf == (runs >= 6), "thresholds at " + runs);
        }
        save.config.cfGameSettings.hide_leaderboards = 1; SaveManager.A_SavesLoaded?.Invoke(); Check(!menu.leaderboards.activeSelf,"hidden leaderboard");
        save.config.cfGameSettings.hide_leaderboards = 2; SaveManager.A_SavesLoaded?.Invoke(); Check(menu.leaderboards.activeSelf,"leaderboard setting only hides at 1");
        MyStats.SetValueForce("runs",0); SaveManager.A_SavesLoaded?.Invoke(); prefs.hasShownUnlocks = false; MyStats.SetValueForce("runs",1); SaveManager.A_SavesLoaded?.Invoke();
        Check(prefs.hasShownUnlocks && menu.blockRaycastOverlay.activeSelf && !ButtonManager.enabled && menu.btnUnlocks.transform.localScale == Vector3.zero,"animation begins and blocks input");
        yield return new WaitForSecondsRealtime(2.5f);
        Check(ButtonManager.enabled && !menu.blockRaycastOverlay.activeSelf && menu.btnUnlocks.transform.localScale == Vector3.one,"animation completes and restores input");
        SaveManager.loaded = false;
        Debug.Log("MenuUnlocksPlayCheck PASSED: thresholds, settings, save-loaded event and animation"); EditorApplication.Exit(0);
    }
}
