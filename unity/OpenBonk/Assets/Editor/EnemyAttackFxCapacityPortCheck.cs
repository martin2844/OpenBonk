using System;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;

public static class EnemyAttackFxCapacityPortCheck
{
    public static void Run()
    {
        var managerObject = new GameObject("pool manager");
        var manager = managerObject.AddComponent<PoolManager>();
        var attackObject = new GameObject("attack");
        attackObject.SetActive(false);
        var attack = attackObject.AddComponent<EnemySpecialAttackPrefabSingle>();
        var effectPrefab = new GameObject("effect template");
        effectPrefab.SetActive(false);
        attack.attackEffectPrefab = effectPrefab;
        var active = new List<GameObject>();
        var activeAttacks = new List<GameObject>();
        try
        {
            for (int i = 0; i < 256; i++)
            {
                var effect = manager.GetEnemyAttackFx(attack);
                if (effect == null || !effect.activeSelf) throw new Exception("Missing active effect at index " + i);
                active.Add(effect);
            }
            var pool = manager.enemyAttacksFxPools[attack.eAttack];
            if (pool.CountAll != 256 || pool.CountActive != 256) throw new Exception("Unexpected pool counts");
            var released = active[127];
            pool.Release(released);
            if (released.activeSelf || pool.CountInactive != 1) throw new Exception("Effect was not released");
            if (manager.GetEnemyAttackFx(attack) != released || !released.activeSelf || pool.CountAll != 256)
                throw new Exception("Released effect was not reused");
            var config = new EnemySpecialAttack { attackName = "capacity check", attackPrefab = effectPrefab };
            for (int i = 0; i < 1024; i++)
            {
                var instance = manager.GetEnemyAttack(config);
                if (instance == null || !instance.activeSelf) throw new Exception("Missing active attack at index " + i);
                activeAttacks.Add(instance);
            }
            var attackPool = manager.enemyAttacksPools[config.attackName];
            if (attackPool.CountAll != 1024 || attackPool.CountActive != 1024) throw new Exception("Unexpected attack pool counts");
            var releasedAttack = activeAttacks[511];
            attackPool.Release(releasedAttack);
            if (releasedAttack.activeSelf || manager.GetEnemyAttack(config) != releasedAttack || !releasedAttack.activeSelf)
                throw new Exception("Released attack was not reused");
            Debug.Log("EnemyAttackFxCapacityPortCheck passed: 256 effects, 1024 attacks and reuse");
        }
        finally
        {
            foreach (var effect in active) if (effect != null) UnityEngine.Object.DestroyImmediate(effect);
            foreach (var instance in activeAttacks) if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(effectPrefab);
            UnityEngine.Object.DestroyImmediate(attackObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
