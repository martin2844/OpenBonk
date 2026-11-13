using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class CactusThornsAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CactusThornsAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; bool oldLoaded = SaveManager.loaded; var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("cactus thorns fixture"); root.SetActive(false); EnemyData other = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_cactus.asset"); var cactus = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/CactusShooter_0.asset");
            Require(achievement != null && achievement.isEnabled && achievement.statName == "cactusKillsWithThorns" && achievement.targetValue == 100 && cactus != null && cactus.enemyName == EEnemy.CactusShooter, "original achievement and cactus assets");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); dict.Add(achievement.internalName, achievement); data.unsortedAchievements = new List<MyAchievement> { achievement }; save.stats.stats.Add(achievement.statName, new MyStat(achievement.statName, 99));
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init();
            var enemy = root.AddComponent<Enemy>(); var setData = typeof(Enemy).GetProperty("enemyData").GetSetMethod(true); setData.Invoke(enemy, new object[] { cactus });
            Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, "FireStaff")); Require(MyStats.GetStat(achievement.statName) == 99, "non-thorns cactus kill excluded");
            other = UnityEngine.Object.Instantiate(cactus); other.enemyName = EEnemy.Goblin; setData.Invoke(enemy, new object[] { other }); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, PlayerHealth.thornsDamageSource)); Require(MyStats.GetStat(achievement.statName) == 99, "thorns on another enemy excluded");
            setData.Invoke(enemy, new object[] { cactus }); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, PlayerHealth.thornsDamageSource)); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat(achievement.statName) == 100 && RunStats.GetStat(EMyStat.cactusKillsWithThorns) == 1 && save.progression.achievements.Contains(achievement.internalName), "100th matching kill unlocks Cactus");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, PlayerHealth.thornsDamageSource)); Require(MyStats.GetStat(achievement.statName) == 100, "cleanup unsubscribes");
            Debug.Log("CactusThornsAchievementPortCheck passed: original cactus and achievement, source/enemy filters, threshold and cleanup");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); if (other != null) UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(root); }
    }
}
