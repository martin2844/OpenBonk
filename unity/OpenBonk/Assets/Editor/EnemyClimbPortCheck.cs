using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyClimbPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string reason) { if (!value) throw new Exception("EnemyClimbPortCheck: " + reason); }
    public static void Run()
    {
        var prior = GameManager.Instance; float oldTime = MyTime.time;
        var state = new GameObject("climb state"); state.SetActive(false);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << 8; game.whatIsGroundAndObjects = (1 << 8) | (1 << 12);
            var actor = new GameObject("enemy"); actor.transform.SetParent(state.transform); actor.transform.position = Vector3.up;
            var enemy = actor.AddComponent<Enemy>(); var movement = actor.AddComponent<EnemyMovementRb>(); enemy.enemyMovement = movement; movement.enemy = enemy;
            enemy.collider = actor.AddComponent<CapsuleCollider>(); enemy.collider.radius = .5f;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
            var target = new GameObject("target"); target.transform.SetParent(state.transform); target.transform.position = new Vector3(10f, 1f, 0); var body = target.AddComponent<Rigidbody>(); body.position = new Vector3(10f, 1f, 0);
            typeof(Enemy).GetProperty("target").GetSetMethod(true).Invoke(enemy, new object[] { body });
            typeof(EnemyMovementRb).GetField("randomOffset", Private).SetValue(movement, .25f);
            var method = typeof(EnemyMovementRb).GetMethod("TryClimbWall", Private); float Step() => (float)method.Invoke(movement, null);
            wall.layer = 12; wall.transform.position = new Vector3(1.1f, .5f, 0); wall.transform.localScale = new Vector3(.2f, 2f, 2f); Physics.SyncTransforms();
            MyTime.time = 10f; Debug.Log("climb diagnostic actor=" + actor.transform.position + " target=" + body.position + " bottom=" + enemy.GetBottomPosition() + " ray=" + Physics.Raycast(enemy.GetBottomPosition() + Vector3.up * .1f, Vector3.right, 1.5f, game.whatIsGroundAndObjects)); Check(Step() == 8f, "ordinary enemy climbs nearby object");
            wall.SetActive(false); Physics.SyncTransforms(); MyTime.time = 10.1f; Check(Step() == 0f, "climbing rechecks immediately when wall vanishes");
            wall.SetActive(true); Physics.SyncTransforms(); MyTime.time = 10.2f; Check(Step() == 0f, "no-climb polling is throttled");
            wall.tag = "Ignore"; MyTime.time = 11.3f; Check(Step() == 0f, "ignore-tag obstacle");
            wall.tag = "Untagged"; MyTime.time = 12.6f; Check(Step() == 8f, "climbing restarts after interval");
            wall.SetActive(false); Physics.SyncTransforms(); MyTime.time = 12.7f; Check(Step() == 0f, "end normal climb");
            typeof(Enemy).GetField("enemyFlag", Private).SetValue(enemy, EEnemyFlag.Boss);
            wall.SetActive(true); wall.layer = 8; wall.transform.position = new Vector3(1.1f, .5f, .3f); wall.transform.localScale = new Vector3(.2f, 2f, .1f); Physics.SyncTransforms();
            MyTime.time = 14f; Check(Step() == 8f, "boss sphere catches offset ground obstacle");
            wall.layer = 12; Physics.SyncTransforms(); MyTime.time = 14.1f; Check(Step() == 0f, "boss cast excludes object-only layer");
            Debug.Log("EnemyClimbPortCheck passed: ray, sphere, throttling, Ignore tag, immediate stop and layer masks");
        }
        finally { GameManager.Instance = prior; MyTime.time = oldTime; UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(data); }
    }
}
