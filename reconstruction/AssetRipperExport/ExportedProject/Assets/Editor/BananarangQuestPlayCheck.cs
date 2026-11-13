using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BananarangQuestPlayCheck
{
    const string Pending = "BananarangQuestPlayCheck.pending";
    static BananarangQuestPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("banana quest test runner").AddComponent<BananarangQuestRunner>().Begin(); } }
}
public class BananarangQuestRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("BananarangQuestPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        InteractableBananarang quest = null;
        try
        {
            var root = new GameObject("test state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_bananarang.asset");
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBananarang.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BananaPickupQuest.prefab");
            Require(achievement && weapon && prefab, "original quest assets"); weapon.Init();
            ((Dictionary<string,MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data)).Add("a_bananarang", achievement);
            ((Dictionary<EWeapon,WeaponData>)typeof(DataManager).GetField("weapons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data)).Add(EWeapon.Bananarang, weapon);
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory = new WeaponInventory();
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            var effect = root.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.bananaQuest = prefab;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = new Vector3(0,-1,0); ground.transform.localScale = new Vector3(100,2,100); manager.whatIsGroundAndObjects = 1 << ground.layer;
            var spawner = root.AddComponent<SpawnInteractables>(); spawner.SetArea(new Vector3(30,10,30), Vector3.zero, 30); Physics.SyncTransforms();
            var spawn = typeof(SpawnInteractables).GetMethod("SpawnOther", BindingFlags.NonPublic | BindingFlags.Instance);
            Require(!MyAchievements.IsAchievementDone("a_bananarang"), "quest initially incomplete");
            spawn.Invoke(spawner, null); quest = FindObjectOfType<InteractableBananarang>();
            Require(quest != null && Mathf.Abs(quest.transform.position.y - 2f) < .02f, "original quest spawns above ground");
            var fx = quest.fx; Require(fx != null && quest.Interact(), "quest interaction succeeds");
            Require(MyAchievements.IsAchievementDone("a_bananarang") && player.inventory.weaponInventory.GetWeaponLevel(EWeapon.Bananarang) == 1, "achievement and weapon awarded");
            Require(fx.activeSelf && fx.transform.parent == null, "completion effect detached and enabled");
            Require(!quest.Interact(), "interaction cannot award twice");
            spawn.Invoke(spawner, null); Require(FindObjectsOfType<InteractableBananarang>().Length == 1, "completed quest does not spawn another object");
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return null;
        try { Require(quest == null, "quest object destroyed"); Debug.Log("BananarangQuestPlayCheck passed: original placement, reward and completion gate"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
