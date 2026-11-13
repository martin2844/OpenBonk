using System;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using System.Reflection;
using Assets.Scripts.UI.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class LanguageSettingsPortCheck
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count < 2) throw new Exception("Expected at least two original locales");
        var original = LocalizationSettings.SelectedLocale;
        var go = new GameObject("language settings check");
        try
        {
            var current = go.AddComponent<CurrentSettings>();
            var settings = new CFGameSettings();
            int selected = original == locales[0] ? 1 : 0;
			LocalizationSettings.SelectedLocale = locales[selected];
			var initialize = typeof(CurrentSettings).GetMethod("InitializeFirstLanguage", BindingFlags.Static | BindingFlags.NonPublic);
			var fresh = new ConfigSaveFile();
			initialize.Invoke(null, new object[] { fresh });
			if (!fresh.hasSelectedLanguage || fresh.cfGameSettings.language != selected) throw new Exception("First launch did not preserve the selected system locale");
			fresh.cfGameSettings.language = 0;
			initialize.Invoke(null, new object[] { fresh });
			if (fresh.cfGameSettings.language != 0) throw new Exception("Existing selection was overwritten");
            int events = 0;
            Action<string, object, object> changed = (name, before, after) => { if (name == "language" && (int)after == selected) events++; };
            CurrentSettings.A_SettingUpdated += changed;
            try
            {
                current.BetterUpdateCfSettings("language", selected, settings);
                if (settings.language != selected || LocalizationSettings.SelectedLocale != locales[selected] || events != 1)
                    throw new Exception("Changing language did not update config, locale and event");
                current.BetterUpdateCfSettings("language", -1, settings);
                if (LocalizationSettings.SelectedLocale != locales[selected]) throw new Exception("Invalid locale index changed locale");
                if (string.IsNullOrEmpty(LocalizationUtility.GetLanguageName(locales[selected]))) throw new Exception("Locale has no display name");
            }
            finally { CurrentSettings.A_SettingUpdated -= changed; }
            Debug.Log("LanguageSettingsPortCheck passed");
        }
        finally
        {
            LocalizationSettings.SelectedLocale = original;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
