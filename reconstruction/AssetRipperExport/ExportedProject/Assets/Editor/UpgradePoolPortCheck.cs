using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts._Data;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using UnityEngine;
using Utility;
using Assets.Scripts.Utility;

public sealed class UpgradePoolNextEncounterCheck : BaseEncounterWindow
{
    public int opened;
    public int closed;
    public override void Open(EEncounter kind) { if (kind != EEncounter.ChestNormal) throw new Exception("unexpected queued encounter"); opened++; }
    public override void OnClose() { closed++; }
    public override void ChooseOffer(int index) { }
}

public static class UpgradePoolPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var save = new GameObject("pool-save").AddComponent<SaveManager>();
        save.Init();
        save.progression = new ProgressionSaveFile();
        save.progression.shopItems[EShopItem.Weapons] = 0;
        save.progression.shopItems[EShopItem.Tomes] = 0;
        var data = new GameObject("pool-data").AddComponent<DataManager>();
        DataManager.Instance = data;
        data.unsortedItems = new List<ItemData>();
        var ws = ScriptableObject.CreateInstance<ShopItemData>(); ws.eShopItem = EShopItem.Weapons;
        var ts = ScriptableObject.CreateInstance<ShopItemData>(); ts.eShopItem = EShopItem.Tomes;
        data.shopItems.Add(EShopItem.Weapons, ws); data.shopItems.Add(EShopItem.Tomes, ts);
        var manager = new GameObject("pool-game").AddComponent<GameManager>(); GameManager.Instance = manager;
        var player = new GameObject("pool-player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.weaponInventory = new WeaponInventory();
        player.inventory.tomeInventory = new TomeInventory();
        player.inventory.itemInventory = new Assets.Scripts.Inventory__Items__Pickups.Items.ItemInventory();
        var weapons = (Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
        var tomes = (Dictionary<ETome, TomeData>)typeof(DataManager).GetField("tomeData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
        var fire = ScriptableObject.CreateInstance<WeaponData>(); fire.eWeapon = EWeapon.FireStaff; fire.isEnabled = true; fire.damage = 10; fire.Init(); weapons.Add(fire.eWeapon, fire);
        var bone = ScriptableObject.CreateInstance<WeaponData>(); bone.eWeapon = EWeapon.Bone; bone.isEnabled = true; bone.Init(); weapons.Add(bone.eWeapon, bone);
        var damage = ScriptableObject.CreateInstance<TomeData>(); damage.eTome = ETome.Damage; damage.isEnabled = true; damage.statModifier = new StatModifier { stat = EStat.DamageMultiplier }; tomes.Add(damage.eTome, damage);
        var speed = ScriptableObject.CreateInstance<TomeData>(); speed.eTome = ETome.Agility; speed.isEnabled = true; speed.statModifier = new StatModifier { stat = EStat.MoveSpeedMultiplier }; tomes.Add(speed.eTome, speed);
        var achievement = ScriptableObject.CreateInstance<MyAchievement>(); achievement.isEnabled = true; achievement.internalName = "test_unlock";
        achievement.localizedDescription = new UnityEngine.Localization.LocalizedString();
        bone.AchievementRequirement = achievement; bone.price = 10;
        RunUnlockables.banishedUpgradables = new HashSet<UnlockableBase>();
        Require(!MyAchievements.IsAvailable(bone), "locked purchase");
        save.progression.purchases.Add(bone.GetInternalName());
        Require(!MyAchievements.IsAvailable(bone), "achievement still locked");
        save.progression.achievements.Add("test_unlock");
        Require(MyAchievements.IsAvailable(bone), "purchased achievement unlock");
        save.progression.inactivated.Add(bone.GetInternalName());
        Require(!MyAchievements.IsAvailable(bone), "deactivated unlock");
        player.inventory.weaponInventory.weapons.Add(bone.eWeapon, new WeaponBase(bone));
        Require(UnlockUtility.GetAvailableWeapons().Contains(bone), "owned weapon retained despite deactivation");
        Require(data.GetAllWeapons().Count == 2 && data.GetAllTomes().Count == 2, "data listings");
        RunUnlockables.BanishUpgradable(bone);
        Require(!UnlockUtility.GetAvailableWeapons().Contains(bone), "banished owned weapon excluded");
        RunUnlockables.banishedUpgradables.Clear();
        player.inventory.weaponInventory.weapons.Add(fire.eWeapon, new WeaponBase(fire));
        player.inventory.tomeInventory.tomeLevels.Add(damage.eTome, 1);
        player.inventory.tomeInventory.tomeLevels.Add(speed.eTome, 1);
        MyRandom.random = new System.Random(12);
        var offers = InventoryUtility.GetRandomUpgrades();
        Require(offers.Count == 3 && new HashSet<IUpgradable>(offers).Count == 3, "three unique offers");
        foreach (var offer in offers) Require(offer.GetLevel() > 0, "full slots only offer owned choices");
        player.inventory.weaponInventory.weapons[fire.eWeapon].level = 40;
        RunUnlockables.BanishUpgradable(speed);
        for (int seed = 0; seed < 20; seed++)
        {
            MyRandom.random = new System.Random(seed);
            var filtered = InventoryUtility.GetRandomUpgrades();
            Require(filtered.Count == 2 && !filtered.Contains(fire) && !filtered.Contains(speed), "maxed and banished excluded");
        }
        // The original draws from separate owned/new pools, consuming a category
        // roll only while both pools have entries (InventoryUtility RVA 0x2259b81).
        player.inventory.weaponInventory.weapons[fire.eWeapon].level = 1;
        player.inventory.weaponInventory.weapons.Remove(bone.eWeapon);
        player.inventory.tomeInventory.tomeLevels.Remove(speed.eTome);
        save.progression.inactivated.Remove(bone.GetInternalName());
        RunUnlockables.banishedUpgradables.Clear();
        for (int seed = 0; seed < 200; seed++)
        {
            var expectedRandom = new System.Random(seed);
            var ownedPool = new List<IUpgradable> { fire, damage };
            var newPool = new List<IUpgradable> { bone, speed };
            var expected = new List<IUpgradable>();
            for (int i = 0; i < 3; i++)
            {
                List<IUpgradable> pool;
                if (newPool.Count == 0) pool = ownedPool;
                else if (ownedPool.Count == 0) pool = newPool;
                else pool = expectedRandom.NextDouble() < (double)(1f / 3f) ? ownedPool : newPool;
                int index = expectedRandom.Next(0, pool.Count);
                expected.Add(pool[index]);
                pool.RemoveAt(index);
            }
            int nextExpected = expectedRandom.Next();
            MyRandom.random = new System.Random(seed);
            var actual = InventoryUtility.GetRandomUpgrades();
            Require(actual.Count == expected.Count, "mixed offer count, seed " + seed);
            for (int i = 0; i < actual.Count; i++)
                Require(ReferenceEquals(actual[i], expected[i]), "mixed offer order, seed " + seed + " index " + i);
            Require(MyRandom.random.Next() == nextExpected, "mixed offer random state, seed " + seed);
        }
        // Late in a run, every other choice can be maxed or banished. The
        // remaining option must still be offered without indexing an empty pool.
        RunUnlockables.BanishUpgradable(bone);
        RunUnlockables.BanishUpgradable(damage);
        RunUnlockables.BanishUpgradable(speed);
        MyRandom.random = new System.Random(21);
        var lastChoice = InventoryUtility.GetRandomUpgrades();
        Require(lastChoice.Count == 1 && ReferenceEquals(lastChoice[0], fire), "sole remaining upgrade offered");
        player.inventory.weaponInventory.weapons[fire.eWeapon].level = 40;
        Require(InventoryUtility.GetRandomUpgrades().Count == 0, "no offer when all upgrades unavailable");
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        health.hp = 100; player.inventory.playerHealth = health;
        var ui = new GameObject("pool-ui").AddComponent<UiManager>(); UiManager.Instance = ui;
        var encounters = ui.gameObject.AddComponent<EncounterWindows>(); ui.encounterWindows = encounters;
        var screen = ui.gameObject.AddComponent<LevelupScreen>();
        var picker = ui.gameObject.AddComponent<UpgradePicker>();
        encounters.levelupScreen = screen; screen.upgradePicker = picker; picker.levelupScreen = screen;
        screen.window = new GameObject("empty reward window"); screen.window.SetActive(false);
        picker.buttons = Array.Empty<UpgradeButton>();
        encounters.AddEncounter(EEncounter.Levelup);
        Require(!encounters.encounterInProgress && !screen.window.activeSelf && !LevelupScreen.isLevelingUp,
            "empty level-up closes and releases encounter");
        var next = ui.gameObject.AddComponent<UpgradePoolNextEncounterCheck>();
        encounters.chestWindow = next;
        var queued = (Queue<EEncounter>)typeof(EncounterWindows).GetField("rewardQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(encounters);
        queued.Enqueue(EEncounter.Levelup);
        queued.Enqueue(EEncounter.ChestNormal);
        typeof(EncounterWindows).GetMethod("PopReward", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(encounters, null);
        Require(next.opened == 1 && encounters.encounterInProgress && MyTime.paused && !screen.window.activeSelf,
            "queued chest opens and keeps game paused after empty level-up");
        encounters.RewardFinished();
        Require(next.closed == 1 && !encounters.encounterInProgress && !MyTime.paused,
            "queued chest closes and resumes game");
        UnityEngine.Object.DestroyImmediate(screen.window); UnityEngine.Object.DestroyImmediate(ui.gameObject);
        UiManager.Instance = null;
        RunUnlockables.Init();
        GameManager.A_RunStarted?.Invoke();
        Require(RunUnlockables.banishedUpgradables.Count == 0, "banishes reset on new run");
        RunUnlockables.Cleanup();
        Debug.Log("UpgradePoolPortCheck passed: unlocks, purchases, activation, owned gear, slot limits, banishment and selection");
    }
}
