using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class ItemAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("ItemAchievementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("item achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance;
        ItemInventory first = null, second = null;
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_kevin", "a_hatPot" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null, "original achievement " + name); achievements.Add(name, asset);
            }
            Require(achievements["a_kevin"].targetValue == 3 && achievements["a_hatPot"].targetValue == 4, "original thresholds");
            var itemData = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            var crystalData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Leeching Crystal.asset");
            var potData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/PotSteel.asset");
            Require(crystalData != null && potData != null && crystalData.eItem == EItem.LeechingCrystal && potData.eItem == EItem.Pot, "original item mapping");
            itemData.Add(EItem.LeechingCrystal, crystalData); itemData.Add(EItem.Pot, potData);
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            first = new ItemInventory(); player.inventory.itemInventory = first;
            MyAchievements.A_TryUnlock += listener; AchievementTracker.Init(); AchievementTracker.Init();
            first.AddItem(EItem.LeechingCrystal, 2); Require(!MyAchievements.IsAchievementDone("a_kevin"), "two crystals excluded");
            first.AddItem(EItem.LeechingCrystal); Require(MyAchievements.IsAchievementDone("a_kevin") && unlocks == 1, "third crystal unlocks Kevin");
            player.character = ECharacter.Fox; first.AddItem(EItem.Pot, 4);
            Require(!MyAchievements.IsAchievementDone("a_hatPot"), "wrong character excluded");
            second = new ItemInventory(); player.inventory.itemInventory = second; player.character = ECharacter.Roberto;
            second.AddItem(EItem.Pot, 3); Require(!MyAchievements.IsAchievementDone("a_hatPot"), "three Roberto pots excluded");
            second.AddItem(EItem.Pot); Require(MyAchievements.IsAchievementDone("a_hatPot") && unlocks == 2, "four Roberto pots unlock hat");
            second.AddItem(EItem.Pot); Require(unlocks == 2, "duplicate unlock ignored");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear(); second.AddItem(EItem.Pot);
            Require(unlocks == 2 && !MyAchievements.IsAchievementDone("a_hatPot"), "cleanup removes handler");
            Debug.Log("ItemAchievementPortCheck passed: real item additions, original thresholds, character gate and cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener; first?.Cleanup(); second?.Cleanup();
            MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
