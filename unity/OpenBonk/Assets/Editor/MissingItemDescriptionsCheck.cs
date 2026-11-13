using System;
using System.Globalization;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class MissingItemDescriptionsCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Check(ItemBase item, string table, string key, string value)
    {
        string text = item.GetDescription(new LocalizedString(table, key));
        if (string.IsNullOrEmpty(text) || text.Contains("{") || !text.Contains(value)) throw new Exception(item.GetType().Name + " description: " + text);
        Debug.Log("Description " + LocalizationSettings.SelectedLocale.Identifier.Code + " " + item.GetType().Name + ": " + text);
    }
    static void CheckLocale(string code)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(code == "es" ? "es-ES" : "en-US");
        LocalizationSettings.SelectedLocale = Locale.CreateLocale(code);
        Check(new ItemBattery(null), "ItemUtility", "ITEM_DESC_STAT_INCREASE", "8%");
        Check(new ItemForbiddenJuice(null), "ItemUtility", "ITEM_DESC_STAT_INCREASE", "10%");
        Check(new ItemSlipperyRing(null), "ItemUtility", "ITEM_DESC_STAT_INCREASE", "15%");
        Check(new ItemGymSauce(null), "ItemUtility", "ITEM_DESC_STAT_INCREASE", "10%");
        Check(new ItemOats(null), "ItemUtility", "ITEM_DESC_STAT_INCREASE", "25");
        Check(new ItemKey(null), "ItemsGray", "Key_DESC", "10%");
        Check(new ItemGoldenGlove(null), "ItemsGray", "GoldenGlove_DESC", "15%");
        Check(new ItemBorgor(null), "ItemsGray", "Borgar_DESC", "2%");
        Check(new ItemEchoShard(null), "ItemsBlue", "EchoShard_DESC", "12%");
        Check(new ItemPumpkin(null), "ItemsBlue", "Pumpkin_DESC", "18%");
        Check(new ItemTimeBracelet(null), "ItemsGray", "TimeBracelet_DESC", "8%");
        Check(new ItemBackpack(null), "ItemsBlue", "Backpack_DESC", "+1");
        Check(new ItemDragonfire(null), "ItemsYellow", "Dragonfire_DESC", "15%");
        Check(new ItemChonkplate(null), "ItemsYellow", "Chonkplate_DESC", "75%");
        Check(new ItemSluttyCannon(null), "ItemsPink", "SluttyCannon_DESC", "20%");
        Check(new ItemLightningOrb(null), "ItemsYellow", "LightningOrb_DESC", "25%");
        Check(new ItemKevin(null), "ItemsBlue", "Kevin_DESC", "+25%");
    }
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        CheckLocale("en"); CheckLocale("es");
        Debug.Log("MissingItemDescriptionsCheck PASS: English and Spanish");
    }
    public static void RunDefaultTables()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if (settings == null) throw new Exception("Project localization settings are missing");
        LocalizationSettings.Instance = settings;
        if (LocalizationSettings.StringDatabase.TableProvider != null) throw new Exception("Expected default table lookup");
        CheckLocale("en"); CheckLocale("es");
        Debug.Log("MissingItemDescriptionsCheck default table lookup PASS: English and Spanish");
    }
    [Serializable] sealed class InventoryEntry { public string name; public string asset; public bool enabled; }
    [Serializable] sealed class InventoryManifest { public InventoryEntry[] items; }
    public static void AuditInventory()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var json = System.IO.File.ReadAllText("/home/your-user/megabonk/analysis/port-inventory/inventory.json");
        var manifest = JsonUtility.FromJson<InventoryManifest>(json);
        int checkedCount = 0, failures = 0;
        foreach (string code in new[] { "en", "es", "es-419" }) {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(code == "es-419" ? "es-AR" : code == "es" ? "es-ES" : "en-US");
            LocalizationSettings.SelectedLocale = Locale.CreateLocale(code);
            foreach (var entry in manifest.items) {
                if (!entry.enabled) continue;
                var asset = entry.asset; int index = asset.IndexOf("Assets/", StringComparison.Ordinal);
                try {
                    if (index < 0) throw new Exception("invalid asset path");
                    var data = AssetDatabase.LoadAssetAtPath<ItemData>(asset.Substring(index));
                    if (data == null) throw new Exception("missing item asset");
                    string text = data.GetDescription();
                    if (string.IsNullOrWhiteSpace(text) || text.Contains("{") || text.Contains("No translation found")) throw new Exception("invalid description: " + text);
                    Debug.Log("ItemDescriptionAudit OK " + code + " " + entry.name + ": " + text);
                } catch (Exception e) { failures++; Debug.Log("ItemDescriptionAudit FAIL " + code + " " + entry.name + ": " + e.Message); }
                checkedCount++;
            }
        }
        Debug.Log($"ItemDescriptionAudit RESULT checked={checkedCount}, failures={failures}");
        if (failures > 0) throw new Exception("Item description audit failed");
    }

}
