using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Tools;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BossSkinAchievementsPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BossSkinAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; float oldTime = MyTime.runTimer;
        var finalFlag = typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic); bool oldFinal = (bool)finalFlag.GetValue(null);
        var root = new GameObject("boss skin fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.character = ECharacter.Fox;
            var speed = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_skin_foxSpeedrun.asset"); var final = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_skin_foxFinalBoss.asset");
            Require(speed != null && final != null && speed.isEnabled && final.isEnabled, "original enabled Fox skin assets");
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); achievements.Add(speed.internalName, speed); achievements.Add(final.internalName, final);
            int[] expected = { 15000, 6, 0, 100, 50000, 100000, 250000, 500000 };
            for (int i = 0; i < expected.Length; i++) Require(AchievementGenerator.GetSkinAchValue((ESkinType)(i + 1)) == expected[i], "original skin threshold " + i);
            Require(AchievementGenerator.GetSkinAchValue(ESkinType.Crypt) == 0, "out of range returns zero");
            AchievementTracker.Init(); finalFlag.SetValue(null, false); MyTime.runTimer = 360.01f; InteractableBossSpawner.A_BossDefeated?.Invoke(true);
            Require(!save.progression.achievements.Contains(speed.internalName) && !save.progression.achievements.Contains(final.internalName), "after threshold, non-final stage");
            MyTime.runTimer = 360f; InteractableBossSpawner.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains(speed.internalName), "speedrun threshold inclusive");
            finalFlag.SetValue(null, true); MyTime.runTimer = 600f; FinalFightController.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Contains(final.internalName), "final boss unlocks final skin");
            save.progression.achievements.Clear(); AchievementTracker.Cleanup(); InteractableBossSpawner.A_BossDefeated?.Invoke(true); FinalFightController.A_BossDefeated?.Invoke(true); Require(save.progression.achievements.Count == 0, "cleanup removes callbacks");
            Debug.Log("BossSkinAchievementsPortCheck passed: original Fox assets, six-minute boundary, final boss, values and cleanup");
        }
        finally { AchievementTracker.Cleanup(); finalFlag.SetValue(null, oldFinal); MyTime.runTimer = oldTime; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
