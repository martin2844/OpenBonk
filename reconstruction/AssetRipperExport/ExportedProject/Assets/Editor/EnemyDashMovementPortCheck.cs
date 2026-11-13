using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyDashMovementPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string reason) { if (!value) throw new Exception("EnemyDashMovementPortCheck: " + reason); }
    public static void Run()
    {
        var prior = GameManager.Instance; float oldTime = MyTime.time;
        var state = new GameObject("dash state"); state.SetActive(false);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << 8;
            var actor = new GameObject("enemy"); actor.transform.SetParent(state.transform); actor.transform.position = Vector3.up;
            var enemy = actor.AddComponent<Enemy>(); var movement = actor.AddComponent<EnemyMovementRb>();
            enemy.enemyMovement = movement; movement.enemy = enemy; movement.rb = actor.AddComponent<Rigidbody>();
            enemy.collider = actor.AddComponent<CapsuleCollider>(); enemy.collider.radius = .5f;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            typeof(Enemy).GetProperty("meshHeight").GetSetMethod(true).Invoke(enemy, new object[] { 2f });
            var target = new GameObject("target"); target.transform.SetParent(state.transform); target.transform.position = new Vector3(10f, 1f, 0);
            var targetBody = target.AddComponent<Rigidbody>(); targetBody.position = target.transform.position;
            typeof(Enemy).GetProperty("target").GetSetMethod(true).Invoke(enemy, new object[] { targetBody });
            var step = typeof(EnemyMovementRb).GetMethod("Dashing", Private);
            wall.layer = 8; wall.transform.position = new Vector3(1.1f, .5f, 0); wall.transform.localScale = new Vector3(.2f, 2f, 2f);
            floor.layer = 8; floor.transform.position = new Vector3(0f, -.1f, 0f); floor.transform.localScale = new Vector3(10f, .2f, 10f);
            Physics.SyncTransforms(); MyTime.time = 10f; movement.DashStart(Vector3.right, 2f, 7f);
            step.Invoke(movement, null); Check(Vector3.Distance(movement.rb.velocity, new Vector3(7f, 15f, 0f)) < .001f, "wall gives upward velocity");
            wall.SetActive(false); Physics.SyncTransforms(); step.Invoke(movement, null);
            Check(Vector3.Distance(movement.rb.velocity, new Vector3(7f, 10f, 0f)) < .001f, "leaving wall gives upward velocity");
            movement.rb.velocity = new Vector3(0f, -3f, 0f); step.Invoke(movement, null);
            Check(Vector3.Distance(movement.rb.velocity, new Vector3(7f, -3f, 0f)) < .001f, "open ground preserves vertical velocity");
            floor.transform.position = new Vector3(0f, -3f, 0f); floor.transform.rotation = Quaternion.Euler(0f, 0f, 30f); Physics.SyncTransforms();
            movement.DashStart(Vector3.right, 2f, 7f); movement.rb.velocity = new Vector3(0f, -3f, 0f); step.Invoke(movement, null);
            float slopeX = Vector3.ProjectOnPlane(Vector3.right, floor.transform.up).normalized.x;
            Debug.Log($"dash slope diagnostic velocity={movement.rb.velocity}, expectedX={7f * slopeX}, floorUp={floor.transform.up}, wall={typeof(EnemyMovementRb).GetField("isDashingWall", Private).GetValue(movement)}");
            Check(Vector3.Distance(movement.rb.velocity, new Vector3(7f * slopeX, -3f, 0f)) < .001f, "ground normal projects dash direction");
            Debug.Log("EnemyDashMovementPortCheck passed: wall, wall exit and open ground velocity");
        }
        finally { GameManager.Instance = prior; MyTime.time = oldTime; UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(wall); UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(data); }
    }
}
