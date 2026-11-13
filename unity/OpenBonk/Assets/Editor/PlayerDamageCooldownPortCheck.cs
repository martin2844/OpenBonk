using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEditor;
using UnityEngine;

public static class PlayerDamageCooldownPortCheck
{
    public static void Run()
    {
        var root = new GameObject("cooldown check"); root.SetActive(false);
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>();
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.DamageCooldownMultiplier] = 2f };
            player.inventory.playerStats = stats;
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var get = typeof(PlayerHealth).GetMethod("GetDamageCooldown", flags);
            var check = typeof(PlayerHealth).GetMethod("CheckDamageCooldown", flags);
            var cooldown = typeof(PlayerHealth).GetField("damageCooldown", flags);
            if (Mathf.Abs((float)get.Invoke(health, null) - .3f) > .0001f) throw new Exception("scaled duration");
            check.Invoke(health, new object[] { new DamageContainer(0f, "Poison") });
            if ((float)cooldown.GetValue(health) != 0f) throw new Exception("non-enemy source changed cooldown");
            check.Invoke(health, new object[] { new DamageContainer(0f, "Enemy") });
            if (Mathf.Abs((float)cooldown.GetValue(health) - .3f) > .0001f || !health.DamageCooldown()) throw new Exception("enemy source cooldown");
            stats.stats[EStat.DamageCooldownMultiplier] = .5f;
            check.Invoke(health, new object[] { new DamageContainer(0f, "Enemy") });
            if (Mathf.Abs((float)cooldown.GetValue(health) - .075f) > .0001f || health.DamageCooldown()) throw new Exception("low cooldown threshold");
            // Damage callbacks can change eligibility after the public entry
            // check; the original reports that second rejection as stopped.
            health.hp = 100;
            cooldown.SetValue(health, 0f);
            var invincible = typeof(MyPlayer).GetField("isInvincible", flags);
            var damage = typeof(PlayerHealth).GetMethod("Damage", flags);
            int stopped = 0;
            Action onCalled = () => invincible.SetValue(player, true);
            Action onStopped = () => stopped++;
            PlayerHealth.A_DamagePlayerCalled += onCalled;
            PlayerHealth.A_StoppedDamage += onStopped;
            try
            {
                damage.Invoke(health, new object[] { new DamageContainer(0f, "test") { damage = 20f }, false });
                if (health.hp != 100 || stopped != 1) throw new Exception("damage callback rejection must report one stopped hit");
            }
            finally
            {
                PlayerHealth.A_DamagePlayerCalled -= onCalled;
                PlayerHealth.A_StoppedDamage -= onStopped;
                invincible.SetValue(player, false);
            }
            player.inventory.statusEffects = new PlayerStatusEffects();
            stats.stats[EStat.Armor] = 0f;
            stats.stats[EStat.DamageReductionMultiplier] = 0f;
            LevelupScreen.A_LevelUpClose?.Invoke();
            if (!player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Invulnerability)) throw new Exception("level-up close must grant temporary invulnerability");
            int calls = 0;
            Action onExternal = () => { calls++; invincible.SetValue(player, true); };
            PlayerHealth.A_DamagePlayerCalled += onExternal;
            try
            {
                health.DamagePlayerExternal(20f, 0f, Vector3.zero, true, "test", DcFlags.BypassAll);
                if (calls != 0 || health.hp != 100) throw new Exception("invulnerability must reject external damage before callbacks");
                player.inventory.statusEffects.RemoveAllStatusEffects();
                health.DamagePlayerExternal(20f, 0f, Vector3.zero, true, "test", DcFlags.BypassAll);
                if (calls != 1) throw new Exception("external damage must resume after invulnerability ends");
            }
            finally
            {
                PlayerHealth.A_DamagePlayerCalled -= onExternal;
                invincible.SetValue(player, false);
                player.inventory.statusEffects.OnDestroy();
            }
            Debug.Log("PlayerDamageCooldownPortCheck passed: helper, scaling, damage-source filter and active threshold");
            UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = null; MyPlayer.Instance = null;
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
