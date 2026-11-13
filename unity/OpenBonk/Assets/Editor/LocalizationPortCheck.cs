using System;
using System.Collections.Generic;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts._Data.Tomes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalizationPortCheck
{
    private sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            table.SharedData.TableCollectionName = name;
            table.LocaleIdentifier = locale.Identifier;
            if (name == "Stats")
            {
                table.AddEntry(EStat.DamageMultiplier + "_NAME", "Damage");
                table.AddEntry(EStat.DamageMultiplier + "_DESC", "More damage");
            }
            else if (name == "Tomes")
            {
                var entry = table.AddEntry("TOME_BALANCE_DESC", "Gain {value}");
                entry.IsSmart = true;
            }
            else if (name == "Maps")
            {
                table.AddEntry("NAME", "Forest");
                table.AddEntry("DESC", "The forest map");
            }
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    public static void Run()
    {
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        var locale = Locale.CreateLocale("en");
        LocalizationSettings.Instance = settings;
        LocalizationSettings.SelectedLocale = locale;
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var tome = ScriptableObject.CreateInstance<TomeData>();
        var map = ScriptableObject.CreateInstance<MapData>();
        var mapObject = new GameObject("MapEntryCheck");
        try
        {
            Require(LocalizationUtility.IsEnglish(), "English locale");
            Require(LocalizationUtility.GetStatName(EStat.DamageMultiplier) == "Damage", "stat name");
            Require(LocalizationUtility.GetStatDesc(EStat.DamageMultiplier) == "More damage", "stat description");
            Require(LocalizationUtility.GetLocalizedString("Stats", "Missing") == "Missing localization for Stats.Missing", "missing entry");
            Require(LocalizationUtility.GetLocalizedString("Stats", "Missing", "Default") == "Default", "English default");
            Require(LocalizationUtility.GetLocalizedString("Tomes", "TOME_BALANCE_DESC", new Dictionary<string, string> { ["value"] = "+5%" }) == "Gain +5%", "smart-string argument");
            tome.eTome = ETome.Damage;
            tome.statModifier = new StatModifier { stat = EStat.DamageMultiplier };
            Require(tome.GetDescription() == "+Damage - More damage", "ordinary tome description");
            map.eMap = Assets.Scripts._Data.MapsAndStages.EMap.Forest;
            map.localizedName = new LocalizedString("Maps", "NAME");
            map.localizedDescription = new LocalizedString("Maps", "DESC");
            var mapEntry = mapObject.AddComponent<MapEntry>();
            mapEntry.t_name = new GameObject("Name", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            mapEntry.t_name.transform.SetParent(mapObject.transform);
            mapEntry.t_description = new GameObject("Description", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            mapEntry.t_description.transform.SetParent(mapObject.transform);
            mapEntry.Set(map);
            Require(map.GetInternalName() == "Forest" && mapEntry.t_name.text == "Forest" && mapEntry.t_description.text == "The forest map", "map entry text");
            Debug.Log("LocalizationPortCheck passed: table lookup, fallback, smart strings, tome description, and map entry text");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tome);
            UnityEngine.Object.DestroyImmediate(mapObject);
            UnityEngine.Object.DestroyImmediate(map);
            LocalizationSettings.Instance = null;
            UnityEngine.Object.DestroyImmediate(settings);
            UnityEngine.Object.DestroyImmediate(locale);
        }
    }
}
