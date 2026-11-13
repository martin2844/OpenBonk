using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

public static class XpMultiplierBatchPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("xp batch fixture"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldTable = XpUtility.xpForLevelsTable;
        Action<PlayerXp,int> oldXp = PlayerXp.A_XpAdded; Action<int> oldLevel = PlayerXp.A_LevelUp;
        try
        {
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game; game.player = player;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
            inventory.playerXp = new PlayerXp(); inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inventory.playerHealth.hp = 100;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.XpIncreaseMultiplier] = 1.25f }; inventory.playerStats = stats;
            XpUtility.Init(); int events = 0, lastGain = -1, levels = 0, lastLevel = -1;
            PlayerXp.A_XpAdded = (xp, gained) => { if (xp == inventory.playerXp) { events++; lastGain = gained; } };
            PlayerXp.A_LevelUp = level => { levels++; lastLevel = level; };
            inventory.AddXp(1); inventory.AddXp(1); Check(inventory.playerXp.xp == 0 && events == 0, "queued before LateUpdate");
            inventory.LateUpdate(); Check(inventory.playerXp.xp == 2 && events == 1 && lastGain == 2, "first batch and fractional carry");
            inventory.AddXp(1); inventory.AddXp(1); inventory.LateUpdate(); Check(inventory.playerXp.xp == 5 && events == 2 && lastGain == 3, "fraction carried to next batch");
            inventory.AddXp(2); inventory.LateUpdate(); Check(inventory.playerXp.xp == 7 && levels == 1 && lastLevel == 1, "level boundary and notification");
            stats.stats[EStat.XpIncreaseMultiplier] = 100f; inventory.AddXp(1); inventory.LateUpdate(); Check(lastGain == 10, "10x multiplier cap");
            int before = inventory.playerXp.xp; inventory.playerHealth.hp = 0; inventory.AddXp(10); inventory.LateUpdate(); Check(inventory.playerXp.xp == before, "dead player cannot queue XP");
            Debug.Log("XpMultiplierBatchPortCheck passed: batching, fractional carry, level event, cap and death gate");
        }
        finally { PlayerXp.A_XpAdded = oldXp; PlayerXp.A_LevelUp = oldLevel; XpUtility.xpForLevelsTable = oldTable; MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; UnityEngine.Object.DestroyImmediate(root); }
    }
}
