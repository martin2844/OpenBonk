using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class MapTitlePlayCheck
{
    const string Pending = "MapTitlePlayCheck.pending";
    static MapTitlePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("map title runner").AddComponent<MapTitlePlayRunner>().Begin(); } }
}
public class MapTitlePlayRunner : MonoBehaviour
{
    static void Require(bool value, string why) { if (!value) throw new Exception("MapTitlePlayCheck: " + why); }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    public void Begin() { StartCoroutine(Run()); }
    IEnumerator Run()
    {
        var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageForest1.asset");
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
        typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null,new object[] { stage });
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null,new object[] { map });
        MapController.runConfig = new RunConfig { mapData = map, stageData = stage, mapTierIndex = 0 };
        var root = new GameObject("title"); var title = root.AddComponent<MapTitle>();
        title.titleCanvasGroup = root.AddComponent<CanvasGroup>();
        TextMeshProUGUI Text(string name) { var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)); go.transform.SetParent(root.transform); return go.AddComponent<TextMeshProUGUI>(); }
        title.t_title=Text("title"); title.t_description=Text("description"); title.t_tier=Text("tier");
        try { title.StartAnimation(); Require(title.t_title.text == "Green Forest", "stage title"); Require(title.titleCanvasGroup.alpha == 0f, "initially transparent"); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(2f);
        try { Require(title.t_description.text == "Time to cook", "typed description"); Require(title.titleCanvasGroup.alpha > 0f, "faded in"); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSeconds(5f);
        try { Require(!root.activeSelf, "faded out and hidden"); Debug.Log("MapTitlePlayCheck passed: original Forest text, fade and typewriter"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
