using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class GreedShrinePlayCheck
{
    const string Pending = "GreedShrinePlayCheck.pending";
    static GreedShrinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("greed shrine test runner").AddComponent<GreedShrinePlayRunner>().Begin(); } }
}
public class GreedShrinePlayRunner : MonoBehaviour
{
    static void Check(bool value, string message) { if (!value) throw new Exception("GreedShrinePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        var markers = new List<GameObject>();
        try
        {
            var root = new GameObject("synthetic state"); root.SetActive(false);
            GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            inventory.statInventory = new StatInventory();
            inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            inventory.playerStats.stats = new Dictionary<EStat, float> { [EStat.ChestPriceMultiplier] = 1f };
            UiManager.Instance = root.AddComponent<UiManager>(); UiManager.Instance.shrineLogs = root.AddComponent<ShrineLogs>(); UiManager.Instance.scoreUi = root.AddComponent<ScoreUi>();
            PoolManager.Instance = root.AddComponent<PoolManager>(); var coins = new List<MoneyFlying>();
            PoolManager.Instance.goldPool = new ObjectPool<GameObject>(() => { var go = new GameObject("test coin"); var c = go.AddComponent<MoneyFlying>(); coins.Add(c); return go; });
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GreedShrine.prefab");
            var purchased = typeof(MoneyUtility).GetField("chestsPurchased", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (int count in new[] { 0, 12 })
            {
                purchased.SetValue(null, count); int cost = MoneyUtility.GetChestPrice(); int expected = Mathf.Max(25, (int)(cost * .1f)); int previousCoins = coins.Count;
                var go = Instantiate(prefab, root.transform); var shrine = go.GetComponent<InteractableShrineGreed>();
                Check(shrine != null && shrine.statEffect != null && shrine.minimapIcon != null && shrine.alertIcon != null, "original prefab references");
                markers.Add(shrine.minimapIcon); markers.Add(shrine.alertIcon);
                Check(shrine.CanInteract() && shrine.ShowInDebug() && shrine.GetDebugName() == "Greed Shrines", "initial interaction and debug state");
                Check(shrine.Interact() && !shrine.Interact() && !shrine.CanInteract(), "interaction applies only once");
                Check(shrine.fx.activeSelf && shrine.fxLoop.activeSelf, "original effects enabled");
                var stat = shrine.statEffect.statModifier;
                Check(inventory.statInventory.permanentChanges.TryGetValue(stat.stat, out var modifiers) && modifiers.Count == (count == 0 ? 1 : 2), "original stat effect applied once");
                int total = 0; for (int i = previousCoins; i < coins.Count; i++) total += coins[i].value;
                Check(total == expected, "actual coin values: expected " + expected + ", got " + total);
                Check(UiManager.Instance.scoreUi.GetQueueCount() == (count == 0 ? 1 : 2), "score notification queued");
            }
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try { foreach (var marker in markers) Check(marker == null, "minimap and alert markers destroyed"); Debug.Log("GreedShrinePlayCheck passed: original prefab, single-use stat effects, minimum and scaled coin drops, score and marker removal"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
