using System;
using System.Reflection;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop.Rank;
using UnityEngine;

public static class RankDisplayPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("RankDisplayCheck");
        var manager = root.AddComponent<IconManager>();
        var previous = IconManager.Instance;
        var frame = new Texture2D(1, 1);
        var first = new Texture2D(1, 1);
        var second = new Texture2D(1, 1);
        try
        {
            IconManager.Instance = null;
            manager.rankFrameIcon = frame;
            manager.rankIcons = new Texture[] { first, second };
            typeof(IconManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(IconManager.Instance == manager, "icon manager initialization");
            Ranks.GetRankTextures(39, out var selectedFrame, out var selectedIcon, out var rankColor, out var frameColor);
            ColorUtility.TryParseHtmlString("#E7E7E7", out var expected);
            Require(selectedFrame == frame && selectedIcon == second && frameColor == expected && rankColor == Color.Lerp(expected, Color.white, .6f), "rank 39");
            Ranks.GetRankTextures(40, out _, out selectedIcon, out _, out frameColor);
            ColorUtility.TryParseHtmlString("#31FF6C", out expected);
            Require(selectedIcon == first && frameColor == expected, "rank 40 tier boundary");
            Ranks.GetRankTextures(1000, out _, out _, out _, out frameColor);
            ColorUtility.TryParseHtmlString("#FFED28", out expected);
            Require(frameColor == expected, "rank clamp");
            var listener = root.AddComponent<KeyListener>();
            listener.justClosed = true;
            Require(listener.IsListening(), "key listener just-closed state");
            listener.justClosed = false;
            Require(!listener.IsListening(), "key listener idle state");
            Debug.Log("RankDisplayPortCheck passed: singleton, rank icons, tier colors, clamp, and key listener state");
        }
        finally
        {
            IconManager.Instance = previous;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }
}
