using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class FoxWispAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("FoxWispAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static; var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("Fox Wisp achievement fixture"); root.SetActive(false); CharacterData other = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_dragonsBreath.asset"); var athena = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
            Require(achievement != null && achievement.isEnabled && achievement.statName == "foxWispsKills" && achievement.targetValue == 1000 && athena != null && athena.eCharacter == ECharacter.Fox, "original achievement and Fox assets");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); dict.Add(achievement.internalName, achievement); data.unsortedAchievements = new List<MyAchievement> { achievement }; save.stats.stats.Add(achievement.statName, new MyStat(achievement.statName, 999));
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.characterData = athena;
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init();
            var enemy = root.AddComponent<Enemy>(); var wisp = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Wisp_0.asset"); Require(wisp != null && wisp.enemyName == Actors.Enemies.EEnemy.Wisp, "original Wisp asset");
            var setData = typeof(Enemy).GetProperty("enemyData").GetSetMethod(true); setData.Invoke(enemy, new object[] { wisp });
            other = UnityEngine.Object.Instantiate(athena); other.eCharacter = ECharacter.Athena; player.inventory.characterData = other; Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(achievement.statName) == 999, "other character excluded");
            player.inventory.characterData = athena; var otherEnemy = UnityEngine.Object.Instantiate(wisp); otherEnemy.enemyName = Actors.Enemies.EEnemy.Goblin; setData.Invoke(enemy, new object[] { otherEnemy }); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(achievement.statName) == 999, "other enemy excluded"); UnityEngine.Object.DestroyImmediate(otherEnemy);
            setData.Invoke(enemy, new object[] { wisp }); Enemy.A_EnemyDied?.Invoke(enemy, null); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat(achievement.statName) == 1000 && RunStats.GetStat(EMyStat.foxWispsKills) == 1 && save.progression.achievements.Contains(achievement.internalName), "1000th matching kill unlocks Dragon's Breath");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(achievement.statName) == 1000, "cleanup unsubscribes");
            Debug.Log("FoxWispAchievementPortCheck passed: original Fox, Wisp and achievement assets, enemy and character filters, null damage source, threshold and cleanup");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); if (other != null) UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(root); }
    }
}
