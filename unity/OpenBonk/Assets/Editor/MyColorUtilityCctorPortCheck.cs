using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

public static class MyColorUtilityCctorPortCheck
{
    static Color Parse(string hex) { if (!ColorUtility.TryParseHtmlString(hex, out var value)) throw new Exception("bad fixture color " + hex); return value; }
    static void Check(Color actual, Color expected, string name) { if (Mathf.Abs(actual.r - expected.r) > .00001f || Mathf.Abs(actual.g - expected.g) > .00001f || Mathf.Abs(actual.b - expected.b) > .00001f || Mathf.Abs(actual.a - expected.a) > .00001f) throw new Exception(name + ": " + actual + " != " + expected); }
    public static void Run()
    {
        Check(MyColorUtility.interactOutlineColor, Parse("#ffd745"), "interact outline");
        Check(MyColorUtility.evadeColor, new Color(0, 1, 1, 1), "evade");
        Check(MyColorUtility.evadePhantomColor, Parse("#f06bff"), "phantom evade");
        var categories = new Dictionary<EStatCategory, string> {
            { EStatCategory.Offensive, "#FF4500" }, { EStatCategory.Defensive, "#00CED1" },
            { EStatCategory.Movement, "#32CD32" }, { EStatCategory.Utility, "#FFD700" },
            { EStatCategory.Difficulty, "#4008FF" }, { EStatCategory.Coolness, "#E50069" }
        };
        foreach (var pair in categories) Check(MyColorUtility.GetStatCategoryColor(pair.Key), Parse(pair.Value), pair.Key.ToString());
        bool threw = false; try { MyColorUtility.GetStatCategoryColor(EStatCategory.Null); } catch (KeyNotFoundException) { threw = true; }
        if (!threw) throw new Exception("unknown category must throw");
        var parseMethod = typeof(MyColorUtility).GetMethod("StringToColor", BindingFlags.Static | BindingFlags.NonPublic);
        Check((Color)parseMethod.Invoke(null, new object[] { "#123456" }), Parse("#123456"), "color parsing");
        ColorUtility.TryParseHtmlString("not-a-color", out var invalid); Check((Color)parseMethod.Invoke(null, new object[] { "not-a-color" }), invalid, "invalid color parsing");
        Debug.Log("MyColorUtilityCctorPortCheck passed: outlines, evade, six stat categories and fallback");
    }
}
