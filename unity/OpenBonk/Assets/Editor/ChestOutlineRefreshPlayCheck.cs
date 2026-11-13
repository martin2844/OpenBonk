using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChestOutlineRefreshPlayCheck
{
    const string Pending = "ChestOutlineRefreshPlayCheck.pending";
    static ChestOutlineRefreshPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("chest refresh runner").AddComponent<ChestOutlineRefreshRunner>().Begin(); } }
}
public class ChestOutlineRefreshRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var state = new GameObject("controlled state"); state.SetActive(false);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.player = player;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.ChestPriceMultiplier] = 1f }; inv.playerStats = stats;
        var detector = state.AddComponent<DetectInteractables>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Chest.prefab"); Check(prefab != null, "original chest prefab");
        var obj = Instantiate(prefab); var chest = obj.GetComponent<InteractableChest>(); Check(chest != null && chest.showOutline, "original chest component");
        typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(detector, chest);
        var fixedUpdate = typeof(InteractableChest).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        chest.StartHover(detector); yield return null;
        var outline = obj.GetComponent<Outline>(); Check(outline != null && outline.OutlineColor == new Color32(189,34,34,255), "unaffordable chest starts red");
        fixedUpdate.Invoke(chest, null);
        typeof(PlayerInventory).GetProperty("gold").GetSetMethod(true).Invoke(inv, new object[] { 1000f });
        fixedUpdate.Invoke(chest, null); yield return null;
        Check(outline.OutlineColor == new Color32(255,215,69,255), "affordable chest refreshes gold outline");
        var renderer = obj.GetComponent<Renderer>(); var fill = renderer.sharedMaterials[renderer.sharedMaterials.Length - 1];
        Check(fill.GetColor("_OutlineColor") == outline.OutlineColor, "render material receives updated color");
        chest.StopHover(); yield return null; Check(obj.GetComponent<Outline>() == null, "hover ends cleanly");
        Destroy(obj); Debug.Log("ChestOutlineRefreshPlayCheck passed: actual chest refreshes red to gold after gaining money"); EditorApplication.Exit(0);
    }
}
