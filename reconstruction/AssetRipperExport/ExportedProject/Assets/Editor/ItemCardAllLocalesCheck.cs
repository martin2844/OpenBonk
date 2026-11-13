using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class ItemCardAllLocalesCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    public static void Run()
    {
        var previous = DataManager.Instance;
        var menu = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Additive);
        var map = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Additive);
        GameObject cloned = null;
        try
        {
            DataManager data = null; UpgradeButton source = null;
            foreach (var root in menu.GetRootGameObjects()) if (data == null) data = root.GetComponentInChildren<DataManager>(true);
            foreach (var root in map.GetRootGameObjects()) if (source == null) source = root.GetComponentInChildren<UpgradeButton>(true);
            if (data == null || source == null || source.t_description == null) throw new Exception("Missing scene bindings");
            DataManager.Instance = data;
            var dict = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            foreach (var item in data.unsortedItems) if (item != null) dict[item.eItem] = item;
            cloned = UnityEngine.Object.Instantiate(source.gameObject); cloned.SetActive(false);
            var card = cloned.GetComponent<UpgradeButton>();
            LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
            LocalizationSettings.StringDatabase.TableProvider = new Provider();
            var errors = new List<string>(); int checkedCount = 0;
            foreach (string lang in new[] { "cs", "de", "en", "es", "es-419", "fr", "it", "ja", "ko", "pl", "pt", "ru", "th", "tr", "uk", "zh-Hans", "zh-Hant" })
            {
                LocalizationSettings.SelectedLocale = Locale.CreateLocale(lang);
                foreach (var item in data.unsortedItems)
                {
                    if (item == null || !item.isEnabled) continue;
                    try
                    {
                        card.SetItem(item);
                        string description = card.t_description.text;
                        if (string.IsNullOrWhiteSpace(description) || description.Contains("{") || description.Contains("Missing localization") || description.Contains("No translation found"))
                            errors.Add(lang + ": " + item.name + " = " + description);
                        if (item.name == "Battery" || item.name == "Borgar" || item.name == "Oats" || item.name == "Pumpkin")
                            Debug.Log("Item card sample " + lang + " " + item.name + ": " + description);
                        checkedCount++;
                    }
                    catch (Exception ex) { errors.Add(lang + ": " + item.name + " threw " + ex.GetType().Name + ": " + ex.Message); }
                }
            }
            Debug.Log("Item card descriptions checked=" + checkedCount + " errors=" + errors.Count);
            foreach (var error in errors) Debug.LogError("Item card description failure: " + error);
            if (errors.Count > 0) throw new Exception("Item card description failures: " + errors.Count);
        }
        finally
        {
            DataManager.Instance = previous;
            if (cloned != null) UnityEngine.Object.DestroyImmediate(cloned);
            EditorSceneManager.CloseScene(map, true); EditorSceneManager.CloseScene(menu, true);
        }
    }
}
