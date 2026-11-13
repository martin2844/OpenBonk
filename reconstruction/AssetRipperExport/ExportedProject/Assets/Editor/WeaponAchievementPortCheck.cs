using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class WeaponAchievementPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception("WeaponAchievementPortCheck: " + message); }
    private static WeaponBase Weapon(string assetName, int level)
    {
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/" + assetName + ".asset");
        Require(data != null, "original weapon " + assetName);
        var weapon = (WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase));
        weapon.weaponData = data; weapon.level = level; return weapon;
    }
    public static void Run()
    {
        var root = new GameObject("weapon achievement check"); root.SetActive(false);
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance;
        int unlocks = 0; Action<string> listener = _ => unlocks++;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var achievements = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            foreach (string name in new[] { "a_durationTome", "a_wirelessDaggers", "a_wizardsHat", "a_hatMagic" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset");
                Require(asset != null, "original achievement " + name); achievements.Add(name, asset);
            }
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.weaponInventory = new WeaponInventory();
            var axe = Weapon("WeaponAxe", 9); var lightning = Weapon("WeaponLightningBolt", 14);
            var fire = Weapon("WeaponFireStaff", 40); var blood = Weapon("WeaponBloodMagic", 39);
            Require(axe.weaponData.eWeapon == EWeapon.Axe && lightning.weaponData.eWeapon == EWeapon.LightningStaff && fire.weaponData.eWeapon == EWeapon.FireStaff && blood.weaponData.eWeapon == EWeapon.BloodMagic, "weapon enum mapping");
            player.inventory.weaponInventory.weapons.Add(EWeapon.FireStaff, fire);
            player.inventory.weaponInventory.weapons.Add(EWeapon.LightningStaff, lightning);
            player.inventory.weaponInventory.weapons.Add(EWeapon.BloodMagic, blood);
            MyAchievements.A_TryUnlock += listener;
            AchievementTracker.Init(); AchievementTracker.Init();
            player.character = ECharacter.Calcium;
            WeaponInventory.A_WeaponAdded?.Invoke(axe); Require(!MyAchievements.IsAchievementDone("a_durationTome"), "axe below threshold");
            axe.level = 10; WeaponInventory.A_WeaponAdded?.Invoke(axe); Require(MyAchievements.IsAchievementDone("a_durationTome"), "axe threshold");
            WeaponInventory.A_WeaponAdded?.Invoke(lightning); Require(!MyAchievements.IsAchievementDone("a_wirelessDaggers"), "lightning below threshold");
            lightning.level = 15; WeaponInventory.A_WeaponAdded?.Invoke(lightning); Require(MyAchievements.IsAchievementDone("a_wirelessDaggers"), "lightning threshold");
            fire.level = 41; WeaponInventory.A_WeaponAdded?.Invoke(fire); Require(!MyAchievements.IsAchievementDone("a_wizardsHat"), "Vlad required");
            player.character = ECharacter.Vlad; fire.level = 40; WeaponInventory.A_WeaponAdded?.Invoke(fire); Require(!MyAchievements.IsAchievementDone("a_wizardsHat"), "Vlad level 40 excluded");
            fire.level = 41; WeaponInventory.A_WeaponAdded?.Invoke(fire); Require(MyAchievements.IsAchievementDone("a_wizardsHat"), "Vlad level 41");
            lightning.level = 40; blood.level = 39; WeaponInventory.A_WeaponAdded?.Invoke(blood); Require(!MyAchievements.IsAchievementDone("a_hatMagic"), "Fox required");
            player.character = ECharacter.Fox; WeaponInventory.A_WeaponAdded?.Invoke(blood); Require(!MyAchievements.IsAchievementDone("a_hatMagic"), "all three weapons required");
            blood.level = 40; WeaponInventory.A_WeaponAdded?.Invoke(blood); Require(MyAchievements.IsAchievementDone("a_hatMagic") && unlocks == 4, "Fox with all three weapons at threshold");
            WeaponInventory.A_WeaponAdded?.Invoke(blood); Require(unlocks == 4, "duplicate unlock ignored");
            AchievementTracker.Cleanup(); save.progression.achievements.Clear(); WeaponInventory.A_WeaponAdded?.Invoke(axe);
            Require(unlocks == 4 && !MyAchievements.IsAchievementDone("a_durationTome"), "cleanup removes handler");
            Debug.Log("WeaponAchievementPortCheck passed: weapon and character thresholds, event path and cleanup");
        }
        finally
        {
            AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= listener;
            MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
