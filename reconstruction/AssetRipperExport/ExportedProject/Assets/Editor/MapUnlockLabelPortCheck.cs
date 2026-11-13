using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class MapUnlockLabelPortCheck
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
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.Instance = settings;
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        if (map == null) throw new Exception("Original Forest map data missing");
        foreach (var spec in new[] { ("en", "Map"), ("es", "Mapa"), ("es-419", "Mapa") })
        {
            LocalizationSettings.SelectedLocale = Locale.CreateLocale(spec.Item1);
            string label = map.GetUnlockableTypeDisplayString();
            if (label != spec.Item2) throw new Exception($"Map label {spec.Item1}: '{label}', expected '{spec.Item2}'");
        }
        Debug.Log("MapUnlockLabelPortCheck passed: original Forest map and English, Spanish and Latin American Spanish labels");
    }
}
