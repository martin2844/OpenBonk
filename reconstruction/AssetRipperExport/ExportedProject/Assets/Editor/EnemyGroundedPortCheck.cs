using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyGroundedPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string reason) { if (!value) throw new Exception("EnemyGroundedPortCheck: " + reason); }
    public static void Run()
    {
        var prior = GameManager.Instance; float oldTime = MyTime.time;
        var state = new GameObject("ground check state"); state.SetActive(false);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game;
            ground.layer = 8; ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(10, 1, 10); game.whatIsGroundAndObjects = 1 << ground.layer;
            var actor = new GameObject("grounded enemy"); actor.transform.SetParent(state.transform); actor.transform.position = Vector3.up;
            var enemy = actor.AddComponent<Enemy>(); var movement = actor.AddComponent<EnemyMovementRb>(); enemy.enemyMovement = movement; movement.enemy = enemy;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
            typeof(EnemyMovementRb).GetField("randomGroundedCheckOffset", Private).SetValue(movement, .1f);
            typeof(EnemyMovementRb).GetField("groundCheckInterval", Private).SetValue(movement, .5f);
            var check = typeof(EnemyMovementRb).GetMethod("CheckGrounded", Private);
            var item = new ItemEagleClaw(null); item.AddAmount(); var hit = new DamageContainer(1f, "check") { enemy = enemy };
            float Addition() { var modifier = new StatComponents(); item.PreAttack(hit, modifier); return modifier.additiveValue; }
            Physics.SyncTransforms(); MyTime.time = 10f; check.Invoke(movement, null);
            Check(movement.grounded && Addition() == 0f, "floor ray and no airborne bonus");
            actor.transform.position = Vector3.up * 4f; Physics.SyncTransforms(); MyTime.time = 10.5f; check.Invoke(movement, null);
            Check(movement.grounded, "cached state before next check");
            MyTime.time = 10.7f; check.Invoke(movement, null);
            Check(!movement.grounded && Mathf.Abs(Addition() - .66f) < .0001f, "airborne ray and Eagle Claw damage bonus");
            actor.transform.position = Vector3.up; ground.layer = 9; Physics.SyncTransforms(); MyTime.time = 11.4f; check.Invoke(movement, null);
            Check(!movement.grounded, "unmasked surfaces do not ground enemy");
            ground.layer = 8; Physics.SyncTransforms(); MyTime.time = 12.1f; check.Invoke(movement, null); Check(movement.grounded, "landing refreshes grounded state");
            data.isFlying = true; MyTime.time = 12.8f; check.Invoke(movement, null); Check(!movement.grounded, "flying enemies remain airborne over ground");
            Debug.Log("EnemyGroundedPortCheck passed: floor, check cadence, airborne damage bonus, layer mask, landing and flying enemy");
        }
        finally { GameManager.Instance = prior; MyTime.time = oldTime; UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(data); }
    }
}
