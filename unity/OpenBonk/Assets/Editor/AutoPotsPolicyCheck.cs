using System;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEngine;

public sealed class AutoPotsFake : InteractablePot
{
    public int Rewards;
    public bool Allowed = true;
    public bool ThrowOnce;
    public override bool CanInteract() => Allowed;
    public override bool Interact()
    {
        if (ThrowOnce) { ThrowOnce = false; throw new Exception("probe interaction failure"); }
        if (Rewards != 0) return false;
        Rewards++;
        gameObject.SetActive(false); // Tests registry mutation while dispatching.
        return true;
    }
}

public static class AutoPotsPolicyCheck
{
    private static void Require(bool ok, string message) { if (!ok) throw new Exception("Auto pots: " + message); }
    public static void Run()
    {
        var nearObject = new GameObject("near pot");
        var secondObject = new GameObject("second pot");
        var farObject = new GameObject("far pot");
        var blockedObject = new GameObject("blocked pot");
        var failingObject = new GameObject("failing pot");
        Action<BaseInteractable, bool> onInteract = null;
        try
        {
            var near = nearObject.AddComponent<AutoPotsFake>();
            var second = secondObject.AddComponent<AutoPotsFake>();
            var far = farObject.AddComponent<AutoPotsFake>();
            var blocked = blockedObject.AddComponent<AutoPotsFake>();
            var failing = failingObject.AddComponent<AutoPotsFake>();
            near.isSilver = second.isSilver = far.isSilver = blocked.isSilver = failing.isSilver = true;
            nearObject.transform.position = new Vector3(2, 0, 0);
            secondObject.transform.position = new Vector3(6, 0, 0);
            farObject.transform.position = new Vector3(7, 0, 0);
            blocked.Allowed = false;
            failing.ThrowOnce = true;
            OpenBonkWorldPolicy.RegisterPot(near); OpenBonkWorldPolicy.RegisterPot(second);
            OpenBonkWorldPolicy.RegisterPot(far); OpenBonkWorldPolicy.RegisterPot(blocked); OpenBonkWorldPolicy.RegisterPot(failing);
            int events = 0; onInteract = (p, result) => { if (result) events++; };
            DetectInteractables.A_Interacted += onInteract;
            OpenBonkWorldPolicy.SetAutoOpenPots(false);
            Require(OpenBonkWorldPolicy.OpenPotsNear(Vector3.zero) == 0 && events == 0, "disabled");
            OpenBonkWorldPolicy.SetAutoOpenPots(true);
            Require(OpenBonkWorldPolicy.OpenPotsNear(Vector3.zero) == 2, "open nearby and boundary");
            Require(near.Rewards == 1 && second.Rewards == 1 && far.Rewards == 0 && blocked.Rewards == 0 && events == 2, "one reward and event each");
            Require(OpenBonkWorldPolicy.OpenPotsNear(Vector3.zero) == 1 && events == 3 && failing.Rewards == 1, "failed pot can be retried without duplicate rewards");
            Require(OpenBonkWorldPolicy.OpenPotsNear(Vector3.zero) == 0 && events == 3, "no duplicates or blocked interaction");
            OpenBonkWorldPolicy.SetAutoOpenPots(false);
            Require(OpenBonkWorldPolicy.OpenPotsNear(new Vector3(7, 0, 0)) == 0 && far.Rewards == 0, "turning off");
            Debug.Log("AutoPotsPolicyCheck PASS: opt-in, range, interaction, events, lifecycle and no duplicate rewards");
        }
        finally
        {
            if (onInteract != null) DetectInteractables.A_Interacted -= onInteract;
            OpenBonkWorldPolicy.SetAutoOpenPots(false);
            UnityEngine.Object.DestroyImmediate(nearObject); UnityEngine.Object.DestroyImmediate(secondObject);
            UnityEngine.Object.DestroyImmediate(farObject); UnityEngine.Object.DestroyImmediate(blockedObject); UnityEngine.Object.DestroyImmediate(failingObject);
        }
    }
}
