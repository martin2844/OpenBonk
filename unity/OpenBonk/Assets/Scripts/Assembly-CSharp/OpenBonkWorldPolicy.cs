using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEngine;

// Optional policies for interactions with already existing world objects.
// Registry avoids a scene-wide search every frame and follows object lifetime.
public static class OpenBonkWorldPolicy
{
    private static readonly HashSet<InteractablePot> Pots = new HashSet<InteractablePot>();
    private static readonly List<InteractablePot> Nearby = new List<InteractablePot>();
    private static bool processing;
    public static bool AutoOpenPots { get; private set; }
    public static void SetAutoOpenPots(bool enabled) { AutoOpenPots = enabled; }
    public static void RegisterPot(InteractablePot pot) { if (pot != null) Pots.Add(pot); }
    public static void UnregisterPot(InteractablePot pot) { if (!object.ReferenceEquals(pot, null)) Pots.Remove(pot); }

    // Uses the normal pot interaction, preserving its own reward and broken
    // state. Snapshot first: interaction or event handlers may disable pots.
    public static int OpenPotsNear(Vector3 position)
    {
        if (!AutoOpenPots || processing) return 0;
        processing = true;
        try
        {
            Nearby.Clear();
            foreach (var pot in Pots)
                if (pot != null && pot.isActiveAndEnabled && (pot.transform.position - position).sqrMagnitude <= 36f)
                    Nearby.Add(pot);
            int opened = 0;
            foreach (var pot in Nearby)
            {
                try
                {
                    if (pot == null || !pot.isActiveAndEnabled || !pot.CanInteract()) continue;
                    if (pot.Interact())
                    {
                        opened++;
                        DetectInteractables.A_Interacted?.Invoke(pot, true);
                    }
                }
                catch (System.Exception error) { Debug.LogWarning("Auto pot interaction failed: " + error.Message); }
            }
            return opened;
        }
        finally { Nearby.Clear(); processing = false; }
    }
}
