using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class PauseStatsLocaleCheck
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        var original = LocalizationSettings.SelectedLocale;
        var go = new GameObject("pause stats locale check");
        try
        {
            var ui = go.AddComponent<StatsUi>();
            var stats = (List<EStat>)typeof(StatsUi).GetField("statsToShow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            if (stats.Count != 31) throw new Exception("Unexpected pause stat count " + stats.Count);
            var english = new Dictionary<EStat, string>();
            foreach (string code in new[] { "en", "es", "es-419" })
            {
                var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
                if (locale == null) throw new Exception("Locale unavailable: " + code);
                LocalizationSettings.SelectedLocale = locale;
                int changed = 0;
                foreach (var stat in stats)
                {
                    string label = LocalizationUtility.GetStatName(stat);
                    if (string.IsNullOrWhiteSpace(label) || label.Contains("LOCALIZATION ERROR") || label.Contains("\uFFFD"))
                        throw new Exception(code + " missing " + stat + ": " + label);
                    if (code == "en") english.Add(stat, label);
                    else if (english[stat] != label) changed++;
                }
                if (code != "en" && changed == 0) throw new Exception(code + " did not differ from English");
                Debug.Log($"PauseStatsLocaleCheck: locale={code} entries={stats.Count} differsFromEnglish={changed}");
            }
            Debug.Log("PauseStatsLocaleCheck passed");
        }
        finally { LocalizationSettings.SelectedLocale = original; UnityEngine.Object.DestroyImmediate(go); }
    }
}
