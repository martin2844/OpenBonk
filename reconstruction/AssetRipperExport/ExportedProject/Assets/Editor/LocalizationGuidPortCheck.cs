using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class LocalizationGuidPortCheck
{
    public static void Run()
    {
        var expected = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        if (expected == null || LocalizationSettings.Instance != expected) throw new Exception("Project localization settings were not registered");
        string[] paths = AssetDatabase.FindAssets("t:SharedTableData", new[] { "Assets/Localization/String Tables" });
        int checkedTables = 0;
        foreach (string assetGuid in paths)
        {
            var data = AssetDatabase.LoadAssetAtPath<SharedTableData>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (data == null || data.TableCollectionNameGuid == Guid.Empty) continue;
            if (!string.Equals(assetGuid, data.TableCollectionNameGuid.ToString("N"), StringComparison.OrdinalIgnoreCase))
                throw new Exception("Localization asset GUID differs from collection GUID: " + data.name);
            checkedTables++;
        }
        if (checkedTables != 51) throw new Exception("Expected 51 recovered shared tables; found " + checkedTables);
        Locale english = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
        Locale spanish = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/Spanish (es).asset");
        TableReference table = Guid.Parse("f761d2c2e38d1d24481733b1dee2691e");
        TableEntryReference entry = 39329299406848L;
        string en = LocalizationSettings.StringDatabase.GetLocalizedString(table, entry, english);
        string es = LocalizationSettings.StringDatabase.GetLocalizedString(table, entry, spanish);
        if (en != "Credits" || es != "Créditos") throw new Exception("Scene GUID lookup failed: " + en + " / " + es);
        Debug.Log("LocalizationGuidPortCheck passed: 51 original collection GUIDs and English/Spanish scene entry lookup");
    }
}
