using System;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class SettingsEnumPortCheck
{
    sealed class TestSettings : CFSettings { public int camera_shake = 1; public List<SettingHeader> GetHeaders() => new List<SettingHeader>(); }
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = ScriptableObject.CreateInstance<StringTable>(); table.SharedData = ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
            table.AddEntry("On", "Enabled"); table.AddEntry("Off", "Disabled"); return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool value, string why) { if (!value) throw new Exception("SettingsEnumPortCheck: " + why); }
    public static void Run()
    {
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.Instance = settings; LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        Require(string.Join(",", ConfigSettingsUtility.GetSettingValues("camera_shake")) == "Off,On", "default boolean options");
        Require(string.Join(",", ConfigSettingsUtility.GetSettingValues("enemy_targeting_mode")) == "SmartAim,ClosestEnemy", "targeting options");
        Require(string.Join(",", ConfigSettingsUtility.GetSettingValues("anti_aliasing")) == "Off,x2,x4,x8", "antialiasing options");
        Require(string.Join(",", ConfigSettingsUtility.GetSettingValues("hp_bar_color")) == "Red,White,Yellow,Blue,Magenta", "health color options");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnumSetting.prefab"); Require(prefab != null, "original enum prefab");
        var obj = UnityEngine.Object.Instantiate(prefab); var control = obj.GetComponent<EnumSetting>(); Require(control && control.valueText && control.settingName, "prefab references");
        var config = new TestSettings(); int saves = 0;
        Action<string,object,CFSettings> save = (name, value, target) => { Require(name == "camera_shake" && target == config, "callback identity"); config.camera_shake = (int)value; saves++; };
        try
        {
            control.SetSetting(save, "camera_shake", config.camera_shake, null, config);
            Require(control.valueText.text.StartsWith("Enabled") && control.valueText.text.Contains("CircleFilled"), "localized option and selection markers");
            control.UpdateValue(1); Require(config.camera_shake == 1 && saves == 0, "upper bound does not wrap or save");
            control.ControllerInputDir(-1, 1f); Require(config.camera_shake == 0 && saves == 1 && control.valueText.text.StartsWith("Disabled"), "controller selection and callback");
            control.UpdateValue(-1); Require(config.camera_shake == 0 && saves == 1, "lower bound does not wrap or save");
            Debug.Log("SettingsEnumPortCheck passed: original prefab, option lists, localization and bounded selection");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
}
