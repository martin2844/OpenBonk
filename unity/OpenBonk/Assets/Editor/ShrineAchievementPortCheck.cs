using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class ShrineAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("ShrineAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("shrine achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; int oldIndex = MapController.index;
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_suckyMagnet", "a_chaosTome" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null, "original achievement " + name); achievements.Add(name, asset);
            }
            MyAchievements.A_TryUnlock += listener; AchievementTracker.Init(); AchievementTracker.Init();
            GameManager.A_RunStarted?.Invoke(); MapController.index = 2;
            ChargeShrine.A_ChargeShrineSpawned?.Invoke(); ChargeShrine.A_ChargeShrineSpawned?.Invoke();
            ChargeShrine.A_Charged?.Invoke(true);
            Require(unlocks == 0, "both shrines required");
            ChargeShrine.A_Charged?.Invoke(false);
            Require(MyAchievements.IsAchievementDone("a_suckyMagnet") && !MyAchievements.IsAchievementDone("a_chaosTome") && unlocks == 1, "interruption excludes chaos tome");
            save.progression.achievements.Clear(); GameManager.A_RunStarted?.Invoke(); MapController.index = 1;
            ChargeShrine.A_ChargeShrineSpawned?.Invoke(); ChargeShrine.A_Charged?.Invoke(true);
            Require(MyAchievements.IsAchievementDone("a_suckyMagnet") && !MyAchievements.IsAchievementDone("a_chaosTome"), "third stage gate");
            MapController.index = 2; ChargeShrine.A_Charged?.Invoke(true);
            Require(MyAchievements.IsAchievementDone("a_chaosTome") && unlocks == 3, "third stage and uninterrupted count");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear(); GameManager.A_RunStarted?.Invoke();
            ChargeShrine.A_ChargeShrineSpawned?.Invoke(); ChargeShrine.A_Charged?.Invoke(true);
            Require(unlocks == 3 && save.progression.achievements.Count == 0, "cleanup removes handlers");
            Debug.Log("ShrineAchievementPortCheck passed: count, interruption, stage gate, reset and cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; MapController.index = oldIndex; DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
