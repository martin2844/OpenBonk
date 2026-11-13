using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Utility;
using UnityEngine;

public static class PlayerDamageFeedbackPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static object Field(object instance, string name) => instance.GetType().GetField(name, Private).GetValue(instance);
    static void Invoke(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, Private).Invoke(instance, args);
    public static void Run()
    {
        var root = new GameObject("damage feedback test"); var player = root.AddComponent<PlayerRenderer>(); Invoke(player, "Awake");
        var body = new GameObject("body"); body.transform.SetParent(root.transform); player.renderer = body.AddComponent<SkinnedMeshRenderer>();
        var shader = Shader.Find("Standard"); Check(shader != null, "test shader missing");
        var original = new Material(shader); var second = new Material(shader); var flash = new Material(shader); player.damageFlash = flash;
        player.renderer.sharedMaterials = new[] { original, second }; player.shieldColor = Color.cyan;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 10; health.shield = 3;
        MyTime.time = 4f; PlayerHealth.A_TakeDamage?.Invoke(health, null, true);
        Check(player.renderer.sharedMaterial == flash && (bool)Field(player, "isDamageFlash"), "damage switches to flash material");
        Check(Mathf.Abs((float)Field(player, "resetMaterialTime") - 4.15f) < .0001f, "flash expiry uses original duration");
        var outline = (Outline)Field(player, "outline"); Check(outline != null && outline.enabled && outline.OutlineColor == Color.cyan && outline.OutlineWidth == 6f && outline.OutlineMode == Outline.Mode.OutlineVisible, "shield outline state initialized");
        MyTime.time = 4.05f; PlayerHealth.A_TakeDamage?.Invoke(health, null, true); Check(Mathf.Abs((float)Field(player, "resetMaterialTime") - 4.15f) < .0001f, "repeated hit during flash does not overwrite saved materials or timer");
        player.ResetMaterial(); var restored = player.renderer.sharedMaterials;
        Check(restored.Length == 2 && restored[0] == original && restored[1] == second, "all original materials restored");
        health.shield = 0; PlayerHealth.A_Heal?.Invoke(health, 1f, false); Check(!outline.enabled, "shield outline disables after shield disappears");
        health.shield = 1; PlayerHealth.A_Heal?.Invoke(health, 1f, true); Check(outline.enabled, "shield outline re-enabled on heal");
        Invoke(player, "OnDestroy"); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(second); UnityEngine.Object.DestroyImmediate(flash);
        Debug.Log("PlayerDamageFeedbackPortCheck passed: hit event, flash duration, material restoration and shield outline state");
    }
}
