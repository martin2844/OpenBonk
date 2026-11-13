using System;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class PickupOrbColorsPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("PickupOrbColorsPortCheck: " + message); }
    static bool Equal(Color a, Color b) { return Mathf.Abs(a.r-b.r) <= 1f/255f && Mathf.Abs(a.g-b.g) <= 1f/255f && Mathf.Abs(a.b-b.b) <= 1f/255f && Mathf.Abs(a.a-b.a) <= 1f/255f; }
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PickupOrb.prefab");
        Require(prefab != null, "original orb prefab");
        var instance = UnityEngine.Object.Instantiate(prefab); var orb = instance.GetComponent<PickupOrb>();
        Require(orb != null && orb.trail != null && orb.particleSystems != null && orb.particleSystems.Length > 0, "original particle and trail references");
        try
        {
            orb.rb = null; // Isolate appearance from physics in this editor check.
            foreach (EPickup type in new[] { EPickup.Health, EPickup.Haste, EPickup.Rage, EPickup.Nuke, EPickup.Shield, EPickup.Time, EPickup.Magnet, EPickup.Stonks })
            {
                orb.Set(type); Color expected = MyColorUtility.PickupToColor(type);
                Require(orb.ePickup == type && Equal(orb.trail.startColor, expected) && Equal(orb.trail.endColor, expected), type + " trail color expected=" + expected.ToString("F5") + " actual start=" + orb.trail.startColor.ToString("F5") + " end=" + orb.trail.endColor.ToString("F5"));
                foreach (var ps in orb.particleSystems) Require(ps != null && Equal(ps.main.startColor.color, expected), type + " particle color");
            }
            Debug.Log("PickupOrbColorsPortCheck passed: eight powerup colors on original orb particles and trail");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
