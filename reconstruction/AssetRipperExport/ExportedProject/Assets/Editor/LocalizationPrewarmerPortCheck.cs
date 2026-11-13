using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class LocalizationPrewarmerPortCheck
{
    private sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            table.SharedData.TableCollectionName = name; table.LocaleIdentifier = locale.Identifier;
            table.AddEntry("test", "Español: ñ, á, Ω");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    public static void Run()
    {
        try
        {
            var root = new GameObject("font-prewarm check");
            var component = root.AddComponent<LocalizationPrewarmer>();
            var all = new List<TMP_FontAsset>();
            TMP_FontAsset Font() { var value = ScriptableObject.CreateInstance<TMP_FontAsset>(); all.Add(value); return value; }
            component.mainFont = Font(); component.notoDefault = Font(); component.notoJp = Font(); component.notoKo = Font();
            component.notoSc = Font(); component.notoTc = Font(); component.notoTh = Font();
            component.tableNamesToPreload = new List<string>();
            var get = typeof(LocalizationPrewarmer).GetMethod("GetFontAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var pair in new[] { ("es", component.notoDefault), ("ja-JP", component.notoJp), ("ko", component.notoKo), ("zh-CN", component.notoSc), ("zh-Hans-SG", component.notoSc), ("zh-HK", component.notoTc), ("zh-Hant-TW", component.notoTc), ("th", component.notoTh) })
            {
                var locale = Locale.CreateLocale(pair.Item1);
                if ((TMP_FontAsset)get.Invoke(component, new object[] { locale }) != pair.Item2) throw new Exception("Incorrect font for " + pair.Item1);
                component.PrewarmFontAsset(pair.Item2, locale);
                if (component.mainFont.fallbackFontAssetTable.Count != 1 || component.mainFont.fallbackFontAssetTable[0] != pair.Item2) throw new Exception("Incorrect first fallback for " + pair.Item1);
                var seen = new HashSet<TMP_FontAsset>(); var current = pair.Item2;
                while (current != null && seen.Add(current)) current = current.fallbackFontAssetTable.Count == 0 ? null : current.fallbackFontAssetTable[0];
                if (current != null || seen.Count != 6) throw new Exception("Fallback chain must contain all six fonts without a cycle");
                UnityEngine.Object.DestroyImmediate(locale);
            }
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            LocalizationSettings.Instance = settings;
            LocalizationSettings.StringDatabase.TableProvider = new Provider();
            var spanish = Locale.CreateLocale("es");
            var original = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MonoBehaviour/NotoSans-Bold SDF.asset");
            if (original == null) throw new Exception("Noto font missing");
            var actualFont = UnityEngine.Object.Instantiate(original);
            component.tableNamesToPreload.Add("PrewarmTest");
            component.PrewarmFontAsset(actualFont, spanish);
            if (!actualFont.HasCharacter('ñ') || !actualFont.HasCharacter('á') || !actualFont.HasCharacter('Ω')) throw new Exception("Localized characters not present after prewarm");
            LocalizationSettings.Instance = null;
            UnityEngine.Object.DestroyImmediate(actualFont); UnityEngine.Object.DestroyImmediate(spanish); UnityEngine.Object.DestroyImmediate(settings);
            foreach (var font in all) UnityEngine.Object.DestroyImmediate(font);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("LocalizationPrewarmerPortCheck passed: locale font mapping, acyclic fallbacks and characters from a localized table");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
