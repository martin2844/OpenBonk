using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class ExtraUpgradeLevelsPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ExtraUpgradeLevelsPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("extra upgrade levels test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; inventory.itemInventory = new ItemInventory(); inventory.tomeInventory = new TomeInventory();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var items = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", flags).GetValue(data);
        var tomes = (Dictionary<ETome, TomeData>)typeof(DataManager).GetField("tomeData", flags).GetValue(data);
        var pot = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/PotSteel.asset"); var hat = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/WizardsHat.asset");
        var tome = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeDamage.asset"); var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponFireStaff.asset");
        Require(pot != null && hat != null && tome != null && weapon != null, "original assets"); items.Add(pot.eItem, pot); items.Add(hat.eItem, hat); tomes.Add(tome.eTome, tome);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            foreach (var item in new[] { pot, hat })
            {
                string description = item.GetDummyItem().GetDescription(item.localizedDescription);
                Require(description.Contains("10") && description.Contains("2") && description.Contains("9") && !description.Contains("{"), "original item description: " + description);
            }
            Require(weapon.GetMaxLevel() == 40 && tome.GetMaxLevel() == 99, "base level caps");
            inventory.itemInventory.AddItem(EItem.Pot); inventory.itemInventory.AddItem(EItem.WizardsHat);
            Require(weapon.GetMaxLevel() == 50 && tome.GetMaxLevel() == 109, "first stack raises actual asset caps");
            inventory.tomeInventory.tomeLevels[tome.eTome] = 99;
            inventory.tomeInventory.AddTome(tome, new List<StatModifier>(), ERarity.Common);
            Require(inventory.tomeInventory.GetTomeLevel(tome.eTome) == 100, "actual tome inventory upgrades past original cap");
            inventory.itemInventory.AddItem(EItem.Pot); inventory.itemInventory.AddItem(EItem.WizardsHat);
            Require(weapon.GetMaxLevel() == 59 && tome.GetMaxLevel() == 118, "second stack falls to nine extra levels");
            inventory.itemInventory.AddItem(EItem.Pot, 10); inventory.itemInventory.AddItem(EItem.WizardsHat, 10);
            Require(weapon.GetMaxLevel() == 100 && tome.GetMaxLevel() == 159, "stack falloff and two-level floor");
            for (int i = 0; i < 11; i++) { inventory.itemInventory.RemoveItem(EItem.Pot); inventory.itemInventory.RemoveItem(EItem.WizardsHat); }
            // The original OnInitOrAmountChanged adds the current stack's
            // bonus on every amount change, including partial removal.
            Require(weapon.GetMaxLevel() == 158 && tome.GetMaxLevel() == 217, "partial removal follows original cumulative behavior");
            inventory.itemInventory.RemoveItem(EItem.Pot); inventory.itemInventory.RemoveItem(EItem.WizardsHat);
            Require(weapon.GetMaxLevel() == 40 && tome.GetMaxLevel() == 99, "final removal restores base caps");
            Require(inventory.tomeInventory.GetTomeLevel(tome.eTome) == 100, "removal preserves already acquired tome levels");
            inventory.tomeInventory.AddTome(tome, new List<StatModifier>(), ERarity.Common);
            Require(inventory.tomeInventory.GetTomeLevel(tome.eTome) == 100, "tome above reduced cap cannot upgrade further");
            inventory.itemInventory.AddItem(EItem.Pot); inventory.itemInventory.AddItem(EItem.WizardsHat);
            Require(weapon.GetMaxLevel() == 50 && tome.GetMaxLevel() == 109, "reacquisition restores cap bonuses");
            inventory.tomeInventory.AddTome(tome, new List<StatModifier>(), ERarity.Common);
            Require(inventory.tomeInventory.GetTomeLevel(tome.eTome) == 101, "tome upgrade resumes after reacquisition");
            Debug.Log("ExtraUpgradeLevelsPortCheck passed: original assets, caps, real tome upgrades, diminishing stacks, removal and reacquisition");
        }
        finally { inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; DataManager.Instance = null; }
    }
}
