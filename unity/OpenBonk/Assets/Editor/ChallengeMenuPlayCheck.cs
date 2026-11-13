using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ChallengeMenuPlayCheck
{
    const string Pending = "ChallengeMenuPlayCheck.pending", Path = "Assets/ChallengeMenuTemporary.unity";
    static ChallengeMenuPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var ui = UnityEngine.Object.FindAnyObjectByType<ChallengesUi>(FindObjectsInactive.Include); var map = UnityEngine.Object.FindAnyObjectByType<MapSelectionUi>(FindObjectsInactive.Include);
        Transform common = map.transform.parent; if (!ui.transform.IsChildOf(common)) throw new Exception("unexpected challenge hierarchy");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); var clone = UnityEngine.Object.Instantiate(common.gameObject); clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone, scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, Path); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("challenge menu runner").AddComponent<ChallengeMenuPlayRunner>().Begin(); } }
}
public class ChallengeMenuPlayRunner : MonoBehaviour
{
    sealed class Provider : ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable { var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset"); return Addressables.ResourceManager.CreateCompletedOperation(table as T, null); } }
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("ChallengeMenuPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run() {
        Application.logMessageReceived += Capture; LocalizationSettings.StringDatabase.TableProvider = new Provider(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en");
        var canvas = FindAnyObjectByType<ChallengesUi>(FindObjectsInactive.Include).transform.parent.gameObject; canvas.SetActive(false);
        var ui = canvas.GetComponentInChildren<ChallengesUi>(true); var menu = canvas.GetComponentInChildren<MapSelectionUi>(true); var statsWindow = canvas.GetComponentInChildren<MapStatsWindow>(true);
        foreach (var b in canvas.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null && b.GetType().Assembly.GetName().Name == "Assembly-CSharp" && !(b is MyButton) && !(b is Window) && !(b is SelectionGroupToggleSingle) && !(b is MapStatsWindow)) b.enabled = false;
        var managers = new GameObject("test managers"); managers.SetActive(false); var save = managers.AddComponent<SaveManager>(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile();
        var dataManager = managers.AddComponent<DataManager>(); DataManager.Instance = dataManager; var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset"); dataManager.maps = new List<MapData> { map };
        var stageOriginal = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageForest1.asset"); var stage = Instantiate(stageOriginal); var original = stageOriginal.challenges.First(c => c != null && c.isEnabled);
        var shown = Instantiate(original); shown.isEnabled = true; shown.prerequisiteChallenge = null; shown.requiresNumChallengesCompleted = 0; shown.sortingOrder = 1; shown.silverMultiplier = 1.5f;
        var hidden = Instantiate(original); hidden.isEnabled = true; hidden.prerequisiteChallenge = null; hidden.requiresNumChallengesCompleted = 2; hidden.sortingOrder = 2; hidden.internalName = "synthetic_hidden";
        var disabled = Instantiate(original); disabled.isEnabled = false; stage.challenges = new[] { hidden, shown, disabled }; menu.runConfig = new RunConfig { mapData = map, stageData = stage, mapTierIndex = 0 };
        new GameObject("event system", typeof(EventSystem), typeof(StandaloneInputModule)); menu.gameObject.SetActive(true); ui.gameObject.SetActive(true); canvas.SetActive(true); yield return null;
        Check(!statsWindow.btnChallenges.GetButton().interactable, "challenges locked before tier completion");
        save.progression.menuMeta.SetTierCompletion(map.eMap, 0); MapSelectionUi.A_RunConfigChanged?.Invoke(menu.runConfig);
        Check(statsWindow.btnChallenges.GetButton().interactable, "tier completion unlocks challenges");
        menu.runConfig.stageData = stage; typeof(ChallengesUi).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        var rows = ui.challengesSelectionGroup.GetComponentsInChildren<SelectionGroupToggleSingleButtonChallenge>(false).Where(b => b.gameObject.activeInHierarchy).ToArray();
        Debug.Log("CHALLENGE TEST ui=" + ui.gameObject.activeInHierarchy + " group=" + ui.challengesSelectionGroup.gameObject.activeInHierarchy + " rows=" + rows.Length + " source=" + stage.challenges.Length + " error=" + error + " details=" + string.Join(",", rows.Select(r => r.name + ":" + (r.challengeData == null ? "null" : r.challengeData.internalName))));
        Check(rows.Length == 2 && rows[0].challengeData == shown && rows[1].challengeData == hidden, "enabled challenges are sorted and displayed");
        Check(rows[0].isShowing && !rows[1].isShowing && rows[0].t_name.text == shown.GetDisplayName() && rows[1].t_name.text == "??", "visible and locked challenge labels");
        rows[0].StartHover();
        Check(ui.t_completed.color == (Color)new Color32(236, 52, 52, 255) && ui.t_reward.color == new Color(0.5f, 0.5f, 0.5f, 1f), "incomplete challenge uses red status and muted reward");
        save.progression.achievements.Add(shown.internalName); rows[0].StartHover();
        Check(ui.t_completed.color == (Color)new Color32(0, 196, 255, 255) && ui.t_reward.color == Color.white, "completed challenge uses cyan status and white reward");
        save.progression.achievements.Remove(shown.internalName); rows[0].StartHover();
        Check(ui.t_completed.color == (Color)new Color32(236, 52, 52, 255) && ui.t_reward.color == new Color(0.5f, 0.5f, 0.5f, 1f), "status colors reset when hovering an incomplete challenge");
        Check(statsWindow.t_mapSpecs.text.Contains("Stages: 1") && !statsWindow.t_mapSpecs.text.Contains("Pacifist"), "map summary replaces scene placeholder");
        rows[0].GetButton().onClick.Invoke(); Check(menu.runConfig.challenge == shown && Mathf.Approximately(menu.runConfig.GetSilverMultiplier(), 1.5f), "real button selects challenge and reward multiplier");
        Check(statsWindow.t_mapSpecs.text.Contains(shown.GetDisplayName()), "summary updates selected challenge");
        menu.tierSelectionGroup.ForceSelect(1); Check(menu.runConfig.challenge == null && menu.runConfig.mapTierIndex == 1, "changing tiers clears challenge");
        menu.tierSelectionGroup.ForceSelect(0); menu.runConfig.stageData = stage; typeof(ChallengesUi).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        rows[0].GetButton().onClick.Invoke(); Check(menu.runConfig.challenge == shown, "challenge can be selected again");
        ui.SetNone(); Check(menu.runConfig.challenge == null && !statsWindow.t_mapSpecs.text.Contains(shown.GetDisplayName()), "clear selected challenge");
        rows[1].GetButton().onClick.Invoke(); Check(menu.runConfig.challenge == null && ui.t_name.text == "??", "locked row cannot activate challenge");
        MyStats.SetValueForce(EMyStat.challengesCompleted.ToString(), 2); typeof(ChallengesUi).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        rows = ui.challengesSelectionGroup.GetComponentsInChildren<SelectionGroupToggleSingleButtonChallenge>(false).Where(b => b.gameObject.activeInHierarchy).ToArray(); Check(rows[1].isShowing, "challenge appears when completion threshold met");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("ChallengeMenuPlayCheck passed: original menu hierarchy, visible and locked entries, selection, clearing, summary and unlock threshold"); EditorApplication.Exit(0);
    }
}
