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

public static class SpecialKillAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("SpecialKillAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static; var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("special kill achievement fixture"); root.SetActive(false); CharacterData other = null;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var pilot = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_hatPilot.asset"); var idle = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_idleJuice.asset"); var tornado = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_birdo.asset");
            var birdo = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Birdo.asset"); var fox = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); var wisp = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Wisp_0.asset");
            Require(pilot != null && pilot.isEnabled && pilot.statName == "birdoFlyingMinesKills" && pilot.targetValue == 1000 && idle != null && idle.isEnabled && idle.statName == "standingStillKills" && idle.targetValue == 500 && tornado != null && tornado.isEnabled && tornado.statName == "killsInTornadoWithTornado" && tornado.targetValue == 100 && birdo != null && birdo.eCharacter == ECharacter.Birdo && fox != null && wisp != null, "original assets");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); data.unsortedAchievements = new List<MyAchievement> { pilot, idle, tornado };
            foreach (var ach in data.unsortedAchievements) { dict.Add(ach.internalName, ach); save.stats.stats.Add(ach.statName, new MyStat(ach.statName, ach.targetValue - 1)); }
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.characterData = fox; var movement = root.AddComponent<PlayerMovement>(); player.playerMovement = movement;
            var avg = typeof(PlayerMovement).GetField("avgVelocity", BindingFlags.NonPublic | BindingFlags.Instance); avg.SetValue(movement, 0.1f);
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true; MyAchievements.Init(); TrackStats.Init();
            var enemy = root.AddComponent<Enemy>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { wisp });
            Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, EWeapon.Mine.ToString())); Require(MyStats.GetStat(pilot.statName) == 999 && MyStats.GetStat(idle.statName) == 499 && MyStats.GetStat(tornado.statName) == 99, "Fox mine and velocity exactly 0.1 excluded");
            player.inventory.characterData = birdo; Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, EWeapon.Tornado.ToString())); Require(MyStats.GetStat(pilot.statName) == 999 && MyStats.GetStat(tornado.statName) == 99, "wrong source and no tornado contact excluded");
            Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, EWeapon.Mine.ToString())); Require(MyStats.GetStat(pilot.statName) == 1000, "Birdo mine kill counted without flight check");
            movement.TouchingTornado(); Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, EWeapon.Mine.ToString())); Require(MyStats.GetStat(tornado.statName) == 99, "touching tornado with other damage excluded");
            Enemy.A_EnemyDied?.Invoke(enemy, new DamageContainer(1f, EWeapon.Tornado.ToString())); Require(MyStats.GetStat(tornado.statName) == 100, "tornado kill while touching counts");
            avg.SetValue(movement, 0.099f); Enemy.A_EnemyDied?.Invoke(enemy, null); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat(idle.statName) == 500 && save.progression.achievements.Contains(pilot.internalName) && save.progression.achievements.Contains(idle.internalName) && save.progression.achievements.Contains(tornado.internalName), "three original achievement thresholds");
            TrackStats.Cleanup(); Enemy.A_EnemyDied?.Invoke(enemy, null); Require(MyStats.GetStat(idle.statName) == 500, "cleanup unsubscribes");
            Debug.Log("SpecialKillAchievementPortCheck passed: three original achievement assets, exact velocity threshold and character, source and tornado filters");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); if (other != null) UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(root); }
    }
}
