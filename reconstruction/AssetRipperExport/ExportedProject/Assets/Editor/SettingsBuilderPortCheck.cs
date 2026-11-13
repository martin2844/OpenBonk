using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class SettingsBuilderPortCheck
{
    sealed class TestSettings : CFSettings { public int camera_shake = 1; public float fov = 85f; public List<SettingHeader> GetHeaders() => new List<SettingHeader> { new SettingHeader(0, "GAME") }; }
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = ScriptableObject.CreateInstance<StringTable>(); table.SharedData = ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
            table.AddEntry("On", "Enabled"); table.AddEntry("Off", "Disabled"); table.AddEntry("GAME", "Game"); return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool value, string why) { if (!value) throw new Exception("SettingsBuilderPortCheck: " + why); }
    public static void Run()
    {
        var loc = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.Instance = loc; LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("builder check"); root.SetActive(false); var settings = root.AddComponent<Settings>(); settings.settings = new List<BetterSetting>();
        settings.enumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnumSetting.prefab"); settings.sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SliderSetting.prefab"); settings.headerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/=HEADER=.prefab");
        var content = new GameObject("content", typeof(RectTransform)); var config = new TestSettings();
        var current = root.AddComponent<CurrentSettings>(); Action<string,object,CFSettings> save = current.BetterUpdateCfSettings;
        try
        {
            typeof(Settings).GetMethod("CreateGenericSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settings, new object[] { save, content.transform, config });
            Require(content.transform.childCount == 3 && settings.settings.Count == 2, "created original header and two controls");
            var button = content.GetComponentInChildren<EnumSetting>(true); var slider = content.GetComponentInChildren<SliderSetting>(true);
            Require(button && slider && slider.slider.minValue == 60 && slider.slider.maxValue == 140, "field types choose original prefabs and range");
            button.UpdateValue(-1); Require(config.camera_shake == 0, "enum writes original config through callback");
            slider.slider.SetValueWithoutNotify(100); slider.UpdateValueSlider(); Require(config.fov == 100, "slider writes original config through callback");
            Debug.Log("SettingsBuilderPortCheck passed: original prefabs built, bound, and updated config");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(content); }
    }
}
