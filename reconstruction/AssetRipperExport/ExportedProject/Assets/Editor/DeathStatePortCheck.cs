using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class DeathStatePortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("DeathStatePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("death state fixture"); root.SetActive(false);
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        player.inventory.playerStats.stats = new Dictionary<EStat, float> { [EStat.HealingMultiplier] = 1f };
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        player.inventory.playerHealth = health; health.hp = health.maxHp = 100;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(PlayerHealth).GetField("checkDeadInterval", flags).SetValue(health, 1f);
        typeof(PlayerHealth).GetField("healInterval", flags).SetValue(health, float.PositiveInfinity);
        var gameOver = typeof(GameManager).GetField("<isGameOver>k__BackingField", flags);
        int deaths = 0; Action onDied = () => { deaths++; gameOver.SetValue(game, true); };
        PlayerHealth.A_Died += onDied;
        try
        {
            MyTime.time = 10f; health.Tick(); Check(deaths == 0, "living player");
            health.hp = 0; health.shield = 5; health.overheal = 3;
            MyTime.time = 10.5f; health.Tick(); Check(deaths == 0, "one-second check interval");
            MyTime.time = 11f; health.Tick(); Check(deaths == 1 && health.hp == 0 && health.shield == 0 && health.overheal == 0, "unreported death and health cleanup");
            MyTime.time = 12f; health.Tick(); Check(deaths == 1, "no repeated death after game over");
            var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            var bossDead = typeof(EnemyManager).GetField("<stageBossIsDead>k__BackingField", flags);
            var finalStage = typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic);
            bossDead.SetValue(enemies, true); finalStage.SetValue(null, false); Check(!game.IsFinalBossDead(), "ordinary stage gate");
            finalStage.SetValue(null, true); bossDead.SetValue(enemies, false); Check(!game.IsFinalBossDead(), "living final boss");
            bossDead.SetValue(enemies, true); Check(game.IsFinalBossDead(), "defeated final boss");
            finalStage.SetValue(null, false);
            Debug.Log("DeathStatePortCheck passed: periodic death recovery, game-over deduplication, final-stage boss gate");
        }
        finally { PlayerHealth.A_Died -= onDied; UnityEngine.Object.DestroyImmediate(root); }
    }
}
