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

public static class CharacterKillStatPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CharacterKillStatPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static; var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("character kill stats fixture"); root.SetActive(false); CharacterData other = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var fox = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); var birdo = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Birdo.asset"); var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Wisp_0.asset");
            Require(fox != null && fox.eCharacter == ECharacter.Fox && birdo != null && birdo.eCharacter == ECharacter.Birdo && enemyData != null, "original character and enemy assets");
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.character = birdo.eCharacter; player.inventory.characterData = fox;
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; TrackStats.Init();
            var enemy = root.AddComponent<Enemy>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
            string foxKey = StatTrackingUtility.GetKeyKillsCharacter(fox.eCharacter), birdoKey = StatTrackingUtility.GetKeyKillsCharacter(birdo.eCharacter);
            Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(birdoKey) == 1 && RunStats.GetStat(birdoKey) == 1 && MyStats.GetStat(foxKey) == 0, "selected character, rather than inventory data, owns kill");
            player.character = fox.eCharacter; Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(foxKey) == 1 && RunStats.GetStat(foxKey) == 1, "selected character changes kill counter");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(foxKey) == 1, "cleanup unsubscribes");
            Debug.Log("CharacterKillStatPortCheck passed: original Fox, Birdo and Wisp assets, selected-character kill counts and cleanup");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); if (other != null) UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(root); }
    }
}
