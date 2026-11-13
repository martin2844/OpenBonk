using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class BossKillStatsAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BossKillStatsAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static; var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var finalFlag = typeof(MapController).GetField("isFinalBossStage", flags); bool oldFinal = (bool)finalFlag.GetValue(null);
        var root = new GameObject("boss kill stats fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var energy = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_energyCore.asset"); var calcium = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Calcium.asset");
            Require(energy != null && energy.isEnabled && energy.statName == "finalBossKills" && energy.targetValue == 2 && calcium != null, "original Energy Core and Calcium assets");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data); dict.Add(energy.internalName, energy); data.unsortedAchievements = new List<MyAchievement> { energy };
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.characterData = calcium;
            var enemy = root.AddComponent<Enemy>(); var enemyFlag = typeof(Enemy).GetField("enemyFlag", BindingFlags.NonPublic | BindingFlags.Instance);
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init();
            enemyFlag.SetValue(enemy, EEnemyFlag.Boss); finalFlag.SetValue(null, true); Enemy.A_EnemyDied?.Invoke(enemy, null);
            Require(MyStats.GetStat("minibossKillsCalcium") == 1 && MyStats.GetStat("finalBossKills") == 0, "Calcium miniboss counts without final-boss credit");
            enemyFlag.SetValue(enemy, EEnemyFlag.StageBoss); finalFlag.SetValue(null, false); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat("finalBossKills") == 0, "stage boss outside final stage excluded");
            finalFlag.SetValue(null, true); Enemy.A_EnemyDied?.Invoke(enemy, null); next.SetValue(null, -1f); MyAchievements.Update(); Require(MyStats.GetStat("finalBossKills") == 1 && !save.progression.achievements.Contains("a_energyCore"), "one final boss below threshold");
            Enemy.A_EnemyDied?.Invoke(enemy, null); next.SetValue(null, -1f); MyAchievements.Update(); Require(MyStats.GetStat("finalBossKills") == 2 && save.progression.achievements.Contains("a_energyCore"), "two final stage bosses unlock Energy Core");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat("finalBossKills") == 2, "cleanup removes death subscriber");
            Debug.Log("BossKillStatsAchievementPortCheck passed: boss classifications and original Energy Core threshold");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); finalFlag.SetValue(null, oldFinal); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
