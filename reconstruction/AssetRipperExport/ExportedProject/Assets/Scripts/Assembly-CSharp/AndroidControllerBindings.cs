using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class AndroidControlText
{
    private static bool Spanish
    {
        get {
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null && !string.IsNullOrEmpty(locale.Identifier.Code) && locale.Identifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        }
    }
    public static string Unbound => Spanish ? "Sin asignar" : "Unbound";
    public static string PressButton => Spanish ? "Pulsa un botón" : "Press a button";
    public static string Fixed => Spanish ? "Fijo" : "Fixed";
    public static string SwapMenuButtons => Spanish ? "Intercambiar A/B en menús" : "Swap A/B in menus";
    public static string TouchControls => Spanish ? "Controles táctiles" : "Touch controls";
    public static string Button(int number) => (Spanish ? "Botón " : "Button ") + number;
    public static string TouchAction(string action)
    {
        if (string.Equals(action, "Jump", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "Jump Bhop", StringComparison.OrdinalIgnoreCase)) return Spanish ? "Saltar" : "Jump";
        if (string.Equals(action, "Slide", StringComparison.OrdinalIgnoreCase)) return Spanish ? "Deslizar" : "Slide";
        if (string.Equals(action, "Interact", StringComparison.OrdinalIgnoreCase)) return Spanish ? "Usar" : "Use";
        if (string.Equals(action, "Aim", StringComparison.OrdinalIgnoreCase)) return Spanish ? "Apuntar" : "Aim";
        if (string.Equals(action, "MapOverlay", StringComparison.OrdinalIgnoreCase)) return Spanish ? "Mapa" : "Map";
        return null;
    }
}

// Android does not have the original Rewired backend. Store gamepad button
// overrides separately from the game's saved action names and keyboard maps.
public static class AndroidControllerBindings
{
    private const string Prefix = "megabonk.android.gamepad.";
    private static readonly string[] Actions = { "Jump", "Interact", "Slide", "Aim", "QuickReset", "MapOverlay", "Wallrun" };
    private static readonly Dictionary<string, int> Overrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static bool loaded;
    public static bool IsListening { get; internal set; }

    private static string Canonical(string action)
    {
        if (string.Equals(action, "Jump Bhop", StringComparison.OrdinalIgnoreCase)) return "Jump";
        foreach (string known in Actions) if (string.Equals(known, action, StringComparison.OrdinalIgnoreCase)) return known;
        return null;
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        foreach (string action in Actions)
        {
            int button = PlayerPrefs.GetInt(Prefix + action, -1);
            if (button >= -2 && button <= 19 && button != -1) Overrides[action] = button;
        }
    }

    public static bool TryGet(string action, out int button)
    {
        Load();
        string key = Canonical(action);
        if (key != null && Overrides.TryGetValue(key, out button)) return true;
        button = -1;
        return false;
    }

    public static bool Set(string action, int button)
    {
        string key = Canonical(action);
        if (key == null || button < 0 || button > 19) return false;
        Load();
        int previous = Overrides.TryGetValue(key, out int current) ? current : DefaultPrimary(key);
        foreach (string other in Actions)
        {
            if (other == key || !UsesButton(other, button)) continue;
            int replacement = previous >= 0 && previous != button ? previous : -2;
            Overrides[other] = replacement;
            PlayerPrefs.SetInt(Prefix + other, replacement);
        }
        Overrides[key] = button;
        PlayerPrefs.SetInt(Prefix + key, button); PlayerPrefs.Save();
        KeyListener.A_MapChanged?.Invoke();
        return true;
    }

    private static int DefaultPrimary(string action)
    {
        switch (action) { case "Jump": return 0; case "Interact": return 2; case "Slide": return 1; case "Aim": return 6; case "MapOverlay": return 7; default: return -2; }
    }

    private static bool UsesButton(string action, int button)
    {
        if (Overrides.TryGetValue(action, out int mapped)) return mapped == button;
        switch (action)
        {
            case "Jump": return button == 0 || button == 5;
            case "Interact": return button == 2;
            case "Slide": return button == 1 || button == 4;
            case "Aim": return button == 3 || button == 6;
            case "MapOverlay": return button == 7;
            default: return false;
        }
    }

    public static void Reset()
    {
        Load(); Overrides.Clear();
        foreach (string action in Actions) PlayerPrefs.DeleteKey(Prefix + action);
        PlayerPrefs.Save();
        KeyListener.A_MapChanged?.Invoke();
    }

    public static string ButtonName(int button)
    {
        if (button < 0) return AndroidControlText.Unbound;
        switch (button)
        {
            case 0: return "A"; case 1: return "B"; case 2: return "X"; case 3: return "Y";
            case 4: return "LB"; case 5: return "RB"; case 6: return "LT"; case 7: return "RT";
            default: return AndroidControlText.Button(button);
        }
    }

    public static string DisplayForAction(string action)
    {
        if (TryGet(action, out int button)) return ButtonName(button);
        switch (Canonical(action))
        {
            case "Jump": return "A / RB"; case "Interact": return "X"; case "Slide": return "B / LB";
            case "Aim": return "LT / Y"; case "MapOverlay": return "RT"; case "QuickReset": case "Wallrun": return AndroidControlText.Unbound;
            default: return string.Empty;
        }
    }
}
