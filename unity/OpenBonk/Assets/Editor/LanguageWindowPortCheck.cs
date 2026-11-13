using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class LanguageWindowPortCheck
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        var expected = new Dictionary<string, string> {
            ["zh-Hans"] = "中文 (简体)", ["zh-Hant"] = "中文 (繁體)", ["cs"] = "Čeština", ["en"] = "English",
            ["fr"] = "Français", ["de"] = "Deutsch", ["it"] = "Italiano", ["ja"] = "日本語", ["ko"] = "한국어",
            ["pl"] = "Polski", ["pt"] = "Português", ["ru"] = "Русский", ["es"] = "Español",
            ["es-419"] = "Español (Latinoamérica)", ["th"] = "ไทย", ["tr"] = "Türkçe", ["uk"] = "Українська"
        };
        foreach (var pair in expected)
        {
            string actual = Assets.Scripts.UI.Localization.LocalizationUtility.GetLanguageName(pair.Key);
            if (actual != pair.Value) throw new Exception("Language name " + pair.Key + ": " + actual + " expected " + pair.Value);
        }
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        var windows = Resources.FindObjectsOfTypeAll<LanguageWindow>();
        LanguageWindow window = null;
        foreach (var item in windows) if (item.gameObject.scene.IsValid()) { window = item; break; }
        if (window == null || window.languageButtonPrefab == null || window.selectionGroup == null)
            throw new Exception("Scene language chooser is missing references");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var refresh = typeof(LanguageWindow).GetMethod("Refresh", flags);
        refresh.Invoke(window, null);
        var buttons = (List<SelectionGroupToggleSingleButton>)typeof(LanguageWindow).GetField("buttons", flags).GetValue(window);
		int count = 0;
		foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
			if (locale != null && !string.IsNullOrEmpty(locale.Identifier.Code)) count++;
        if (count < 2 || buttons.Count != count) throw new Exception("Locale buttons were not populated");
        for (int i = 0; i < count; i++)
        {
            var label = buttons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (!buttons[i].gameObject.activeSelf || label == null || string.IsNullOrEmpty(label.text)) throw new Exception("Missing visible language label " + i + " code=" + LocalizationSettings.AvailableLocales.Locales[i].Identifier.Code + " active=" + buttons[i].gameObject.activeSelf + " text=" + (label == null ? "<null>" : label.text));
            if (buttons[i].GetButton() == null) throw new Exception("Missing selectable " + i);
        }
        refresh.Invoke(window, null);
        if (buttons.Count != count) throw new Exception("Reopening duplicated language choices");
        // A locale without a code occurs in Android's loaded list. Inserting a
        // blank-code entry here tests the row-to-locale index gap without writing
        // to the project's locale assets or a user's saved config.
        var locales = LocalizationSettings.AvailableLocales.Locales;
        int gap = Math.Min(2, locales.Count);
        var invalid = UnityEngine.Localization.Locale.CreateLocale("");
        if (!string.IsNullOrEmpty(invalid.Identifier.Code)) throw new Exception("Expected a blank locale code");
        locales.Insert(gap, invalid);
        try
        {
            refresh.Invoke(window, null);
            var indices = (List<int>)typeof(LanguageWindow).GetField("localeIndices", flags).GetValue(window);
            if (indices.Count != count || buttons.Count != count) throw new Exception("Invalid locale changed row count");
            for (int row = 0; row < count; row++)
            {
                int index = indices[row];
                if (index == gap || locales[index] == null || string.IsNullOrEmpty(locales[index].Identifier.Code))
                    throw new Exception("Invalid locale included in row " + row);
                var label = buttons[row].GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (label.text != Assets.Scripts.UI.Localization.LocalizationUtility.GetLanguageName(locales[index]))
                    throw new Exception("Locale index/label mismatch at row " + row);
            }
            Debug.Log("LanguageWindowPortCheck invalid locale gap passed; rows=" + count);
        }
        finally
        {
            locales.RemoveAt(gap);
            UnityEngine.Object.DestroyImmediate(invalid);
        }
        Debug.Log("LanguageWindowPortCheck passed with " + count + " locales");
    }
}
