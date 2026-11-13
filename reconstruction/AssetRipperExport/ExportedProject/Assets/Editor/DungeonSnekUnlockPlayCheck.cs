using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DungeonSnekUnlockPlayCheck
{
    const string Pending = "DungeonSnekUnlockPlayCheck.pending";
    static DungeonSnekUnlockPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var original = EditorSceneManager.OpenScene("Assets/Scenes/GeneratedMap.unity", OpenSceneMode.Single);
        DungeonSnekSpawner spawner = null;
        foreach (var root in original.GetRootGameObjects()) { spawner = root.GetComponentInChildren<DungeonSnekSpawner>(true); if (spawner) break; }
        if (!spawner || !spawner.snek || !spawner.chest) throw new Exception("original scene references missing");
        var clean = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var clone = UnityEngine.Object.Instantiate(spawner.snek); clone.name = "test scene Snek"; clone.SetActive(false);
        SceneManager.MoveGameObjectToScene(clone, clean);
        var interaction = clone.GetComponent<InteractableUnlock>();
        if (!interaction || !interaction.fx) throw new Exception("original Snek interaction references missing");
        var effect = UnityEngine.Object.Instantiate(interaction.fx); effect.transform.SetParent(clone.transform); effect.SetActive(false); interaction.fx = effect;
        var chest = new GameObject("test chest"); SceneManager.MoveGameObjectToScene(chest, clean);
        var selector = new GameObject("test selector"); SceneManager.MoveGameObjectToScene(selector, clean);
        var testSpawner = selector.AddComponent<DungeonSnekSpawner>(); testSpawner.enabled = false; testSpawner.snek = clone; testSpawner.chest = chest;
        EditorSceneManager.CloseScene(original, true); SceneManager.SetActiveScene(clean);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("Snek unlock runner").AddComponent<DungeonSnekUnlockRunner>().Begin();
        }
    }
}

public class DungeonSnekUnlockRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("DungeonSnekUnlockPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        yield return null;
        try
        {
            var root = new GameObject("controlled state"); root.SetActive(false);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_snek.asset");
            var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Snek.asset");
            Require(achievement && item && item.eItem == EItem.Snek, "real item and achievement assets");
            typeof(DataManager).GetField("achievementsData", Private).SetValue(data, new Dictionary<string, MyAchievement> { ["a_snek"] = achievement });
            typeof(DataManager).GetField("itemData", Private).SetValue(data, new Dictionary<EItem, ItemData> { [EItem.Snek] = item });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
            var selector = GameObject.Find("test selector").GetComponent<DungeonSnekSpawner>();
            RsgController.isCurrentMapRandomSeed = true;
            typeof(DungeonSnekSpawner).GetMethod("Start", Private).Invoke(selector, null);
            var interaction = selector.snek.GetComponent<InteractableUnlock>(); var fx = interaction.fx;
            Require(selector.snek.activeSelf && !selector.chest.activeSelf && interaction.unlock == item, "random crypt selects original Snek reward");
            Require(interaction.Interact(), "interaction succeeds");
            Require(MyAchievements.IsAchievementDone("a_snek") && player.inventory.itemInventory.GetAmount(EItem.Snek) == 1, "achievement and Snek item granted");
            Require(fx.activeSelf && fx.transform.parent == null && !interaction.Interact(), "effect released and interaction one-shot");
            player.inventory.itemInventory.Cleanup();
            Debug.Log("DungeonSnekUnlockPlayCheck passed: scene reward selection, Snek item, achievement and effect");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
