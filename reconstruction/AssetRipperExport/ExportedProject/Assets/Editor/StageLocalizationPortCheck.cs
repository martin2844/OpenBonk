using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class StageLocalizationPortCheck
{
    public static void Run()
    {
        var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageForest1.asset");
        var english = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
        var spanish = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/Spanish (es).asset");
        if (!stage || !english || !spanish) throw new Exception("Stage or locale asset missing");
        var previous = LocalizationSettings.SelectedLocale;
        try {
            LocalizationSettings.SelectedLocale = english;
            if (stage.GetName() != "Green Forest" || stage.GetDescription() != "Time to cook") throw new Exception("English stage strings differ from the original table");
            LocalizationSettings.SelectedLocale = spanish;
            if (stage.GetName() != "Bosque Verde" || stage.GetDescription() != "Solo buenas vibras") throw new Exception("Spanish stage strings differ from the original table");
            Debug.Log("StageLocalizationPortCheck passed: original Forest stage name and description in English and Spanish");
        } finally { LocalizationSettings.SelectedLocale = previous; }
    }
}
