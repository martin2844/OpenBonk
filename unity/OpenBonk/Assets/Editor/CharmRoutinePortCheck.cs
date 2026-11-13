using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Pool;

public static class CharmRoutinePortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("charm fixture"); root.SetActive(false);
        var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
        var movement = root.AddComponent<PlayerMovement>(); PlayerMovement.Instance = movement;
        var playerBody = root.AddComponent<Rigidbody>();
        typeof(PlayerMovement).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(movement, playerBody);
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        var effects = new List<GameObject>();
        pool.charmPool = new ObjectPool<GameObject>(() => { var effect = new GameObject("effect"); effects.Add(effect); return effect; });
        var enemy = root.AddComponent<Enemy>(); enemy.hp = 100f;
        enemy.enemyMovement = root.AddComponent<EnemyMovementRb>();
        typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 10f });
        var pending = enemy.debuffsToAdd;
        var item = new ItemWeebHeadset(null); item.AddAmount();
        var hits = typeof(ItemWeebHeadset).GetField("numProcsThisTick", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            Check(item.HasOnHitEffectProc() && !item.HasPreAttackProc(), "proc flags");
            var dc = new DamageContainer(10000f, "fixture") { enemy = enemy };
            hits.SetValue(item, 100); item.ProcOnHitEffects(dc);
            Check(pending.Count == 0 && effects.Count == 0, "per-tick cap");
            item.Tick(); item.ProcOnHitEffects(dc);
            Check(enemy.enemyMovement.state == EnemyMovementRb.State.Charmed, "charmed state");
            Check(pending.TryGetValue(EDebuff.Charm, out var queued) && Mathf.Abs(queued.duration - 6.5f) < .001f, "queued duration");
            Check(effects.Count == 1 && Vector3.Distance(effects[0].transform.position, root.transform.position + Vector3.up * 6f) < .001f, "effect position");
            enemy.ReleaseCharm();
            Check(enemy.enemyMovement.state == EnemyMovementRb.State.Normal && enemy.target == playerBody, "release restores movement and player target");
            Debug.Log("CharmRoutinePortCheck passed: proc cap, duration, state, effect and release");
        }
        finally
        {
            foreach (var effect in effects) UnityEngine.Object.DestroyImmediate(effect);
            UnityEngine.Object.DestroyImmediate(root);
            GameManager.Instance = null; PlayerMovement.Instance = null; PoolManager.Instance = null;
        }
    }
}
