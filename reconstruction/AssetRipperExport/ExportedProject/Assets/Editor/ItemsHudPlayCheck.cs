using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ItemsHudPlayCheck
{
    const string Pending = "ItemsHudPlayCheck.pending", TempScene = "Assets/ItemsHudTemporary.unity";
    static ItemsHudPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<ItemsHud>(FindObjectsInactive.Include); if (original == null) throw new Exception("original item HUD not found");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); var clone = UnityEngine.Object.Instantiate(original.gameObject); clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone, scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, TempScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("items HUD runner").AddComponent<ItemsHudPlayRunner>().Begin(); } }
}

public class ItemsHudPlayRunner : MonoBehaviour
{
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var playerObject = new GameObject("test player"); playerObject.SetActive(false); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var managerObject = new GameObject("test data"); managerObject.SetActive(false); var manager = managerObject.AddComponent<DataManager>(); DataManager.Instance = manager;
        var clover = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset"); Check(clover != null && clover.icon != null, "original Clover data and icon");
        var items = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager); items.Add(EItem.Clover, clover);
        var inventory = player.inventory.itemInventory; inventory.AddItem(EItem.Clover);
        var hud = FindAnyObjectByType<ItemsHud>(FindObjectsInactive.Include); Check(hud != null && hud.prefab != null, "cloned original HUD and template"); hud.gameObject.SetActive(true); yield return null;
        var entries = hud.GetComponentsInChildren<ItemsHudElementPrefab>(false); Check(entries.Length == 1 && entries[0].icon.texture == clover.icon && entries[0].t_amount.text == "", "existing inventory creates real icon without single-stack count");
        inventory.AddItem(EItem.Clover); Check(entries[0].t_amount.text == "x2", "second item updates stack count");
        inventory.RemoveItem(EItem.Clover); Check(entries[0].t_amount.text == "", "removing one hides single-stack count");
        inventory.RemoveItem(EItem.Clover); yield return null; Check(hud.GetComponentsInChildren<ItemsHudElementPrefab>(false).Length == 0, "last removal destroys icon");
        inventory.AddItem(EItem.Clover); Check(hud.GetComponentsInChildren<ItemsHudElementPrefab>(false).Length == 1, "reacquiring recreates icon");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture; inventory.Cleanup();
        Debug.Log("ItemsHudPlayCheck passed: original hierarchy, preexisting item, stack changes, removal and reacquisition"); EditorApplication.Exit(0);
    }
}
