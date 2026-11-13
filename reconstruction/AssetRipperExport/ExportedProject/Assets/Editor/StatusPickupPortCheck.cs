using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class StatusPickupPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Call(object o, string n) => o.GetType().GetMethod(n, Private).Invoke(o, null);
    static float TimeLeft(StatusEffectPickup o) => (float)typeof(StatusEffectPickup).GetField("timeLeft", Private).GetValue(o);
    static void Require(bool v, string text) { if (!v) throw new Exception("StatusPickupPortCheck: " + text); }
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldMovement = PlayerMovement.Instance; var oldEvents = Pickup.A_PickupTriggered;
        float oldTime = MyTime.time, oldDelta = MyTime.deltaTime;
        var root = new GameObject("status pickup test"); root.SetActive(false);
        try
        {
            Pickup.A_PickupTriggered = null; var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.PowerupBoostMultiplier] = 1f }; player.inventory.playerStats = stats;
            var status = (PlayerStatusEffects)FormatterServices.GetUninitializedObject(typeof(PlayerStatusEffects)); status.statusEffects = new Dictionary<EStatusEffect,StatusEffect>(); player.inventory.statusEffects = status;
            var movement = root.AddComponent<PlayerMovement>(); PlayerMovement.Instance = movement; var rb = root.AddComponent<Rigidbody>(); typeof(PlayerMovement).GetField("rb", Private).SetValue(movement, rb);
            var pickup = root.AddComponent<Pickup>(); MyTime.time = 100f; MyTime.deltaTime = 0f;
            foreach (var spec in new[] { ("Haste", EPickup.Haste, EStatusEffect.Haste, 20f), ("Rage", EPickup.Rage, EStatusEffect.Rage, 15f), ("Shield", EPickup.Shield, EStatusEffect.Shield, 15f) })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + spec.Item1 + "PickupEffect.prefab"); Require(prefab != null, spec.Item1 + " prefab");
                var instance = UnityEngine.Object.Instantiate(prefab); var effect = instance.GetComponent<StatusEffectPickup>(); Require(effect != null && effect.ePickup == spec.Item2 && effect.statusEffect == spec.Item3, spec.Item1 + " original setup");
                status.statusEffects[spec.Item3] = new StatusEffect(spec.Item3, 120f, Array.Empty<StatModifier>());
                var hasExisting = typeof(StatusEffectPickup).GetMethod("HasStatusEffect", Private);
                Require(!(bool)hasExisting.Invoke(effect, null), spec.Item1 + " same-frame pickup permits initial effect");
                MyTime.time = 100.1f;
                Require((bool)hasExisting.Invoke(effect, null), spec.Item1 + " later pickup detects existing effect");
                MyTime.time = 100f;
                effect.Set(); Require(effect.transform.parent == player.transform && Mathf.Abs(TimeLeft(effect) - spec.Item4) < .001f, spec.Item1 + " follows player and duration");
                MyTime.deltaTime = 2f; Call(effect, "Update"); Require(Mathf.Abs(TimeLeft(effect) - (spec.Item4 - 2f)) < .001f, spec.Item1 + " countdown");
                pickup.ePickup = spec.Item2; Pickup.A_PickupTriggered(pickup); Require(Mathf.Abs(TimeLeft(effect) - spec.Item4) < .001f, spec.Item1 + " matching pickup refreshes duration");
	                var sparks = effect.sparksEffect; effect.sparksEffect = null;
	                Call(effect, "OnDestroy"); Require(Pickup.A_PickupTriggered == null, spec.Item1 + " unsubscribes");
	                if (sparks != null) UnityEngine.Object.DestroyImmediate(sparks);
                UnityEngine.Object.DestroyImmediate(instance); status.statusEffects.Clear(); MyTime.deltaTime = 0f;
            }
            Debug.Log("StatusPickupPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; PlayerMovement.Instance = oldMovement; Pickup.A_PickupTriggered = oldEvents; MyTime.time = oldTime; MyTime.deltaTime = oldDelta; }
    }
}
