using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class AndroidControllerBindingsCheck
{
    static void Check(bool ok, string why) { if (!ok) throw new Exception("AndroidControllerBindingsCheck: " + why); }
    public static void Run()
    {
        string[] actions = { "Jump", "Interact", "Slide", "Aim", "QuickReset", "MapOverlay", "Wallrun" };
        const string prefix = "megabonk.android.gamepad.";
        var values = new int[actions.Length]; var present = new bool[actions.Length];
        for (int i = 0; i < actions.Length; i++) { present[i] = PlayerPrefs.HasKey(prefix + actions[i]); values[i] = PlayerPrefs.GetInt(prefix + actions[i], -1); }
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var oldLocale = LocalizationSettings.SelectedLocale;
        var query = typeof(AndroidNativeInput).GetMethod("IsButtonPressed", flags);
        var analog = typeof(AndroidNativeInput).GetMethod("BoundAnalog", flags);
        bool Press(string action, KeyCode active) { Func<KeyCode, bool> isKey = k => k == active; return (bool)query.Invoke(null, new object[] { action, isKey, isKey }); }
        float Analog(string action, float left, float right) => (float)analog.Invoke(null, new object[] { action, left, right });
        try
        {
            foreach (string localePath in new[] { "Assets/Localization/Locales/Spanish (es).asset", "Assets/Localization/Locales/Spanish_LatAm (es-419).asset" }) {
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(localePath); Check(locale != null, "Spanish locale asset " + localePath); LocalizationSettings.SelectedLocale = locale;
                Check(AndroidControllerBindings.ButtonName(-2) == "Sin asignar" && AndroidControlText.PressButton == "Pulsa un botón" && AndroidControlText.Fixed == "Fijo" && AndroidControlText.SwapMenuButtons == "Intercambiar A/B en menús" && AndroidControllerBindings.ButtonName(8) == "Botón 8", "Spanish controller labels");
            }
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            Check(AndroidControllerBindings.ButtonName(-2) == "Unbound" && AndroidControlText.PressButton == "Press a button", "English controller labels");
            AndroidControllerBindings.Reset();
            Check(!MyInputManager.IsActionBound("Wallrun") && !Press("Wallrun", KeyCode.JoystickButton8), "unbound wallrun remains automatic");
            Check(AndroidControllerBindings.Set("Wallrun", 8) && MyInputManager.IsActionBound("Wallrun") && Press("Wallrun", KeyCode.JoystickButton8) && !Press("Wallrun", KeyCode.JoystickButton9), "wallrun binding controls held input");
            Check(AndroidControllerBindings.Set("Wallrun", 6) && Analog("Wallrun", .8f, .3f) == .8f, "wallrun trigger routing");
            AndroidControllerBindings.Reset();
            Check(Press("Jump", KeyCode.JoystickButton0) && Press("Jump", KeyCode.JoystickButton5), "default jump buttons");
            Check(Press("Interact", KeyCode.JoystickButton2) && Press("UISubmit", KeyCode.JoystickButton2), "default interact and submit");
            Check(Analog("Aim", .8f, .3f) == .8f && Analog("MapOverlay", .8f, .3f) == .3f, "default trigger axes");
            Check(AndroidControllerBindings.Set("Jump", 3), "save custom jump");
            Check(Press("Jump", KeyCode.JoystickButton3) && !Press("Jump", KeyCode.JoystickButton0) && Press("Jump", KeyCode.Space), "override retains keyboard");
            Check(Press("Aim", KeyCode.JoystickButton0) && !Press("Aim", KeyCode.JoystickButton3), "conflicting action swaps to previous button");
            Check(Press("Jump Bhop", KeyCode.JoystickButton3), "jump alias");
            Check(AndroidControllerBindings.Set("Aim", 7), "save trigger override");
            Check(Analog("Aim", .8f, .3f) == .3f && !Press("Aim", KeyCode.JoystickButton3), "aim uses only rebound controller trigger");
            Check(AndroidControllerBindings.Set("Slide", 6) && Analog("Slide", .8f, .3f) == .8f, "trigger works for another action");
            Check(!AndroidControllerBindings.Set("UISubmit", 0) && !AndroidControllerBindings.Set("Jump", 20), "invalid mappings rejected");
            Check(Press("UISubmit", KeyCode.JoystickButton0), "menu submit unchanged");
            Check(PlayerPrefs.GetInt(prefix + "Jump") == 3, "override persisted");
            typeof(AndroidControllerBindings).GetField("loaded", flags).SetValue(null, false);
            ((System.Collections.IDictionary)typeof(AndroidControllerBindings).GetField("Overrides", flags).GetValue(null)).Clear();
            Check(AndroidControllerBindings.TryGet("Jump", out int reloaded) && reloaded == 3, "reload persisted override");
            AndroidControllerBindings.Reset(); Check(!AndroidControllerBindings.TryGet("Jump", out _) && !PlayerPrefs.HasKey(prefix + "Jump"), "reset removes overrides");
            Check(AndroidControllerBindings.Set("QuickReset", 2) && AndroidControllerBindings.TryGet("Interact", out int displaced) && displaced == -2 && !Press("Interact", KeyCode.JoystickButton2), "unbound action displaces conflicting default");
            Debug.Log("AndroidControllerBindingsCheck passed: defaults, overrides, keyboard/menu separation, trigger routing, persistence and reset");
        }
        finally
        {
            LocalizationSettings.SelectedLocale = oldLocale;
            for (int i = 0; i < actions.Length; i++) { if (present[i]) PlayerPrefs.SetInt(prefix + actions[i], values[i]); else PlayerPrefs.DeleteKey(prefix + actions[i]); }
            PlayerPrefs.Save();
            ((System.Collections.IDictionary)typeof(AndroidControllerBindings).GetField("Overrides", flags).GetValue(null)).Clear();
            typeof(AndroidControllerBindings).GetField("loaded", flags).SetValue(null, false);
        }
    }
}
