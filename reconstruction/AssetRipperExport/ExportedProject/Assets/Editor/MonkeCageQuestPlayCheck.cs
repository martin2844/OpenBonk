using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MonkeCageQuestPlayCheck {
    const string Pending = "MonkeCageQuestPlayCheck.pending";
    static MonkeCageQuestPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("cage test runner").AddComponent<MonkeCageQuestRunner>().Begin(); } }
}
public class MonkeCageQuestRunner : MonoBehaviour {
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("MonkeCageQuestPlayCheck: " + message); }
    static void Use(DetectInteractables detector, BaseInteractable item) { typeof(DetectInteractables).GetField("currentInteractable", Private).SetValue(detector, item); detector.TryInteract(); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        QuestMonke released = null;
        try {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_monke.asset"); var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/CageKeyMonke.asset");
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            typeof(DataManager).GetField("achievementsData", Private).SetValue(data, new Dictionary<string,MyAchievement> { ["a_monke"] = achievement });
            typeof(DataManager).GetField("itemData", Private).SetValue(data, new Dictionary<EItem,ItemData> { [EItem.CageKey] = item });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
            var detector = root.AddComponent<DetectInteractables>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(200,2,200); game.whatIsGroundAndObjects = 1 << ground.layer;
            var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
            MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100,20,100); Physics.SyncTransforms(); MapController.index = 3;
            var effect = root.AddComponent<EffectManager>(); effect.monkeCage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MonkeCage.prefab"); effect.monkeCageKey = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/KeyPickupMonke.prefab");
            Check(achievement && item && effect.monkeCage && effect.monkeCageKey, "original quest assets");
            typeof(EffectManager).GetMethod("Awake", Private).Invoke(effect, null); MapGenerationController.A_GenerationComplete?.Invoke();
            var cage = FindObjectOfType<InteractableCage>(); var key = FindObjectOfType<InteractableCageKey>();
            Check(cage && key, "Forest generates cage and key");
            Check(Mathf.Abs(cage.transform.root.position.y) < .05f && Mathf.Abs(key.transform.root.position.y - 2f) < .05f, "original ground and key height");
            Check(!cage.CanInteract(), "cage locked before key");
            var keyFx = key.fx; Use(detector, key); Check(player.inventory.itemInventory.GetAmount(EItem.CageKey) == 1 && keyFx.activeSelf && keyFx.transform.parent == null, "actual pickup grants key and releases effect");
            Check(cage.CanInteract(), "key makes cage interactable"); var fx = cage.fx;
            Use(detector, cage); Check(MyAchievements.IsAchievementDone("a_monke") && cage.monke.activeSelf, "cage interaction unlocks Monke");
            Check(!cage.collider.enabled && !cage.meshRenderer.enabled && !cage.audioSource.enabled && fx.activeSelf && fx.transform.parent == null, "cage opens and stops audio");
            Check(player.inventory.itemInventory.GetAmount(EItem.CageKey) == 1 && !cage.Interact(), "key retained and cage opens once");
            MapGenerationController.A_GenerationComplete?.Invoke(); Check(FindObjectsOfType<InteractableCage>().Length == 1 && FindObjectsOfType<InteractableCageKey>().Length == 1, "completed achievement prevents new quest");
            released = cage.monke.GetComponent<QuestMonke>();
            Check(released && released.animator && released.rb, "released monkey controller wiring");
            released.animator.speed = 100f;
            player.inventory.itemInventory.Cleanup();
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        for (int i = 0; i < 100 && released && !released.animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"); i++) yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        try {
            Check(released && released.animator.GetCurrentAnimatorStateInfo(0).IsName("Walking"), "released monkey transitions from dance to walking");
            Check((bool)typeof(QuestMonke).GetField("isWalking", Private).GetValue(released), "walking animation enables controller");
            Check(new Vector2(released.rb.velocity.x, released.rb.velocity.z).magnitude > 11f, "released monkey runs after animation");
            Debug.Log("MonkeCageQuestPlayCheck passed: original key and cage, unlock, animation transition and running"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
