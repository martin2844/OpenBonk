using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MoaiEncounterIntegrationCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("MoaiEncounterIntegrationCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Moai integration"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var original = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/UnstableTransfusion.asset");
        Require(original != null, "original item asset exists");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(original.eItem, original);
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.Luck] = 1f }; inventory.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 100; inventory.playerHealth = health;
        inventory.itemInventory = new ItemInventory();
        var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui;
        var encounters = root.AddComponent<EncounterWindows>(); ui.encounterWindows = encounters;
        var screen = root.AddComponent<LevelupScreen>(); var picker = root.AddComponent<UpgradePicker>();
        encounters.levelupScreen = screen; screen.upgradePicker = picker; picker.levelupScreen = screen;
        screen.window = new GameObject("reward window"); screen.window.SetActive(false);
        picker.buttons = new UpgradeButton[3];
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("offer " + i, typeof(RectTransform), typeof(Button)); go.SetActive(false);
            picker.buttons[i] = go.AddComponent<UpgradeButton>(); picker.buttons[i].upgradePicker = picker;
        }
        var oldPools = RunUnlockables.availableItems; var oldRandom = global::Utility.MyRandom.random;
        var clones = new List<ItemData>();
        try
        {
            RunUnlockables.availableItems = new Dictionary<EItemRarity, List<ItemData>>();
            for (int rarity = 0; rarity < 4; rarity++)
            {
                var pool = new List<ItemData>();
                for (int i = 0; i < 4; i++) { var item = UnityEngine.Object.Instantiate(original); item.rarity = (EItemRarity)rarity; clones.Add(item); pool.Add(item); }
                RunUnlockables.availableItems.Add((EItemRarity)rarity, pool);
            }
            global::Utility.MyRandom.random = new System.Random(42);
            var luckField = typeof(UpgradePicker).GetField("moaiLuckMode", P);
            var itemField = typeof(UpgradeButton).GetField("itemData", P);
            int[] seeds = { -1, -1, -1, -1 };
            for (int seed = 0; seed < 5000; seed++)
            {
                UnityEngine.Random.InitState(seed); int mode = UnityEngine.Random.Range(0, 3);
                if (UnityEngine.Random.Range(0f, 1f) <= .01f) mode = 3;
                if (seeds[mode] < 0) seeds[mode] = seed;
            }
            for (int mode = 0; mode < 4; mode++)
            {
                Require(seeds[mode] >= 0, "seed found for mode " + mode);
                UnityEngine.Random.InitState(seeds[mode]);
                encounters.AddEncounter(EEncounter.Moai);
                Require(encounters.encounterInProgress && screen.window.activeSelf, "Moai window opens for mode " + mode);
                Require((int)luckField.GetValue(picker) == mode, "rolled luck reaches picker");
                int count = mode == 0 ? 2 : 3;
                Require(picker.GetNumUpgrades() == count, "correct number of item offers for mode " + mode);
                var offered = new HashSet<ItemData>();
                for (int i = 0; i < count; i++)
                {
                    var item = (ItemData)itemField.GetValue(picker.buttons[i]);
                    Require(picker.buttons[i].gameObject.activeSelf && item != null && offered.Add(item), "active distinct item offer");
                    if (mode == 3) Require(item.rarity == EItemRarity.Legendary, "special mode offers legendary items");
                }
                picker.banishCooldownOverAtTime = -1f;
                picker.buttons[0].SelectUpgrade();
                Require(inventory.itemInventory.GetAmount(original.eItem) == mode + 1 && !encounters.encounterInProgress && !screen.window.activeSelf, "selection adds item and closes encounter");
            }
            Debug.Log("MoaiEncounterIntegrationCheck passed: reward queue, all four modes, offer counts, legendary offers and item selection");
        }
        finally
        {
            inventory.itemInventory.Cleanup(); RunUnlockables.availableItems = oldPools; global::Utility.MyRandom.random = oldRandom;
            foreach (var item in clones) UnityEngine.Object.DestroyImmediate(item);
            foreach (var button in picker.buttons) UnityEngine.Object.DestroyImmediate(button.gameObject);
            UnityEngine.Object.DestroyImmediate(screen.window); UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; UiManager.Instance = null;
        }
    }
}
