using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Inventory__Items__Pickups;
using UnityEditor;
using UnityEngine;

public static class CrownAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CrownAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldXp = XpUtility.xpForLevelsTable;
        var root = new GameObject("crown fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var crown = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_hatCrown.asset"); Require(crown != null && crown.isEnabled, "original enabled crown asset");
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            achievements.Add(crown.internalName, crown); data.unsortedAchievements = new List<MyAchievement> { crown };
            XpUtility.Init(); int rank100 = XpUtility.LevelToXp(99);
            foreach (ECharacter character in Enum.GetValues(typeof(ECharacter))) save.progression.GetCharacterProgression(character).xp = rank100;
            save.progression.GetCharacterProgression(ECharacter.Roberto).xp = rank100 - 1;
            AchievementTracker.Init(); MainMenu.A_MenuOpened?.Invoke();
            Require(!save.progression.achievements.Contains(crown.internalName), "one character below rank 100 blocks unlock");
            save.progression.GetCharacterProgression(ECharacter.Roberto).xp = rank100; MainMenu.A_MenuOpened?.Invoke();
            Require(save.progression.achievements.Contains(crown.internalName), "all characters rank 100 unlocks crown");
            save.progression.achievements.Remove(crown.internalName); AchievementTracker.Cleanup(); MainMenu.A_MenuOpened?.Invoke();
            Require(!save.progression.achievements.Contains(crown.internalName), "cleanup unsubscribes menu event");
            Debug.Log("CrownAchievementPortCheck passed: actual crown asset, every character at rank 100, menu event and cleanup");
        }
        finally { AchievementTracker.Cleanup(); XpUtility.xpForLevelsTable = oldXp; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
