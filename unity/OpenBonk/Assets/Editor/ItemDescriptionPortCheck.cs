using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ItemDescriptionPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ItemDescriptionPortCheck: " + message); }
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("item test"); root.SetActive(false); DataManager.Instance = root.AddComponent<DataManager>();
        var data = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(DataManager.Instance);
        var beaconData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beacon.asset"); var wrenchData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Wrench.asset");
        data.Add(EItem.Beacon, beaconData); data.Add(EItem.Wrench, wrenchData);
        var items = new ItemInventory(); var beacon = new ItemBeacon(items) { amount = 3 }; var wrench = new ItemWrench(items) { amount = 2 };
        items.items.Add(EItem.Beacon, beacon); items.items.Add(EItem.Wrench, wrench);
        Require(beaconData.rarity != wrenchData.rarity, "fixture covers different original rarities");
        Require(items.GetUniqueItemsInRarity(beaconData.rarity) == 1 && items.GetUniqueItemsInRarity(wrenchData.rarity) == 1, "counts distinct items rather than stacks");
        string shrine = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_Interactables_en.asset").GetEntry("CHARGE_SHRINE_NAME").GetLocalizedString();
        string beaconDescription = beacon.GetDescription(new LocalizedString("ItemsBlue", "Beacon_DESC"));
        string wrenchDescription = wrench.GetDescription(new LocalizedString("ItemsGray", "Wrench_DESC"));
        Require(beaconDescription == "Charging a " + shrine + " will turn it into a healing zone. Also spawn +2 extra " + shrine + " for future stages.", "Beacon description uses exported smart string");
        Require(wrenchDescription == shrine + ": Charge 4% faster and get 7.5% increased rewards.", "Wrench description uses exported smart string");
        items.RemoveItem(EItem.Beacon); Require(items.GetUniqueItemsInRarity(beaconData.rarity) == 1, "partial removal preserves distinct count");
        items.RemoveItem(EItem.Beacon); items.RemoveItem(EItem.Beacon); Require(items.GetUniqueItemsInRarity(beaconData.rarity) == 0, "last removal updates count");
        items.Cleanup(); UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("ItemDescriptionPortCheck passed: original rarity counts and Beacon/Wrench localized descriptions");
    }
}
