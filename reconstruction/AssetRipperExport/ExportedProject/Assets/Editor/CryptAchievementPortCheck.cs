using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Tools;
using UnityEditor;
using UnityEngine;

public static class CryptAchievementPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("CryptAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("crypt achievement check"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldSave = SaveManager.Instance; bool oldRandom = RsgController.isCurrentMapRandomSeed;
        var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_skin_robinetteCrypt.asset");
        try
        {
            Require(asset != null && asset.targetValue == 21, "original Robinette crypt achievement");
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            ((Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data)).Add(asset.internalName, asset);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.character = ECharacter.Robinette;
            Require(AchievementGenerator.GetSkinAchievementName(ECharacter.Robinette, ESkinType.Crypt) == asset.internalName, "original crypt key format");
            Require(AchievementGenerator.GetSkinAchievementName(ECharacter.Robinette, ESkinType.FinalBoss) == "a_skin_robinetteFinalBoss", "other skin key format");
            int unlocks = 0; Action<string> listener = _ => unlocks++; MyAchievements.A_TryUnlock += listener;
            try
            {
                AchievementTracker.Init(); AchievementTracker.Init(); RsgController.isCurrentMapRandomSeed = false; InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(10f); Require(!MyAchievements.IsAchievementDone(asset.internalName), "fixed seed cannot unlock");
                RsgController.isCurrentMapRandomSeed = true; InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(21.01f); Require(!MyAchievements.IsAchievementDone(asset.internalName), "slower than target cannot unlock");
                InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(21f); Require(MyAchievements.IsAchievementDone(asset.internalName) && unlocks == 1, "threshold unlocks once");
                InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(10f); Require(unlocks == 1, "repeat does not unlock twice");
                save.progression.achievements.Clear(); AchievementTracker.Cleanup(); InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(10f); Require(!MyAchievements.IsAchievementDone(asset.internalName), "cleanup removes event handler");
            }
            finally { AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; }
            Debug.Log("CryptAchievementPortCheck passed: original skin keys, random-seed gate, timing threshold and event lifecycle");
        }
        finally { MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); RsgController.isCurrentMapRandomSeed = oldRandom; UnityEngine.Object.DestroyImmediate(root); }
    }
}
