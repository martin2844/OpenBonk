using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForestGiftQuestPlayCheck
{
    const string Pending = "ForestGiftQuestPlayCheck.pending";
    static ForestGiftQuestPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("gift test runner").AddComponent<ForestGiftQuestRunner>().Begin(); } }
}
public class ForestGiftQuestRunner : MonoBehaviour
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ForestGiftQuestPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        InteractableGift gift = null; InteractableUnlock hat = null;
        try {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_santaHat.asset");
            Require(achievement != null, "original Santa hat achievement");
            ((Dictionary<string,MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data)).Add("a_santaHat", achievement);
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            var effect = root.AddComponent<EffectManager>(); effect.presentQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GiftQuest.prefab");
            Require(effect.presentQuest != null, "original gift prefab");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(200,2,200);
            manager.whatIsGroundAndObjects = 1 << ground.layer; MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100,20,100); Physics.SyncTransforms();
            var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest;
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
            typeof(EffectManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(effect, null);
            MapController.index = 3; MapGenerationController.A_GenerationComplete?.Invoke(); gift = FindObjectOfType<InteractableGift>();
            Require(gift != null && Mathf.Abs(gift.transform.position.y) < .05f, "gift generated on Forest ground");
            var hatObject = gift.hatFloating; var giftFx = gift.fx; hat = hatObject.GetComponent<InteractableUnlock>();
            Require(hat != null && !hatObject.activeSelf && !giftFx.activeSelf, "original hidden hat and effect");
            Require(gift.Interact() && !gift.Interact(), "gift opens once");
            Require(hatObject.activeSelf && giftFx.activeSelf && hatObject.transform.parent == null && giftFx.transform.parent == null, "hat and effect released");
            Require(hat.Interact() && !hat.Interact(), "hat collected once");
            Require(MyAchievements.IsAchievementDone("a_santaHat"), "Santa hat achievement awarded");
            MapGenerationController.A_GenerationComplete?.Invoke(); Require(FindObjectsOfType<InteractableGift>().Length == 1, "completed achievement suppresses new gift");
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return null;
        try { Require(gift == null && hat == null, "collected objects destroyed"); Debug.Log("ForestGiftQuestPlayCheck passed: original gift, released hat, achievement and spawn gate"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
