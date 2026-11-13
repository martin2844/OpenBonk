using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class VictorySilverLocalizationCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var root = new GameObject("victory localization check");
        var screen = root.AddComponent<VictoryScreen>();
        var textObject = new GameObject("silver", typeof(RectTransform), typeof(TextMeshProUGUI));
        screen.extraText = textObject.GetComponent<TextMeshProUGUI>();
        try
        {
            foreach (string locale in new[] { "en", "es" })
            {
                LocalizationSettings.SelectedLocale = Locale.CreateLocale(locale);
                var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_RoundOver_" + locale + ".asset");
                string expected = table.GetEntry("VICTORY_SILVER").GetLocalizedString();
                Set(screen, "silverStarted", true); Set(screen, "silverTimer", .3f); Set(screen, "maxSilver", 20);
                Invoke(screen, "Animate");
                if (!screen.extraText.text.EndsWith(expected)) throw new Exception(locale + " animated counter: " + screen.extraText.text + " expected " + expected);
                Invoke(screen, "FinishSilverCounter");
                if (!screen.extraText.text.EndsWith(expected)) throw new Exception(locale + " finished counter: " + screen.extraText.text + " expected " + expected);
                Debug.Log("VictorySilverLocalizationCheck " + locale + " label=" + expected);
            }
            Debug.Log("VictorySilverLocalizationCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(textObject); UnityEngine.Object.DestroyImmediate(root); }
    }
}
