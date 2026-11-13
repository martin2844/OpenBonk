using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class EnemyScalingOriginalPortCheck
{
    static void Near(float actual, float expected, string message)
    {
        float tolerance = Mathf.Max(.001f, Mathf.Abs(expected) * .00001f);
        if (float.IsNaN(actual) || Mathf.Abs(actual - expected) > tolerance)
            throw new Exception(message + ": " + actual + " expected " + expected);
    }
    public static void Run()
    {
        var root = new GameObject("enemy scaling fixture"); root.SetActive(false);
        GameManager.Instance = root.AddComponent<GameManager>();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> {
            [EStat.Difficulty] = .2f, [EStat.EnemyScalingMultiplier] = 1.5f,
            [EStat.EnemyHpMultiplier] = 1.2f, [EStat.EnemyDamageMultiplier] = 1.3f,
            [EStat.EnemySpeedMultiplier] = 1.1f,
            [EStat.Armor] = .6f, [EStat.DamageReductionMultiplier] = .25f
        };
        player.inventory.playerStats = stats;
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/GoblinWeak.asset");
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var graveyard = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapGraveyard.asset");
        if (data == null || forest == null || graveyard == null) throw new Exception("original enemy or map asset missing");
        var enemy = root.AddComponent<Enemy>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        var flag = typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic);
        var config = new RunConfig { mapData = forest }; MapController.runConfig = config;
        try
        {
            var hpTier = new[] { .8f, .95f, 1.05f, 1f };
            var speedTier = new[] { .85f, .98f, 1.065f, 1f };
            var damageTier = new[] { .75f, .95f, 1.1f, 1f };
            for (int tier = 0; tier < 4; tier++)
            {
                config.mapTierIndex = tier;
                Near(config.GetEnemyHp(100), 100 * hpTier[tier], "tier HP " + tier);
                Near(config.GetEnemySpeed(100), 100 * speedTier[tier], "tier speed " + tier);
                Near(config.GetEnemyDamage(100), 100 * damageTier[tier], "tier damage " + tier);
            }
            config.mapData = graveyard; config.mapTierIndex = 0;
            Near(config.GetEnemyHp(100), 105f, "graveyard HP override");
            Near(config.GetEnemySpeed(100), 106.5f, "graveyard speed override");
            Near(config.GetEnemyDamage(100), 110f, "graveyard damage override");
            config.mapData = forest; config.mapTierIndex = 1;
            MyTime.difficultyTimer = 120f; MyTime.finalSwarmTimer = 60f;
            var hpStage = new[] { 0f, 27f * 1.09f, 145f * 1.09f };
            var dmgStage = new[] { 0f, 2f, 4f };
            var speedStage = new[] { 0f, .18f, .46f };
            for (int stage = 0; stage < 3; stage++)
            {
                MapController.index = stage;
                float hpScale = 1f + .3f + 3f + hpStage[stage] + .22f;
                float damageScale = 1f + .084f + 1f + dmgStage[stage] + .2f;
                float speedScale = 1f + .075f + 1.5f + speedStage[stage] + .1f;
                foreach (EEnemyFlag flags in new[] { EEnemyFlag.None, EEnemyFlag.Elite, EEnemyFlag.Challenge, EEnemyFlag.Elite | EEnemyFlag.Challenge, EEnemyFlag.StageBoss })
                {
                    flag.SetValue(enemy, flags);
                    float statusHp = flags == EEnemyFlag.Elite ? 10f : flags == EEnemyFlag.Challenge ? 7f : flags == (EEnemyFlag.Elite | EEnemyFlag.Challenge) ? 15f : 1f;
                    float bossStage = flags == EEnemyFlag.StageBoss && stage == 1 ? 2.5f : flags == EEnemyFlag.StageBoss && stage == 2 ? 5f : 1f;
                    float eliteDamage = (flags & EEnemyFlag.Elite) != 0 ? 1.5f : 1f;
                    Near(EnemyStats.GetHp(enemy), data.hp * .95f * hpScale * statusHp * bossStage * 1.2f, "HP stage=" + stage + " flags=" + flags);
                    Near(EnemyStats.GetDamage(enemy), data.damage * .95f * damageScale * eliteDamage * 1.3f, "damage stage=" + stage + " flags=" + flags);
                }
                Near(EnemyStats.GetSpeed(data), Mathf.Clamp(data.speed * .98f * speedScale * 1.1f, 0f, 90f), "speed stage=" + stage);
            }
            var cases = new[] {
                (DcFlags.None, 20f, 6f, 12),
                (DcFlags.BossDamage, 20f, 10.5f, 6),
                (DcFlags.FinalBossDamage, 20f, 8.25f, 9),
                (DcFlags.BossDamage | DcFlags.FinalBossDamage, 20f, 8.25f, 9),
                (DcFlags.IgnoreArmor | DcFlags.BossDamage, 20f, 15f, 0),
                (DcFlags.None, .1f, .75f, 0)
            };
            foreach (var test in cases)
            {
                var damage = DamageUtility.GetPlayerDamage(test.Item2, 7f, Vector3.forward, enemy, "fixture", test.Item1);
                Near(damage.damage, test.Item3, "incoming damage flags=" + test.Item1);
                if (damage.damageBlockedByArmor != test.Item4 || damage.enemy != enemy || damage.flags != test.Item1 || damage.knockback != 7f || damage.direction != Vector3.forward || damage.damageSource != "fixture")
                    throw new Exception("incoming damage metadata flags=" + test.Item1 + " blocked=" + damage.damageBlockedByArmor);
            }
            var reused = DamageUtility.GetPlayerDamage(20f, 0f, Vector3.zero, null, "second", DcFlags.None);
            if (reused.enemy != null || reused.direction != Vector3.zero || reused.knockback != 0f || reused.damageSource != "second") throw new Exception("reused damage container retained metadata");
            var ent2 = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage2.asset");
            if (ent2 == null || (int)ent2.enemyName != 20 || ent2.hp != 8000) throw new Exception("Forest stage 2 boss data missing or changed");
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ent2 });
            flag.SetValue(enemy, EEnemyFlag.StageBoss);
            stats.stats[EStat.Difficulty] = 0f; stats.stats[EStat.EnemyHpMultiplier] = 1f; stats.stats[EStat.EnemyScalingMultiplier] = 1f;
            config.mapTierIndex = 2; MapController.index = 1; MyTime.difficultyTimer = 120f; MyTime.finalSwarmTimer = 0f;
            float ent2hp = EnemyStats.GetHp(enemy);
            Near(ent2hp, 592200f, "controlled Forest stage 2 boss HP");
            Debug.Log($"ControlledEnt2Hp: enemy_id={(int)ent2.enemyName} base_hp={ent2.hp} tier=2 stage=1 difficulty_time=120 swarm_time=0 difficulty=0 hp_stat=1 scaling_stat=1 hp={ent2hp:R}");
            Debug.Log("PlayerIncomingDamageOriginalPortCheck passed: armor, boss flags, damage reduction, minimum and container reuse");
            Debug.Log("EnemyScalingOriginalPortCheck passed: tier/map factors, time, swarm, stage, difficulty, enemy flags and stat multipliers");
        }
        finally { MapController.runConfig = null; MapController.index = 0; GameManager.Instance = null; MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
