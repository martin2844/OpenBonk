using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class PoisonContactPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string why) { if (!ok) throw new Exception("PoisonContactPortCheck: " + why); }
    public static void Run()
    {
        var root = new GameObject("poison contact fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.EnemyDamageMultiplier] = 1f, [EStat.EnemyScalingMultiplier] = 1f, [EStat.Difficulty] = 0f, [EStat.Armor] = 0f, [EStat.DamageReductionMultiplier] = 0f, [EStat.Thorns] = 0f, [EStat.DamageCooldownMultiplier] = 0f };
        player.inventory.playerStats = stats;
        var status = new PlayerStatusEffects(); player.inventory.statusEffects = status;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; player.inventory.playerHealth = health;
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
        var scorpion = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Scorpion_0.asset");
        var ent = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage1.asset");
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var previousConfig = MapController.runConfig; int previousIndex = MapController.index;
        var enemy = root.AddComponent<Enemy>(); var enemyData = typeof(Enemy).GetProperty("enemyData").GetSetMethod(true);
        try
        {
            Check(scorpion != null && scorpion.isPoison && ent != null && !ent.isPoison && map != null, "original enemy and map assets");
            MapController.runConfig = new RunConfig { mapData = map, mapTierIndex = 0 }; MapController.index = 0;
            MyTime.time = 10f; MyTime.difficultyTimer = 0f; MyTime.finalSwarmTimer = 0f;
            enemyData.Invoke(enemy, new object[] { ent }); health.DamagePlayer(enemy, Vector3.up, DcFlags.BypassEvade);
            Check(!status.HasStatusEffect(EStatusEffect.Poison), "non-poison Ent does not poison");
            enemyData.Invoke(enemy, new object[] { scorpion }); health.DamagePlayer(enemy, Vector3.up, DcFlags.BypassEvade);
            Check(status.HasStatusEffect(EStatusEffect.Poison) && Mathf.Abs(status.statusEffects[EStatusEffect.Poison].expirationTime - 16f) < .001f, "Scorpion applies six seconds");
            MyTime.time = 12f; health.DamagePlayer(enemy, Vector3.up, DcFlags.BypassEvade);
            Check(Mathf.Abs(status.statusEffects[EStatusEffect.Poison].expirationTime - 18f) < .001f, "later contact refreshes duration");
            status.RemoveAllStatusEffects(); typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, true); health.DamagePlayer(enemy, Vector3.up, DcFlags.BypassEvade);
            Check(!status.HasStatusEffect(EStatusEffect.Poison), "invulnerable player rejects contact before poison");
            Debug.Log("PoisonContactPortCheck passed: original Ent/Scorpion data, six-second poison, refresh and invulnerability");
        }
        finally { status.OnDestroy(); MapController.runConfig = previousConfig; MapController.index = previousIndex; MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
