using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class KeyChestPlayCheck
{
    const string Pending = "KeyChestPlayCheck.pending";
    static KeyChestPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("key chest test").AddComponent<KeyChestPlayRunner>().Begin(); } }
}
public class KeyChestPlayRunner : MonoBehaviour
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double value) { this.value = value; } public override double NextDouble() { return value; } }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("KeyChestPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool next; try { next = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!next) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var state = new GameObject("controlled state"); state.SetActive(false);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = state.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.ChestPriceMultiplier] = 1f }; player.inventory.playerStats = stats;
        typeof(PlayerInventory).GetProperty("gold").SetValue(player.inventory, 100f);
        typeof(PlayerInventory).GetProperty("goldInt").SetValue(player.inventory, 100);
        var priceCount = typeof(MoneyUtility).GetField("chestsPurchased", BindingFlags.Static | BindingFlags.NonPublic); priceCount.SetValue(null, 0);
        var key = new ItemKey(player.inventory.itemInventory); key.AddAmount(); player.inventory.itemInventory.items[EItem.Key] = key;
        Check(Mathf.Abs(key.GetChance() - 1f/11f) < .00001f, "one stack chance");
        key.AddAmount(); Check(Mathf.Abs(key.GetChance() - 1f/6f) < .00001f, "two stack chance");
        key.RemoveAmount(); Check(Mathf.Abs(key.GetChance() - 1f/11f) < .00001f, "removed stack chance");
        var effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects; effects.wuiFreeChest = new GameObject("key free effect"); effects.wuiFreeChest.SetActive(false);
        var oldRandom = global::Utility.MyRandom.random;
        var close = typeof(InteractableChest).GetMethod("OnChestWindowClose", BindingFlags.Instance | BindingFlags.NonPublic);
        var opening = typeof(InteractableChest).GetField("opening", BindingFlags.Instance | BindingFlags.NonPublic);
        var freeObject = new GameObject("free result"); freeObject.SetActive(false); var free = freeObject.AddComponent<InteractableChest>(); free.chestType = EChest.Normal;
        opening.SetValue(free, true); global::Utility.MyRandom.random = new FixedRandom(.09); close.Invoke(free, null);
        Check(player.inventory.gold == 100 && (int)priceCount.GetValue(null) == 0, "successful key must preserve gold and chest price");
        var paidObject = new GameObject("paid result"); paidObject.SetActive(false); var paid = paidObject.AddComponent<InteractableChest>(); paid.chestType = EChest.Normal;
        opening.SetValue(paid, true); global::Utility.MyRandom.random = new FixedRandom(.10); close.Invoke(paid, null);
        Check(player.inventory.gold == 70 && (int)priceCount.GetValue(null) == 1, "failed key must charge gold and raise chest price");
        global::Utility.MyRandom.random = oldRandom;
        yield return null; Debug.Log("KeyChestPlayCheck passed: key stack scaling and seeded free versus paid chest paths"); EditorApplication.Exit(0);
    }
}
