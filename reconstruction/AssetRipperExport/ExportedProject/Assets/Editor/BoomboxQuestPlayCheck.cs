using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BoomboxQuestPlayCheck {
    const string Pending = "BoomboxQuestPlayCheck.pending";
    static BoomboxQuestPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("boombox test runner").AddComponent<BoomboxQuestRunner>().Begin(); } }
}
public class BoomboxQuestRunner : MonoBehaviour {
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("BoomboxQuestPlayCheck: " + message); }
    static void Use(DetectInteractables detector, InteractableBoombox box) { typeof(DetectInteractables).GetField("currentInteractable", Private).SetValue(detector, box); detector.TryInteract(); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_boombox.asset");
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            typeof(DataManager).GetField("achievementsData", Private).SetValue(data, new Dictionary<string,MyAchievement> { ["a_boombox"] = achievement });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var detector = root.AddComponent<DetectInteractables>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(200,2,200); game.whatIsGroundAndObjects = 1 << ground.layer;
            var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
            MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100,20,100); Physics.SyncTransforms(); MapController.index = 3;
            var effect = root.AddComponent<EffectManager>(); effect.boomboxQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BoomboxQuest.prefab");
            Check(achievement && achievement.targetValue == 3 && effect.boomboxQuest, "original achievement and prefab");
            typeof(EffectManager).GetMethod("Awake", Private).Invoke(effect, null); AchievementTracker.Init(); GameManager.A_StageStarted?.Invoke();
            MapGenerationController.A_GenerationComplete?.Invoke(); var first = FindObjectsOfType<InteractableBoombox>(); Check(first.Length == 3, "three Forest boomboxes");
            foreach (var box in first) Check(Mathf.Abs(box.transform.root.position.y) < .05f && box.CanInteract(), "ground placement and interaction availability");
            DetectInteractables.A_Interacted?.Invoke(first[0], false); Check(!MyAchievements.IsAchievementDone("a_boombox"), "failed interaction ignored");
            var icon = first[0].alertIcon; Use(detector, first[0]); Check(!first[0].CanInteract() && first[0].fx.activeSelf && first[0].pauseZone.enabled, "activation effects and pause zone");
            Use(detector, first[1]); Check(!MyAchievements.IsAchievementDone("a_boombox"), "two radios insufficient");
            GameManager.A_StageStarted?.Invoke(); MapGenerationController.A_GenerationComplete?.Invoke();
            var all = FindObjectsOfType<InteractableBoombox>(); Check(all.Length == 6, "new stage placements");
            int used = 0; foreach (var box in all) { if (Array.IndexOf(first, box) >= 0) continue; Use(detector, box); used++; if (used < 3) Check(!MyAchievements.IsAchievementDone("a_boombox"), "stage reset and threshold"); }
            Check(used == 3 && MyAchievements.IsAchievementDone("a_boombox"), "third activation unlocks achievement");
            MapGenerationController.A_GenerationComplete?.Invoke(); Check(FindObjectsOfType<InteractableBoombox>().Length == 6, "completed achievement stops spawning");
            AchievementTracker.Cleanup();
            Debug.Log("BoomboxQuestPlayCheck passed: Forest placement, real interactions, stage reset and achievement"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
