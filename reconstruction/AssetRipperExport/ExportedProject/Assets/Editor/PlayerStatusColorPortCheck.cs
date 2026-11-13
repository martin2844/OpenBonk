using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

public static class PlayerStatusColorPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void SetEffect(PlayerStatusEffects effects, EStatusEffect effect) => effects.statusEffects[effect] = new StatusEffect(effect, 30f, Array.Empty<StatModifier>());
    static void Invoke(PlayerRenderer renderer, string method, params object[] args) => typeof(PlayerRenderer).GetMethod(method, Private).Invoke(renderer, args);
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance;
        var root = new GameObject("status color test");
        var model = root.AddComponent<PlayerRenderer>();
        var player = root.AddComponent<MyPlayer>();
        var effects = new PlayerStatusEffects();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        inventory.statusEffects = effects; player.inventory = inventory; MyPlayer.Instance = player;
        var shader = Shader.Find("MK/Toon/Built-in/Standard/Simple");
        Check(shader != null, "toon shader missing");
        var first = new Material(shader); var second = new Material(shader);
        typeof(PlayerRenderer).GetField("activeMaterials", Private).SetValue(model, new[] { first, second });
        model.colorFreeze = Color.cyan; model.colorMud = Color.yellow; model.poisonColor = Color.green; model.colorNothing = Color.clear;
        SetEffect(effects, EStatusEffect.Poison); SetEffect(effects, EStatusEffect.Bleed); SetEffect(effects, EStatusEffect.Slow); SetEffect(effects, EStatusEffect.Freeze);
        Invoke(model, "OnStatusEffectAdded", EStatusEffect.Freeze, true);
        Check(first.GetColor("_IridescenceColor") == Color.cyan && second.GetColor("_IridescenceColor") == Color.cyan, "freeze priority and all materials");
        Check(first.IsKeywordEnabled("_MK_IRIDESCENCE_DEFAULT"), "effect variant enabled");
        Check(Mathf.Abs(first.GetFloat("_IridescenceSize") - .18f) < .0001f, "effect size");
        effects.statusEffects.Remove(EStatusEffect.Freeze); Invoke(model, "OnStatusEffectRemoved", EStatusEffect.Freeze);
        Check(first.GetColor("_IridescenceColor") == Color.yellow, "slow priority");
        effects.statusEffects.Remove(EStatusEffect.Slow); Invoke(model, "OnStatusEffectRemoved", EStatusEffect.Slow);
        Check(first.GetColor("_IridescenceColor") == Color.red, "bleed priority");
        effects.statusEffects.Remove(EStatusEffect.Bleed); Invoke(model, "OnStatusEffectRemoved", EStatusEffect.Bleed);
        Check(first.GetColor("_IridescenceColor") == Color.green, "poison color");
        effects.statusEffects.Remove(EStatusEffect.Poison); SetEffect(effects, EStatusEffect.BossPoison); Invoke(model, "OnStatusEffectAdded", EStatusEffect.BossPoison, true);
        Check(first.GetColor("_IridescenceColor") == Color.green, "boss poison color");
        effects.statusEffects.Clear(); Invoke(model, "OnStatusEffectRemoved", EStatusEffect.BossPoison);
        Check(first.GetColor("_IridescenceColor") == Color.clear, "cleared color");
        Check(!first.IsKeywordEnabled("_MK_IRIDESCENCE_DEFAULT"), "effect variant disabled");
        effects.OnDestroy(); MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second);
        Debug.Log("PlayerStatusColorPortCheck passed: event refresh, status priority, color and size across two materials");
    }
}
