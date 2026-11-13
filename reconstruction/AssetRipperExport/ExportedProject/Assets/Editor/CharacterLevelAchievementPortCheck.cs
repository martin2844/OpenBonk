using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class CharacterLevelAchievementPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("CharacterLevelAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("character level achievement check"); root.SetActive(false); var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldSave = SaveManager.Instance;
        try
        {
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            var names = new[] { "a_armorTome", "a_bloodyCleaver", "a_demonicSoul" }; var characters = new[] { ECharacter.SirOofie, ECharacter.Vlad, ECharacter.Calcium }; var assets = new MyAchievement[3];
            for (int i = 0; i < names.Length; i++) { assets[i] = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + names[i] + ".asset"); Require(assets[i] != null, "original asset " + names[i]); dict.Add(names[i], assets[i]); }
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            int unlocks = 0; Action<string> listener = _ => unlocks++; MyAchievements.A_TryUnlock += listener;
            try
            {
                AchievementTracker.Init(); AchievementTracker.Init();
                for (int i = 0; i < assets.Length; i++)
                {
                    player.character = ECharacter.Fox; PlayerXp.A_LevelUp?.Invoke(assets[i].targetValue); Require(!MyAchievements.IsAchievementDone(names[i]), "wrong character " + names[i]);
                    player.character = characters[i]; PlayerXp.A_LevelUp?.Invoke(assets[i].targetValue - 1); Require(!MyAchievements.IsAchievementDone(names[i]), "below threshold " + names[i]);
                    PlayerXp.A_LevelUp?.Invoke(assets[i].targetValue); Require(MyAchievements.IsAchievementDone(names[i]), "threshold " + names[i]);
                    PlayerXp.A_LevelUp?.Invoke(assets[i].targetValue + 1); Require(unlocks == i + 1, "only one event per unlock");
                }
                save.progression.achievements.Clear(); AchievementTracker.Cleanup(); player.character = ECharacter.Vlad; PlayerXp.A_LevelUp?.Invoke(assets[1].targetValue); Require(!MyAchievements.IsAchievementDone(names[1]), "cleanup unsubscribes");
            }
            finally { AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; }
            Debug.Log("CharacterLevelAchievementPortCheck passed: three original assets, character and level gates, duplicates and cleanup");
        }
        finally { MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
