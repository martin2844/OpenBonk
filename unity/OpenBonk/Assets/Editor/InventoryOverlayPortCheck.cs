using System;
using System.Reflection;
using Assets.Scripts.UI.InGame.Levelup;
using UnityEngine;

public static class InventoryOverlayPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    public static void Run()
    {
        var root = new GameObject("overlay fixture"); root.SetActive(false);
        var first = new GameObject("overlay one"); var second = new GameObject("overlay two");
        first.SetActive(false); second.SetActive(false);
        var ui = root.AddComponent<UiManager>(); var encounters = root.AddComponent<EncounterWindows>();
        UiManager.Instance = ui; ui.encounterWindows = encounters;
        var overlay = root.AddComponent<InventoryOverlay>(); overlay.windows = new[] { first, second }; overlay.inventoryUi = root.AddComponent<UpgradeInventoryUI>();
        try
        {
            Call(overlay, "Awake");
            EncounterWindows.A_WindowOpened?.Invoke();
            Check(first.activeSelf && second.activeSelf, "reward opened");
            Set(encounters, "<encounterInProgress>k__BackingField", true); Call(overlay, "Update");
            Check(first.activeSelf && second.activeSelf, "active encounter keeps inventory visible");
            Set(encounters, "<encounterInProgress>k__BackingField", false);
            Set(encounters, "closedEncounterThisFrame", true); Call(overlay, "Update");
            Check(first.activeSelf && second.activeSelf, "closing frame stays visible");
            Set(encounters, "closedEncounterThisFrame", false); Call(overlay, "Update");
            Check(!first.activeSelf && !second.activeSelf, "fallback hides stale inventory");
            EncounterWindows.A_WindowOpened?.Invoke(); EncounterWindows.A_WindowClosed?.Invoke();
            Check(!first.activeSelf && !second.activeSelf, "closed event hides inventory");
            Call(overlay, "OnDestroy"); EncounterWindows.A_WindowOpened?.Invoke();
            Check(!first.activeSelf && !second.activeSelf, "destroy unsubscribes events");
            Debug.Log("InventoryOverlayPortCheck passed: open, close, encounter fallback and cleanup");
        }
        finally
        {
            Call(overlay, "OnDestroy"); UiManager.Instance = null;
            UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
