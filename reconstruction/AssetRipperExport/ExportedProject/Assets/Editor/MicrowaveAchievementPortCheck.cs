using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class MicrowaveAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("MicrowaveAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("microwave achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance;
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_noelle", "a_poisonGloves" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null && asset.targetValue == 3, "original achievement " + name); achievements.Add(name, asset);
            }
            MyAchievements.A_TryUnlock += listener; AchievementTracker.Init(); AchievementTracker.Init();
            GameManager.A_StageStarted?.Invoke();
            InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal); InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal);
            InteractableMicrowave.A_Used?.Invoke(EItem.MoldyCheese);
            InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal); InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal);
            Require(unlocks == 0, "different item resets both streaks");
            GameManager.A_StageStarted?.Invoke(); InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal);
            Require(unlocks == 0, "new stage resets streak");
            InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal); InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal);
            Require(MyAchievements.IsAchievementDone("a_noelle") && unlocks == 1, "three consecutive ice crystals unlock Noelle");
            InteractableMicrowave.A_Used?.Invoke(EItem.IceCrystal);
            InteractableMicrowave.A_Used?.Invoke(EItem.MoldyCheese); InteractableMicrowave.A_Used?.Invoke(EItem.MoldyCheese);
            Require(!MyAchievements.IsAchievementDone("a_poisonGloves"), "two cheeses insufficient");
            InteractableMicrowave.A_Used?.Invoke(EItem.MoldyCheese);
            Require(MyAchievements.IsAchievementDone("a_poisonGloves") && unlocks == 2, "three consecutive cheeses unlock Poison Gloves");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear();
            InteractableMicrowave.A_Used?.Invoke(EItem.MoldyCheese);
            Require(unlocks == 2 && save.progression.achievements.Count == 0, "cleanup removes handler");
            Debug.Log("MicrowaveAchievementPortCheck passed: streaks, interruption, stage reset and cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
