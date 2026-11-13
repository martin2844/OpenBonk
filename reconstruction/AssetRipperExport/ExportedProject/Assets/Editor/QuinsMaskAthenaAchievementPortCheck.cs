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

public static class QuinsMaskAthenaAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("QuinsMaskAthenaAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static; var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("Quin Athena achievement fixture"); root.SetActive(false); CharacterData other = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var achievement = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_hatClown.asset"); var athena = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Athena.asset");
            Require(achievement != null && achievement.isEnabled && achievement.statName == "quinsMaskKillsAsAthena" && achievement.targetValue == 500 && athena != null && athena.eCharacter == ECharacter.Athena, "original achievement and Athena assets");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); dict.Add(achievement.internalName, achievement); data.unsortedAchievements = new List<MyAchievement> { achievement }; save.stats.stats.Add(achievement.statName, new MyStat(achievement.statName, 499));
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.characterData = athena;
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init();
            var enemy = root.AddComponent<Enemy>();
            Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, "fixture")); Require(MyStats.GetStat(achievement.statName) == 499, "other damage source excluded");
            other = UnityEngine.Object.Instantiate(athena); other.eCharacter = ECharacter.Fox; player.inventory.characterData = other; Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, ItemQuinsMask.damageSource)); Require(MyStats.GetStat(achievement.statName) == 499, "other character excluded");
            player.inventory.characterData = athena; Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, ItemQuinsMask.damageSource)); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat(achievement.statName) == 500 && RunStats.GetStat(EMyStat.quinsMaskKillsAsAthena) == 1 && save.progression.achievements.Contains(achievement.internalName), "500th matching kill unlocks Clown Hat");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, ItemQuinsMask.damageSource)); Require(MyStats.GetStat(achievement.statName) == 500, "cleanup unsubscribes");
            Debug.Log("QuinsMaskAthenaAchievementPortCheck passed: original Athena and achievement assets, source and character filters, threshold and cleanup");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); if (other != null) UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(root); }
    }
}
