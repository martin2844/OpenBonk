using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ShrineInformationPlayCheck
{
    const string Pending = "ShrineInformationPlayCheck.pending";
    static ShrineInformationPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("shrine information runner").AddComponent<ShrineInformationPlayRunner>().Begin(); } }
}
public class ShrineInformationPlayRunner : MonoBehaviour
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ShrineInformationPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var saveObject = new GameObject("save"); saveObject.SetActive(false); var save = saveObject.AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.debug_shrines = 1; saveObject.SetActive(true);
        var gameObject = new GameObject("game"); gameObject.SetActive(false); var game = gameObject.AddComponent<GameManager>(); GameManager.Instance = game;
        var uiObject = new GameObject("shrine ui", typeof(RectTransform)); uiObject.SetActive(false); var ui = uiObject.AddComponent<ShrineInformationUi>();
        var parent = new GameObject("rows", typeof(RectTransform)); parent.transform.SetParent(uiObject.transform, false); ui.parent = parent.transform;
        var template = new GameObject("template", typeof(RectTransform)); template.transform.SetParent(parent.transform, false); var row = template.AddComponent<ShrineInformationPrefab>();
        var name = new GameObject("name", typeof(RectTransform), typeof(TextMeshProUGUI)); name.transform.SetParent(template.transform, false); row.t_name = name.GetComponent<TextMeshProUGUI>();
        var value = new GameObject("value", typeof(RectTransform), typeof(TextMeshProUGUI)); value.transform.SetParent(template.transform, false); row.t_values = value.GetComponent<TextMeshProUGUI>(); ui.prefab = template;
        InteractablesStatus.Init(); uiObject.SetActive(true); yield return null;
        try
        {
            MapGenerationController.A_PreGeneration?.Invoke(); BaseInteractable.A_DebugSpawn?.Invoke("Zeta"); BaseInteractable.A_DebugSpawn?.Invoke("Alpha"); BaseInteractable.A_DebugSpawn?.Invoke(InteractablePot.debugNameCrypt); BaseInteractable.A_DebugSpawn?.Invoke(InteractableChest.debugNameCrypt);
            var fixedUpdate = typeof(ShrineInformationUi).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic); for (int i=0;i<5;i++) fixedUpdate.Invoke(ui, null);
            var list = (List<ShrineInformationPrefab>)typeof(ShrineInformationUi).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Check(list.Count == 4 && list[0].t_name.text == "Alpha" && list[3].t_name.text == "Zeta", "rows sorted by name");
            Check(list[0].gameObject.activeSelf && list[0].t_values.text == "0 / 1" && !list[1].gameObject.activeSelf, "overworld and crypt filtering");
            InteractablesStatus.interactablesByName["Alpha"].numUsed = 1; InteractablesStatus.A_InteractableUsed?.Invoke("Alpha"); Check(list[0].t_values.text == "1 / 1", "usage event refreshes count");
            typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(game, new object[] { true }); GameManager.A_DungeonStarted?.Invoke();
            Check(!list[0].gameObject.activeSelf && list[1].gameObject.activeSelf && list[2].gameObject.activeSelf, "crypt shows only crypt rows");
            save.config.cfGameSettings.debug_shrines = 0; CurrentSettings.A_SettingUpdated?.Invoke("debug_shrines", 1, 0); Check(!parent.activeSelf, "setting hides HUD");
            save.config.cfGameSettings.debug_shrines = 1; CurrentSettings.A_SettingUpdated?.Invoke("debug_shrines", 0, 1); Check(parent.activeSelf, "setting shows HUD");
            Debug.Log("ShrineInformationPlayCheck passed: event-driven rows, sorting, counts, crypt and visibility"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally { InteractablesStatus.Cleanup(); GameManager.Instance = null; }
    }
}
