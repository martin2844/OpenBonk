using System;
using System.Reflection;
using Assets.Scripts.Utility;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes;
using UnityEngine;
using UnityEngine.UI;

public static class HudColorsPortCheck
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("HudColorsPortCheck: " + message); }
    private static bool Near(Color a, Color b) { return Mathf.Abs(a.r-b.r)<.001f && Mathf.Abs(a.g-b.g)<.001f && Mathf.Abs(a.b-b.b)<.001f && Mathf.Abs(a.a-b.a)<.001f; }
    public static void Run()
    {
        var root = new GameObject("HUD colors check");
        var bar = new GameObject("health", typeof(RectTransform), typeof(RawImage));
        var mat = new Material(Shader.Find("Kaima/Depth/ForceField"));
        try
        {
            var settings = root.AddComponent<CurrentSettings>(); settings.warningMaterial = mat;
            var hud = root.AddComponent<HealthUI>(); hud.healthBar = bar.transform;
            var apply = typeof(HealthUI).GetMethod("OnSettingUpdated", BindingFlags.Instance | BindingFlags.NonPublic);
            var game = new CFGameSettings(); var visuals = new CFVisualsSettings();
            var health = new[] { new Color(1,0,0,.75f), new Color(1,1,1,.75f), new Color(1,235f/255f,4f/255f,.75f), new Color(0,0,1,.75f), new Color(1,0,1,.75f) };
            string[] warning = { "#FF0C00", "#ffffff", "#fff82b", "#4275f5", "#d91cff" };
            for (int i=0;i<health.Length;i++)
            {
                apply.Invoke(hud, new object[] { "hp_bar_color", 0, i });
                Check(Near(bar.GetComponent<RawImage>().color, health[i]), "health color " + i);
                settings.BetterUpdateCfSettings("warning_color", i, visuals);
                ColorUtility.TryParseHtmlString(warning[i], out var expected); expected.a = .5f;
                Check(Near(mat.GetColor("_MainColor"), expected), "warning color " + i);
            }
            Check(Near(MyColorUtility.GetHealthBarColor((EHpBarColor)99), Color.red), "unknown health color defaults red");
            Debug.Log("HudColorsPortCheck passed: five health and warning colors, opacity, settings dispatch and fallback");
        }
        finally { UnityEngine.Object.DestroyImmediate(mat); UnityEngine.Object.DestroyImmediate(bar); UnityEngine.Object.DestroyImmediate(root); }
    }
}
