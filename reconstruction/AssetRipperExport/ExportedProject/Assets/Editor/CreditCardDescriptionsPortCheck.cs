using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class CreditCardDescriptionsPortCheck
{
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("CreditCardDescriptionsPortCheck: " + reason); }
    public static void Run()
    {
        var green = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/CreditCardGreen.asset");
        var red = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/CreditCardRed.asset");
        var previous = LocalizationSettings.SelectedLocale;
        try
        {
            Require(green != null && red != null && green.inItemPool && red.inItemPool, "original items and pool flags");
            foreach (string localeName in new[] { "English (en)", "Spanish (es)" })
            {
                var locale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/" + localeName + ".asset");
                Require(locale != null, "original locale " + localeName); LocalizationSettings.SelectedLocale = locale;
                string greenText = green.GetDescription(), redText = red.GetDescription();
                Require(!string.IsNullOrEmpty(greenText) && !greenText.Contains("{") && greenText.Contains("+2%") && greenText.Contains("+10%"), localeName + " green: " + greenText);
                Require(!string.IsNullOrEmpty(redText) && !redText.Contains("{") && redText.Contains("+2.5%"), localeName + " red: " + redText);
                Debug.Log("CreditCardDescriptionsPortCheck " + localeName + ": " + greenText + " / " + redText);
            }
            Debug.Log("CreditCardDescriptionsPortCheck passed: original card assets and English/Spanish localized templates");
        }
        finally { LocalizationSettings.SelectedLocale = previous; }
    }
}
