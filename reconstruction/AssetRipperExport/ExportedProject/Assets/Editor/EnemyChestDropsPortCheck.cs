using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyChestDropsPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double v) { value = v; } public override double NextDouble() => value; }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static T Call<T>(object o, string name, params object[] args) => (T)o.GetType().GetMethod(name, Private).Invoke(o, args);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static int ChestCount() { int count = 0; foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) if (go.name == "reward fixture(Clone)") count++; return count; }
    public static void Run()
    {
        var root = new GameObject("drop fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = Vector3.right * 5;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); stats.stats[EStat.PowerupChance] = 1f; player.inventory.playerStats = stats;
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
        var manager = root.AddComponent<EffectManager>(); var enemy = root.AddComponent<Enemy>();
        var prefab = new GameObject("reward fixture"); prefab.SetActive(false); manager.openChestNormal = prefab;
        var flag = typeof(MapController).GetField("isFinalBossStage", StaticPrivate);
        var modifiers = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", StaticPrivate).GetValue(null);
        var previousRandom = global::Utility.MyRandom.random;
        try {
            MapController.index = 0; flag.SetValue(null, false); MyTime.time = 210f;
            Check(Mathf.Abs(Call<float>(manager, "GetChestDropTimeMultiplier") - 1f) < .00001f, "time multiplier midpoint");
            Set(enemy, "enemyFlag", EEnemyFlag.None);
            Check(Mathf.Abs(Call<float>(manager, "GetChestDropChance", enemy) - .0005f) < .0000001f, "normal drop chance");
            Set(enemy, "enemyFlag", EEnemyFlag.Elite);
            Check(Mathf.Abs(Call<float>(manager, "GetChestDropChance", enemy) - .001f) < .0000001f, "elite chance");
            global::Utility.MyRandom.random = new FixedRandom(.99);
            Call(manager, "Awake"); Enemy.A_EnemyDied(enemy, null);
            Check(ChestCount() == 1 && Get<bool>(manager, "hasSpawnedFirstEliteChest"), "first elite guaranteed via death event");
            Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 1, "later elite not guaranteed");
            Set(enemy, "enemyFlag", EEnemyFlag.Boss); Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 2, "boss guaranteed");
            flag.SetValue(null, true); Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 2, "final stage boss not guaranteed");
            flag.SetValue(null, false); Set(enemy, "enemyFlag", EEnemyFlag.None); global::Utility.MyRandom.random = new FixedRandom(0);
            Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 3 && Get<float>(manager, "lastChestAtTime") == 210f, "random drop and timer reset");
            Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 3, "zero chance immediately after drop");
            modifiers.Add("no_items"); Set(enemy, "enemyFlag", EEnemyFlag.Boss); Enemy.A_EnemyDied(enemy, null); Check(ChestCount() == 3, "no-items challenge"); modifiers.Remove("no_items");
            Call(manager, "OnDestroy"); Check(Enemy.A_EnemyDied == null || Array.FindIndex(Enemy.A_EnemyDied.GetInvocationList(), d => d.Target == manager) < 0, "death listener cleanup");
            Debug.Log("EnemyChestDropsPortCheck passed: first elite, boss, final stage, random timing, challenge and event cleanup");
        } finally {
            modifiers.Remove("no_items"); global::Utility.MyRandom.random = previousRandom; MyPlayer.Instance = null; GameManager.Instance = null; EffectManager.Instance = null; flag.SetValue(null, false);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) if (go.name == "reward fixture(Clone)") UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
