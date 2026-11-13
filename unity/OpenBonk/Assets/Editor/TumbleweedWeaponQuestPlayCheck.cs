using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TumbleweedWeaponQuestPlayCheck {
    const string Pending = "TumbleweedWeaponQuestPlayCheck.pending";
    static TumbleweedWeaponQuestPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("weapon quest runner").AddComponent<TumbleweedWeaponQuestRunner>().Begin(); } }
}
public class TumbleweedWeaponQuestRunner : MonoBehaviour {
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("TumbleweedWeaponQuestPlayCheck: " + msg); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run() {
        try {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var katanaAch = Instantiate(AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_katana.asset")); var shotgunAch = Instantiate(AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_shotgun.asset"));
            Check(katanaAch.targetValue == 5 && shotgunAch.targetValue == 5, "original five percent odds"); katanaAch.targetValue = shotgunAch.targetValue = 100;
            var katana = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponKatana.asset"); var shotgun = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponShotgun.asset"); katana.Init(); shotgun.Init();
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            typeof(DataManager).GetField("achievementsData", P).SetValue(data, new Dictionary<string,MyAchievement> { ["a_katana"] = katanaAch, ["a_shotgun"] = shotgunAch });
            typeof(DataManager).GetField("weapons", P).SetValue(data, new Dictionary<EWeapon,WeaponData> { [katana.eWeapon] = katana, [shotgun.eWeapon] = shotgun });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory = new WeaponInventory(); player.inventory.itemInventory = new ItemInventory();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down; ground.transform.localScale = new Vector3(50,2,50); game.whatIsGround = 1 << ground.layer; Physics.SyncTransforms();
            var effect = root.AddComponent<EffectManager>(); effect.katanaQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/KatanaQuest.prefab"); effect.shotgunQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShotgunQuest.prefab"); effect.luckTomeQuest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LuckTomePickupQuest.prefab");
            Check(effect.katanaQuest && effect.shotgunQuest && effect.luckTomeQuest, "original pickup prefabs"); typeof(EffectManager).GetMethod("Awake", P).Invoke(effect, null); AchievementTracker.Init();
            var weedObject = new GameObject("tumbleweed"); weedObject.SetActive(false); weedObject.transform.position = new Vector3(3, 5, 4); var weed = weedObject.AddComponent<InteractableTumbleWeed>();
            MapController.index = 0; DetectInteractables.A_Interacted?.Invoke(weed, false); Check(!effect.spawnedKatanaObject, "failed interaction ignored");
            DetectInteractables.A_Interacted?.Invoke(weed, true); Check(effect.spawnedKatanaObject && !effect.spawnedShotgunObject, "stage one spawns Katana");
            Check(Vector3.Distance(effect.spawnedKatanaObject.transform.position, new Vector3(3,2,4)) < .01f, "pickup placed above raycast ground");
            var first = effect.spawnedKatanaObject; DetectInteractables.A_Interacted?.Invoke(weed, true); Check(effect.spawnedKatanaObject == first, "no duplicate while pickup exists");
            var pickup = first.GetComponent<InteractableUnlock>(); Check(pickup.Interact() && !pickup.Interact(), "Katana picked up once");
            Check(MyAchievements.IsAchievementDone("a_katana") && player.inventory.weaponInventory.GetWeaponLevel(katana.eWeapon) == 1, "Katana and achievement awarded");
            MapController.index = 1; DetectInteractables.A_Interacted?.Invoke(weed, true); Check(effect.spawnedShotgunObject, "stage two spawns Shotgun");
            pickup = effect.spawnedShotgunObject.GetComponent<InteractableUnlock>(); Check(pickup.Interact(), "Shotgun picked up"); Check(MyAchievements.IsAchievementDone("a_shotgun") && player.inventory.weaponInventory.GetWeaponLevel(shotgun.eWeapon) == 1, "Shotgun and achievement awarded");
            effect.TrySpawnLuckQuest(new Vector3(1,2,3)); var luck = effect.spawnedLuckTomeObject; effect.TrySpawnLuckQuest(Vector3.zero); Check(luck && effect.spawnedLuckTomeObject == luck, "Luck pickup uses same single-instance rule");
            AchievementTracker.Cleanup(); player.inventory.weaponInventory.Cleanup(); player.inventory.itemInventory.Cleanup();
            Debug.Log("TumbleweedWeaponQuestPlayCheck passed: stage gates, spawn chances, weapon rewards and single-instance placement"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
