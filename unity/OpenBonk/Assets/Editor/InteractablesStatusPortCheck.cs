using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEngine;

public static class InteractablesStatusPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("InteractablesStatusPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("status test"); root.SetActive(false);
        var a = new GameObject("pot a"); a.transform.SetParent(root.transform); var first = a.AddComponent<InteractablePot>();
        var b = new GameObject("pot b"); b.transform.SetParent(root.transform); var second = b.AddComponent<InteractablePot>();
        var plain = new GameObject("banana"); plain.transform.SetParent(root.transform); var banana = plain.AddComponent<InteractableBananarang>();
        int spawns = 0, uses = 0; Action<string> spawned = _ => spawns++; Action<string> used = _ => uses++;
        InteractablesStatus.A_InteractableSpawned += spawned; InteractablesStatus.A_InteractableUsed += used;
        try
        {
            Check(banana.GetDebugName() == string.Empty, "unoverridden base debug name is an empty string");
            InteractablesStatus.Init(); InteractablesStatus.Init(); MapGenerationController.A_PreGeneration?.Invoke();
            var start = typeof(BaseInteractable).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            start.Invoke(first, null); start.Invoke(second, null);
            var status = InteractablesStatus.interactablesByName[InteractablePot.debugName];
            Check(status.numTotal == 2 && status.numUsed == 0 && spawns == 2 && !status.IsDone(), "spawn and duplicate initialization");
            DetectInteractables.A_Interacted?.Invoke(first, false); Check(status.numUsed == 0, "failed interaction ignored");
            DetectInteractables.A_Interacted?.Invoke(first, true); Check(status.numUsed == 1 && uses == 1 && !status.IsDone(), "successful interaction counted");
            typeof(InteractablePot).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(second, null);
            Check(status.numTotal == 1 && status.IsDone(), "unused disabled object removed");
            BaseInteractable.A_DebugSpawn?.Invoke(InteractableChest.debugNameCrypt); BaseInteractable.A_DebugSpawn?.Invoke(InteractablePot.debugNameCrypt);
            GameManager.A_DungeonEnded?.Invoke(); Check(!InteractablesStatus.interactablesByName.ContainsKey(InteractableChest.debugNameCrypt) && !InteractablesStatus.interactablesByName.ContainsKey(InteractablePot.debugNameCrypt), "crypt counts cleared");
            BaseInteractable.A_DebugSpawn?.Invoke(ChargeShrine.debugName); ChargeShrine.A_Charged?.Invoke(true);
            Check(InteractablesStatus.interactablesByName[ChargeShrine.debugName].numUsed == 1, "completed charge shrine counted");
            InteractablesStatus.Cleanup(); BaseInteractable.A_DebugSpawn?.Invoke("after cleanup"); Check(!InteractablesStatus.interactablesByName.ContainsKey("after cleanup"), "cleanup unsubscribes");
            Debug.Log("InteractablesStatusPortCheck passed: spawn, interaction, disable, dungeon, shrine and cleanup");
        }
        finally { InteractablesStatus.Cleanup(); InteractablesStatus.A_InteractableSpawned -= spawned; InteractablesStatus.A_InteractableUsed -= used; UnityEngine.Object.DestroyImmediate(root); }
    }
}
