using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class CombatAchievementsPortCheck {
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("CombatAchievementsPortCheck: " + msg); }
    static void Flag(Enemy e, EEnemyFlag value) => typeof(Enemy).GetField("enemyFlag", P).SetValue(e, value);
    static void SpawnTime(Enemy e, float value) => typeof(Enemy).GetProperty("spawnedAtTime").GetSetMethod(true).Invoke(e, new object[] { value });
    public static void Run() {
        var root = new GameObject("combat achievement fixture"); root.SetActive(false);
        try {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var achievements = new Dictionary<string,MyAchievement>();
            foreach (string name in new[] { "a_tacticalGlasses", "a_bossBuster", "a_hatSheriff", "a_quinsMask", "a_roberto" }) { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Check(asset != null, name + " asset"); achievements.Add(name, asset); }
            typeof(DataManager).GetField("achievementsData", P).SetValue(data, achievements);
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; var enemy = root.AddComponent<Enemy>(); var dc = new DamageContainer(1f, "other");
            AchievementTracker.Init(); MyTime.time = 100f; enemy.teleportTime = .25f;
            Flag(enemy, EEnemyFlag.None); SpawnTime(enemy, 100f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(save.progression.achievements.Count == 0, "ordinary enemy does not unlock timed boss awards");
            Flag(enemy, EEnemyFlag.Boss); SpawnTime(enemy, 74f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(!MyAchievements.IsAchievementDone("a_tacticalGlasses"), "slow boss excluded");
            SpawnTime(enemy, 75f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(MyAchievements.IsAchievementDone("a_tacticalGlasses"), "non-stage boss at 25 seconds qualifies");
            Flag(enemy, EEnemyFlag.StageBoss); SpawnTime(enemy, 69f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(!MyAchievements.IsAchievementDone("a_bossBuster"), "slow stage boss excluded");
            SpawnTime(enemy, 70f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(MyAchievements.IsAchievementDone("a_bossBuster"), "stage boss at 30 seconds qualifies");
            player.character = ECharacter.Athena; Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(!MyAchievements.IsAchievementDone("a_quinsMask"), "wrong damage source excluded");
            dc.damageSource = EWeapon.Aegis.ToString(); Flag(enemy, EEnemyFlag.Boss); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(!MyAchievements.IsAchievementDone("a_quinsMask"), "non-stage boss excluded from Athena award");
            Flag(enemy, EEnemyFlag.StageBoss); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(MyAchievements.IsAchievementDone("a_quinsMask"), "Athena Aegis stage boss qualifies");
            player.character = ECharacter.Cl4nk; Flag(enemy, EEnemyFlag.Boss); SpawnTime(enemy, 98.7f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(!MyAchievements.IsAchievementDone("a_hatSheriff"), "Cl4nk boss slower than one second after teleport excluded");
            SpawnTime(enemy, 98.75f); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(MyAchievements.IsAchievementDone("a_hatSheriff"), "Cl4nk boss at one second after teleport qualifies");
            var ghost = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/GhostInvincible.asset"); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ghost });
            Flag(enemy, EEnemyFlag.None); Enemy.A_EnemyDied?.Invoke(enemy, dc); Check(MyAchievements.IsAchievementDone("a_roberto"), "ghost death unlocks Roberto");
            AchievementTracker.Cleanup(); Debug.Log("CombatAchievementsPortCheck passed: timed bosses, Athena, Cl4nk and Roberto");
        } finally { AchievementTracker.Cleanup(); MyPlayer.Instance = null; DataManager.Instance = null; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
