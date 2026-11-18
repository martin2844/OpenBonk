using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using UnityEngine;

public static class ChestTrackerPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("ChestTrackerPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("tracker test"); root.SetActive(false);
        var a = new GameObject("chest a"); a.transform.SetParent(root.transform); var first = a.AddComponent<InteractableChest>();
        var b = new GameObject("chest b"); b.transform.SetParent(root.transform); var second = b.AddComponent<InteractableChest>();
        var type = typeof(InteractableChest);
        var onEnable = type.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        var onDisable = type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            OpenBonkItemGrantPolicy.SetEnabled(false); OpenBonkChestTracker.SetEnabled(false);
            OpenBonkOptionalUiPolicy.Apply("chest_tracker", false);
            Check(!OpenBonkChestTracker.Enabled && !OpenBonkItemGrantPolicy.Enabled, "false result enables nothing");
            OpenBonkOptionalUiPolicy.Apply("chest_tracker", true);
            Check(OpenBonkChestTracker.Enabled && !OpenBonkItemGrantPolicy.Enabled, "tracker must not enable item lab");
            OpenBonkChestTracker.SetEnabled(false);
            OpenBonkOptionalUiPolicy.Apply("item_lab", true);
            Check(!OpenBonkChestTracker.Enabled && OpenBonkItemGrantPolicy.Enabled, "item lab must not enable tracker");
            OpenBonkItemGrantPolicy.SetEnabled(false);
            OpenBonkChestTracker.Register(first.GetInstanceID());
            Check(OpenBonkChestTracker.Total == 0, "disabled ignores spawns");
            OpenBonkChestTracker.SetEnabled(true);
            onEnable.Invoke(first, null); onEnable.Invoke(first, null); onEnable.Invoke(second, null);
            Check(OpenBonkChestTracker.Total == 2 && OpenBonkChestTracker.Unopened == 2, "unique registration");
            OpenBonkChestTracker.Opened(first.GetInstanceID()); OpenBonkChestTracker.Opened(first.GetInstanceID());
            Check(OpenBonkChestTracker.Total == 2 && OpenBonkChestTracker.Unopened == 1, "repeated opens count once");
            onDisable.Invoke(first, null); onEnable.Invoke(first, null);
            Check(OpenBonkChestTracker.Total == 2 && OpenBonkChestTracker.Unopened == 1, "opened disable and reuse");
            onDisable.Invoke(second, null); onDisable.Invoke(second, null);
            Check(OpenBonkChestTracker.Total == 1 && OpenBonkChestTracker.Unopened == 0, "unopened removal once");
            onEnable.Invoke(second, null); Check(OpenBonkChestTracker.Total == 2 && OpenBonkChestTracker.Unopened == 1, "unopened re-enable");
            MapGenerationController.A_PreGeneration?.Invoke();
            Check(OpenBonkChestTracker.Total == 0 && OpenBonkChestTracker.Unopened == 0, "map reset");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; ++i) OpenBonkChestTracker.Register(i);
            for (int i = 0; i < 10000; ++i) OpenBonkChestTracker.Opened(i);
            timer.Stop();
            Check(OpenBonkChestTracker.Total == 10000 && OpenBonkChestTracker.Unopened == 0, "large lifecycle count");
            Debug.Log("Chest tracker: 10000 registrations + 10000 opens in " + timer.Elapsed.TotalMilliseconds.ToString("F2") + " ms (Editor; not a device benchmark)");
            OpenBonkChestTracker.SetEnabled(false);
            Check(OpenBonkChestTracker.Total == 0, "disable clears");
            Debug.Log("ChestTrackerPortCheck passed");
        }
        finally { OpenBonkChestTracker.SetEnabled(false); UnityEngine.Object.DestroyImmediate(root); }
    }
}
