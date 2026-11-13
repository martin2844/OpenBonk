using System;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers;
using UnityEngine;

public static class ChallengeDamagePortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var playerObject = new GameObject("challenge test player");
        var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        player.inventory.statusEffects = new PlayerStatusEffects();
        var health = player.inventory.playerHealth; health.hp = 10;
        var pacifist = ScriptableObject.CreateInstance<ChallengeModifierNoDamageToEnemies>();
        pacifist.Init(null); Enemy.A_Damage?.Invoke(null, null);
        Check(health.hp == 0, "enemy damage must kill the pacifist player");
        pacifist.Cleanup(); health.hp = 10; Enemy.A_Damage?.Invoke(null, null);
        Check(health.hp == 10, "pacifist must unsubscribe on cleanup");
        var noDamage = ScriptableObject.CreateInstance<ChallengeModifierNoDamage>(); noDamage.Init(null);
        foreach (var effect in new[] { EStatusEffect.Shield, EStatusEffect.TimeFreeze, EStatusEffect.Invulnerability })
        {
            player.inventory.statusEffects.statusEffects[effect] = new StatusEffect(effect, 100f, Array.Empty<StatModifier>());
            PlayerHealth.A_DamagePlayerCalled?.Invoke(); noDamage.Tick();
            Check(health.hp == 10, "protected damage should not fail the one-hit challenge: " + effect);
            player.inventory.statusEffects.statusEffects.Clear();
        }
        PlayerHealth.A_DamagePlayerCalled?.Invoke(); Check(health.hp == 10, "one-hit death is deferred until tick");
        noDamage.Tick(); Check(health.hp == 0, "unprotected damage kills on tick");
        noDamage.Cleanup(); health.hp = 10;
        var fresh = ScriptableObject.CreateInstance<ChallengeModifierNoDamage>(); fresh.Init(null); fresh.Cleanup();
        PlayerHealth.A_DamagePlayerCalled?.Invoke(); fresh.Tick(); Check(health.hp == 10, "one-hit modifier must unsubscribe on cleanup");
        player.inventory.statusEffects.OnDestroy(); MyPlayer.Instance = null;
        UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(pacifist); UnityEngine.Object.DestroyImmediate(noDamage); UnityEngine.Object.DestroyImmediate(fresh);
        Debug.Log("ChallengeDamagePortCheck passed: enemy damage, immunity states, deferred death and event cleanup");
    }
}
