using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class GhostBossAchievementPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("GhostBossAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("ghost boss achievement check"); root.SetActive(false); var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldSave = SaveManager.Instance; float oldTimer = MyTime.stageTimer;
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var names = new[] { "a_scythe", "a_hatSunglasses" }; var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            foreach (var name in names) { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(asset != null, "original asset " + name); dict.Add(name, asset); }
            Require(dict[names[1]].targetValue == 60, "original time threshold");
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; var boss = root.AddComponent<Enemy>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(boss, new object[] { enemyData });
            var otherObj = new GameObject("other enemy"); otherObj.SetActive(false); var other = otherObj.AddComponent<Enemy>(); var room = root.AddComponent<GraveyardBossRoom>(); typeof(GraveyardBossRoom).GetField("bossEnemy", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(room, boss);
            var death = typeof(GraveyardBossRoom).GetMethod("OnEnemyDied", BindingFlags.NonPublic | BindingFlags.Instance); int unlocks = 0; Action<string> listener = _ => unlocks++; MyAchievements.A_TryUnlock += listener;
            try
            {
                AchievementTracker.Init(); AchievementTracker.Init(); enemyData.enemyName = EEnemy.GhostKing; player.character = ECharacter.Fox; MyTime.stageTimer = 10f;
                death.Invoke(room, new object[] { other, null }); Require(unlocks == 0, "nonboss death ignored");
                death.Invoke(room, new object[] { boss, null }); Require(MyAchievements.IsAchievementDone(names[0]) && !MyAchievements.IsAchievementDone(names[1]) && unlocks == 1, "ghost king unlocks scythe for any character");
                player.character = ECharacter.Calcium; MyTime.stageTimer = 60.01f; death.Invoke(room, new object[] { boss, null }); Require(!MyAchievements.IsAchievementDone(names[1]), "late kill does not unlock sunglasses");
                MyTime.stageTimer = 60f; death.Invoke(room, new object[] { boss, null }); Require(MyAchievements.IsAchievementDone(names[1]) && unlocks == 2, "Calcium threshold unlocks sunglasses");
                death.Invoke(room, new object[] { boss, null }); Require(unlocks == 2, "duplicate death suppressed");
                save.progression.achievements.Clear(); AchievementTracker.Cleanup(); death.Invoke(room, new object[] { boss, null }); Require(unlocks == 2, "cleanup unsubscribes");
            }
            finally { AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; UnityEngine.Object.DestroyImmediate(otherObj); }
            Debug.Log("GhostBossAchievementPortCheck passed: actual room death event, original assets, character/time gates and cleanup");
        }
        finally { MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); MyTime.stageTimer = oldTimer; UnityEngine.Object.DestroyImmediate(enemyData); UnityEngine.Object.DestroyImmediate(root); }
    }
}
