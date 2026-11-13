using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class TickAchievementsPortCheck
{
    static void Require(bool b, string s) { if (!b) throw new Exception("TickAchievementsPortCheck: " + s); }
    static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    static FieldInfo Field(string name) => typeof(AchievementTracker).GetField(name, PrivateStatic);
    public static void Run()
    {
        var root = new GameObject("tick achievements check"); root.SetActive(false);
        var oldData = DataManager.Instance; var oldSave = SaveManager.Instance; var oldGame = GameManager.Instance;
        float oldFixed = MyTime.fixedDeltaTime, oldSwarm = MyTime.finalSwarmTimer;
        try
        {
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            foreach (string name in new[] { "a_aura", "a_ghost", "a_bob", "a_soulHarvester", "a_kills5000" }) { var a = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(a != null, name + " asset"); dict.Add(name, a); }
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            typeof(RunStats).GetMethod("Reset", PrivateStatic).Invoke(null, null);
            AchievementTracker.Init(); AchievementTracker.Init(); Field("noDamageTimer").SetValue(null, 119f); MyTime.fixedDeltaTime = 1f; MyTime.finalSwarmTimer = 0f;
            MyTime.A_Tick?.Invoke(); Require((float)Field("noDamageTimer").GetValue(null) == 119f, "idle timer");
            game.StartPlaying(); MyTime.A_Tick?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_aura"), "strict 120-second boundary");
            PlayerHealth.A_TakeDamage?.Invoke(null, null, false); Require((float)Field("noDamageTimer").GetValue(null) == 0f && (bool)Field("hasTakenDamageThisRun").GetValue(null), "damage resets timer");
            Enemy.A_Damage?.Invoke(null, null); Require((bool)Field("hasDealtDamageThisRun").GetValue(null), "enemy damage tracked");
            Field("noDamageTimer").SetValue(null, 120f); MyTime.A_Tick?.Invoke(); Require(MyAchievements.IsAchievementDone("a_aura"), "over 120 seconds");
            MyTime.finalSwarmTimer = 59.99f; MyTime.A_Tick?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_ghost"), "swarm boundary below");
            MyTime.finalSwarmTimer = 60f; MyTime.A_Tick?.Invoke(); Require(MyAchievements.IsAchievementDone("a_ghost") && !MyAchievements.IsAchievementDone("a_bob"), "60 seconds");
            MyTime.finalSwarmTimer = 120f; MyTime.A_Tick?.Invoke(); Require(MyAchievements.IsAchievementDone("a_bob") && !MyAchievements.IsAchievementDone("a_soulHarvester"), "two minutes");
            MyTime.finalSwarmTimer = 360f; MyTime.A_Tick?.Invoke(); Require(MyAchievements.IsAchievementDone("a_soulHarvester"), "six minutes");
            RunStats.AddValue(EMyStat.kills, 5000); MyTime.A_Tick?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_kills5000"), "5000 boundary");
            RunStats.AddValue(EMyStat.kills, 1); MyTime.A_Tick?.Invoke(); Require(MyAchievements.IsAchievementDone("a_kills5000"), "5001 kills");
            AchievementTracker.Cleanup(); Field("noDamageTimer").SetValue(null, 50f); PlayerHealth.A_TakeDamage?.Invoke(null, null, false); MyTime.A_Tick?.Invoke(); Require((float)Field("noDamageTimer").GetValue(null) == 50f, "cleanup");
            Debug.Log("TickAchievementsPortCheck passed: combat reset, survival, swarm and kill thresholds, event cleanup");
        }
        finally { AchievementTracker.Cleanup(); DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); GameManager.Instance = oldGame; MyTime.fixedDeltaTime = oldFixed; MyTime.finalSwarmTimer = oldSwarm; UnityEngine.Object.DestroyImmediate(root); }
    }
}
