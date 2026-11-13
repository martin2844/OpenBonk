using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
public static class LocaleInventoryCheck
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        int i = 0;
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            Debug.Log($"LOCALE {i++}: code={locale.Identifier.Code}, name={locale.LocaleName}, path={AssetDatabase.GetAssetPath(locale)}");
    }
}
