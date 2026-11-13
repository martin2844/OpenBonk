using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class PotAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("PotAchievementPortCheck: " + message); }

    public static void Run()
    {
        var root = new GameObject("pot achievement check"); root.SetActive(false);
        var previousSave = SaveManager.Instance; var previousData = DataManager.Instance;
        var previousPlayer = MyPlayer.Instance; int previousStage = MapController.index;
        int unlocked = 0;
        Action<string> listener = name => { if (name == "a_xpTome") unlocked++; };
        try
        {
            var save = root.AddComponent<SaveManager>();
            save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_xpTome.asset");
            Require(asset != null && asset.targetValue == 20, "original achievement threshold");
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            achievements.Add(asset.internalName, asset);
            var pot = root.AddComponent<InteractablePot>();
            MyAchievements.A_TryUnlock += listener;
            AchievementTracker.Init(); AchievementTracker.Init(); TrackStats.Init();
            DetectInteractables.A_Interacted?.Invoke(pot, false);
            Require(RunStats.GetStat("potsBroken") == 0 && unlocked == 0, "failed interaction ignored");
            for (int i = 0; i < 19; i++) DetectInteractables.A_Interacted?.Invoke(pot, true);
            Require(RunStats.GetStat("potsBroken") == 19 && !MyAchievements.IsAchievementDone("a_xpTome"), "nineteen pots do not unlock");
            DetectInteractables.A_Interacted?.Invoke(pot, true);
            Require(RunStats.GetStat("potsBroken") == 20 && MyAchievements.IsAchievementDone("a_xpTome") && unlocked == 1, "twentieth pot unlocks once");
            DetectInteractables.A_Interacted?.Invoke(pot, true);
            Require(unlocked == 1, "subsequent pot does not unlock again");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear();
            DetectInteractables.A_Interacted?.Invoke(pot, true);
            Require(unlocked == 1 && !MyAchievements.IsAchievementDone("a_xpTome"), "cleanup removes handler");
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.statInventory = new StatInventory();
            var giveLuck = typeof(InteractablePot).GetMethod("GiveLuck", BindingFlags.Instance | BindingFlags.NonPublic);
            MapController.index = 0; pot.isBig = false; giveLuck.Invoke(pot, null);
            MapController.index = 1; pot.isBig = true; giveLuck.Invoke(pot, null);
            var modifiers = player.inventory.statInventory.permanentChanges[EStat.Luck];
            Require(modifiers.Count == 2 && modifiers[0].modifyType == EStatModifyType.Flat && Mathf.Abs(modifiers[0].modification - .0025f) < .000001f, "first stage small pot luck");
            Require(modifiers[1].modifyType == EStatModifyType.Flat && Mathf.Abs(modifiers[1].modification - .0075f) < .000001f, "second stage large pot luck");
            Debug.Log("PotAchievementPortCheck passed: original threshold, event path, cleanup and pot luck scaling");
        }
        finally
        {
            TrackStats.Cleanup(); AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener;
            DataManager.Instance = previousData;
            MyPlayer.Instance = previousPlayer; MapController.index = previousStage;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
