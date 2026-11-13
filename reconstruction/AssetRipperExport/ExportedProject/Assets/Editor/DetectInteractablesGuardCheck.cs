using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.Utility;
using UnityEngine;

public sealed class DetectInteractablesGuardTarget : BaseInteractable
{
    public int count;
    public override bool Interact() { count++; return true; }
    public override string GetInteractString() => "test";
}

public static class DetectInteractablesGuardCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("DetectInteractablesGuardCheck: " + message); }
    public static void Run()
    {
        var previousUi = UiManager.Instance; var previousPlayer = MyPlayer.Instance;
        bool previousPause = MyTime.paused;
        var root = new GameObject("interaction guard check"); root.SetActive(false);
        try
        {
            var ui = root.AddComponent<UiManager>(); var encounters = root.AddComponent<EncounterWindows>(); ui.encounterWindows = encounters; UiManager.Instance = ui;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var detector = root.AddComponent<DetectInteractables>(); var target = root.AddComponent<DetectInteractablesGuardTarget>();
            typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, target);
            var setEncounter = typeof(EncounterWindows).GetProperty("encounterInProgress").GetSetMethod(true);
            detector.TryInteract(); Require(target.count == 1, "normal interaction");
            setEncounter.Invoke(encounters, new object[] { true }); detector.TryInteract(); Require(target.count == 1, "active encounter blocks interaction");
            setEncounter.Invoke(encounters, new object[] { false }); player.isTeleporting = true; detector.TryInteract(); Require(target.count == 1, "teleport blocks interaction");
            player.isTeleporting = false; MyTime.paused = true; detector.TryInteract(); Require(target.count == 2, "detector has no separate pause check");
            UiManager.Instance = null; detector.TryInteract(); Require(target.count == 2, "missing UI is guarded");
            UiManager.Instance = ui; MyPlayer.Instance = null; detector.TryInteract(); Require(target.count == 2, "missing player is guarded");
            Debug.Log("DetectInteractablesGuardCheck passed: encounter, teleport, pause separation and missing references");
        }
        finally
        {
            UiManager.Instance = previousUi; MyPlayer.Instance = previousPlayer; MyTime.paused = previousPause;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
