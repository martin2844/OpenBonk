using System;
using System.Reflection;
using Inventory__Items__Pickups.Pickups;
using UnityEditor;
using UnityEngine;

public static class XpVisualsPortCheck
{
    static void Require(bool ok, string text) { if (!ok) throw new Exception("XpVisualsPortCheck: " + text); }
    static bool Close(Color a, Color b) => Mathf.Abs(a.r-b.r) < .001f && Mathf.Abs(a.g-b.g) < .001f && Mathf.Abs(a.b-b.b) < .001f && Mathf.Abs(a.a-b.a) < .001f;
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab");
        Require(prefab != null, "original XP prefab exists");
        var go = UnityEngine.Object.Instantiate(prefab); go.SetActive(false);
        try
        {
            var visuals = go.GetComponentInChildren<XpVisuals>(true);
            Require(visuals != null && visuals.ps != null && visuals.pickup != null, "original prefab references");
            var pickup = visuals.pickup;
            var awake = typeof(XpVisuals).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            var destroy = typeof(XpVisuals).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(visuals, null);
            pickup.Set(Vector3.zero, 9, 0f); Require(visuals.transform.localScale == Vector3.one, "low tier scale");
            pickup.AddValue(1); Require(Close(visuals.ps.main.startColor.color, visuals.c_mid) && Vector3.Distance(visuals.transform.localScale, Vector3.one * 1.25f) < .001f, "medium tier at ten");
            pickup.AddValue(39); Require(Close(visuals.ps.main.startColor.color, visuals.c_mid), "medium below fifty");
            pickup.AddValue(1); Require(Close(visuals.ps.main.startColor.color, visuals.c_high) && Vector3.Distance(visuals.transform.localScale, Vector3.one * 1.5f) < .001f, "high tier at fifty");
            visuals.SetEchoXp(); Require(Close(visuals.ps.main.startColor.color, visuals.c_echo), "echo color");
            pickup.Set(Vector3.zero, 1, 0f); Require(Close(visuals.ps.main.startColor.color, visuals.c_low) && visuals.transform.localScale == Vector3.one, "reused pickup resets tier");
            destroy.Invoke(visuals, null); Require(pickup.A_ValueUpdated == null, "value listener removed");
            Debug.Log("XpVisualsPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
