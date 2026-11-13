using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts._Data;
using Assets.Scripts._Data.ShopItems;
using UnityEngine;

public static class AutoUpgradePortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("AutoUpgradePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("auto upgrade check"); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.progression = new ProgressionSaveFile();
        save.progression.shopItems[EShopItem.Weapons] = 0; save.progression.shopItems[EShopItem.Tomes] = 0;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var weaponShop = ScriptableObject.CreateInstance<ShopItemData>(); weaponShop.eShopItem = EShopItem.Weapons;
        var tomeShop = ScriptableObject.CreateInstance<ShopItemData>(); tomeShop.eShopItem = EShopItem.Tomes;
        data.shopItems.Add(EShopItem.Weapons, weaponShop); data.shopItems.Add(EShopItem.Tomes, tomeShop);
        GameManager.Instance = root.AddComponent<GameManager>();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.weaponInventory = new WeaponInventory(); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.Luck] = 0f }; inventory.playerStats = stats;
        var upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
        upgradeData.upgradeModifiers = new List<StatModifier> { new StatModifier { stat = EStat.DamageMultiplier, modification = 1f, modifyType = EStatModifyType.Flat } };
        var fire = ScriptableObject.CreateInstance<WeaponData>(); fire.eWeapon = EWeapon.FireStaff; fire.damage = 10; fire.isEnabled = true; fire.upgradeData = upgradeData; fire.Init();
        ((Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", P).GetValue(data)).Add(fire.eWeapon, fire);
        var weapon = new WeaponBase(fire); inventory.weaponInventory.weapons.Add(fire.eWeapon, weapon);
        var oldRandom = global::Utility.MyRandom.random; var oldBanishes = RunUnlockables.banishedUpgradables;
        var windows = root.AddComponent<EncounterWindows>(); windows.levelupScreen = root.AddComponent<LevelupScreen>();
        var progress = typeof(EncounterWindows).GetField("<encounterInProgress>k__BackingField", P); progress.SetValue(windows, true);
        var active = typeof(EncounterWindows).GetField("activeEncounterWindow", P);
        var queue = (Queue<EEncounter>)typeof(EncounterWindows).GetField("rewardQueue", P).GetValue(windows);
        var onLevel = typeof(EncounterWindows).GetMethod("OnLevelUp", P);
        try
        {
            RunUnlockables.banishedUpgradables = new HashSet<UnlockableBase>(); global::Utility.MyRandom.random = new System.Random(9);
            save.config.cfGameSettings.auto_select_upgrades = 1; save.config.cfGameSettings.auto_select_after_level = 10f;
            windows.currentLevel = 10; UnityEngine.Random.InitState(11); onLevel.Invoke(windows, new object[] { 12 });
            Require(weapon.level == 3 && queue.Count == 0 && windows.currentLevel == 12, "two automatic choices apply real upgrades without opening a level window");
            Require(weapon.GetValue(EStat.DamageMultiplier) > 10f, "automatic choice applies weapon stat modifiers");
            active.SetValue(windows, windows.levelupScreen); onLevel.Invoke(windows, new object[] { 13 });
            Require(weapon.level == 3 && queue.Count == 1, "active level window queues instead of auto selecting");
            queue.Clear(); active.SetValue(windows, null); save.config.cfGameSettings.auto_select_upgrades = 0;
            onLevel.Invoke(windows, new object[] { 14 });
            Require(weapon.level == 3 && queue.Count == 1, "disabled option queues upgrade");
            queue.Clear(); save.config.cfGameSettings.auto_select_upgrades = 1; save.config.cfGameSettings.auto_select_after_level = 100;
            onLevel.Invoke(windows, new object[] { 15 });
            Require(weapon.level == 3 && queue.Count == 1, "below threshold queues upgrade");
            Debug.Log("AutoUpgradePortCheck passed: applied weapon upgrades, auto threshold, disabled setting and active-window queue");
        }
        finally
        {
            inventory.itemInventory.Cleanup(); weapon.Cleanup(); RunUnlockables.banishedUpgradables = oldBanishes; global::Utility.MyRandom.random = oldRandom;
            UnityEngine.Object.DestroyImmediate(fire); UnityEngine.Object.DestroyImmediate(upgradeData); UnityEngine.Object.DestroyImmediate(weaponShop); UnityEngine.Object.DestroyImmediate(tomeShop);
            UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null;
        }
    }
}
