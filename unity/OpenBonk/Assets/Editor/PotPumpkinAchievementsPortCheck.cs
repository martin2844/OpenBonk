using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class PotPumpkinAchievementsPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("PotPumpkinAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; bool oldRandom = RsgController.isCurrentMapRandomSeed;
        var oldStatuses = InteractablesStatus.interactablesByName; var mods = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); bool oldNoItems = mods.Contains("no_items");
        var root = new GameObject("pot and pumpkin achievement fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.character = ECharacter.Fox;
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game; typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(game, new object[] { true }); typeof(GameManager).GetProperty("cryptIndex").GetSetMethod(true).Invoke(game, new object[] { 0 });
            var entries = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_potSteel", "a_pumpkin" }) { var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(asset != null && asset.isEnabled, "original enabled " + name); entries.Add(name, asset); }
            InteractablesStatus.interactablesByName = new Dictionary<string, InteractablesStatus.InteractableStatusContainer>();
            var chests = new InteractablesStatus.InteractableStatusContainer(InteractableChest.debugNameCrypt) { numTotal = 1, numUsed = 1 };
            var pots = new InteractablesStatus.InteractableStatusContainer(InteractablePot.debugNameCrypt) { numTotal = 2, numUsed = 1 };
            var pumpkins = new InteractablesStatus.InteractableStatusContainer(InteractablePot.debugGraveyardName) { numTotal = 2, numUsed = 1 };
            InteractablesStatus.interactablesByName.Add(chests.debugName, chests); InteractablesStatus.interactablesByName.Add(pots.debugName, pots); InteractablesStatus.interactablesByName.Add(pumpkins.debugName, pumpkins);
            AchievementTracker.Init(); mods.Remove("no_items"); RsgController.isCurrentMapRandomSeed = true; InteractablesStatus.A_InteractableUsed?.Invoke(pots.debugName);
            Require(!save.progression.achievements.Contains("a_potSteel"), "incomplete crypt pots do not unlock"); pots.numUsed = 2;
            RsgController.isCurrentMapRandomSeed = false; InteractablesStatus.A_InteractableUsed?.Invoke(pots.debugName); Require(!save.progression.achievements.Contains("a_potSteel"), "fixed seed blocks pot");
            RsgController.isCurrentMapRandomSeed = true; mods.Add("no_items"); InteractablesStatus.A_InteractableUsed?.Invoke(chests.debugName); Require(!save.progression.achievements.Contains("a_potSteel"), "no-items challenge blocks pot");
            mods.Remove("no_items"); typeof(GameManager).GetProperty("cryptIndex").GetSetMethod(true).Invoke(game, new object[] { 1 }); InteractablesStatus.A_InteractableUsed?.Invoke(pots.debugName); Require(!save.progression.achievements.Contains("a_potSteel"), "later crypt blocks pot");
            typeof(GameManager).GetProperty("cryptIndex").GetSetMethod(true).Invoke(game, new object[] { 0 }); InteractablesStatus.A_InteractableUsed?.Invoke(chests.debugName); Require(save.progression.achievements.Contains("a_potSteel"), "first random crypt cleared unlocks pot");
            player.character = ECharacter.Roberto; InteractablesStatus.A_InteractableUsed?.Invoke(pumpkins.debugName); Require(!save.progression.achievements.Contains("a_pumpkin"), "unfinished pumpkins");
            pumpkins.numUsed = 2; player.character = ECharacter.Fox; InteractablesStatus.A_InteractableUsed?.Invoke(pumpkins.debugName); Require(!save.progression.achievements.Contains("a_pumpkin"), "wrong character");
            player.character = ECharacter.Roberto; InteractablesStatus.A_InteractableUsed?.Invoke(pumpkins.debugName); Require(save.progression.achievements.Contains("a_pumpkin"), "Roberto clears pumpkins");
            save.progression.achievements.Clear(); AchievementTracker.Cleanup(); InteractablesStatus.A_InteractableUsed?.Invoke(pumpkins.debugName); Require(save.progression.achievements.Count == 0, "event unsubscribed");
            Debug.Log("PotPumpkinAchievementsPortCheck passed: original achievements, crypt gates, completion and Roberto");
        }
        finally { AchievementTracker.Cleanup(); if (oldNoItems) mods.Add("no_items"); else mods.Remove("no_items"); InteractablesStatus.interactablesByName = oldStatuses; RsgController.isCurrentMapRandomSeed = oldRandom; MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
