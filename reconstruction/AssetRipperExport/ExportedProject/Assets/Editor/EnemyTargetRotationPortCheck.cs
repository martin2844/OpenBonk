using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyTargetRotationPortCheck
{
    public static void Run()
    {
        var oldGame = GameManager.Instance;
        var oldPlayer = PlayerMovement.Instance;
        var oldTime = MyTime.time;
        var game = new GameObject("test game"); game.SetActive(false);
        var player = new GameObject("test player"); player.SetActive(false);
        var enemyObject = new GameObject("test enemy"); enemyObject.SetActive(false);
        var blackHole = new GameObject("black hole"); blackHole.SetActive(false);
        var attractor = new GameObject("attractor");
        EnemyData data = null;
        try
        {
            GameManager.Instance = game.AddComponent<GameManager>();
            PlayerMovement.Instance = player.AddComponent<PlayerMovement>();
            var playerBody = player.AddComponent<Rigidbody>();
            typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(PlayerMovement.Instance, playerBody);
            var enemy = enemyObject.AddComponent<Enemy>();
            var movement = enemyObject.AddComponent<EnemyMovementRb>();
            movement.enemy = enemy; movement.rb = enemyObject.AddComponent<Rigidbody>(); enemy.enemyMovement = movement;
            enemy.FindTarget();
            if (enemy.target != playerBody) throw new Exception("Enemy did not reacquire player rigidbody");
            enemy.transform.position = new Vector3(5, 3, 2);
            movement.SetDesiredRotation(new Vector3(15, 20, 2));
            var desired = (Quaternion)typeof(EnemyMovementRb).GetField("desiredRotation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(movement);
            if (Quaternion.Angle(desired, Quaternion.Euler(0, 90, 0)) > .01f) throw new Exception("Enemy desired rotation did not face target on horizontal plane");
            enemy.MyUpdate();
            data = ScriptableObject.CreateInstance<EnemyData>();
            typeof(Enemy).GetField("<enemyData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data);
            typeof(EnemyMovementRb).GetField("nextGetSpeedTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, 100f);
            typeof(EnemyMovementRb).GetField("storedSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, 7f);
            MyTime.time = 1f; enemy.transform.position = Vector3.zero; movement.rb.position = Vector3.zero; attractor.transform.position = Vector3.forward * 10f;
            movement.Suck(attractor.transform);
            if (movement.state != EnemyMovementRb.State.Sucked || Mathf.Abs(movement.distanceToTarget - 10f) > .001f) throw new Exception("Suction did not select and immediately track the attractor");
            var velocity = (Vector3)typeof(EnemyMovementRb).GetField("desiredVelocity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(movement);
            if (Vector3.Distance(velocity, Vector3.forward * 7f) > .01f) throw new Exception("Suction did not move toward the attractor");
            movement.Suck(player.transform);
            if (typeof(EnemyMovementRb).GetField("suckTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(movement) != (object)attractor.transform) throw new Exception("Repeated suction replaced an active target");
            var blackHoleProjectile = blackHole.AddComponent<ProjectileBlackHole>();
            var victims = (HashSet<Enemy>)typeof(ProjectileBlackHole).GetField("suckedEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(blackHoleProjectile); victims.Add(enemy);
            typeof(ProjectileBlackHole).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(blackHoleProjectile, null);
            if (movement.state != EnemyMovementRb.State.Normal || victims.Count != 0) throw new Exception("Black hole cleanup did not release sucked enemy");
            typeof(EnemyMovementRb).GetField("totalSuckTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, 15f);
            movement.Suck(attractor.transform);
            if (movement.state != EnemyMovementRb.State.Normal) throw new Exception("Suction ignored maximum time");
            Debug.Log("EnemyTargetRotationPortCheck passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(attractor); UnityEngine.Object.DestroyImmediate(blackHole); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(game);
            MyTime.time = oldTime;
            GameManager.Instance = oldGame; PlayerMovement.Instance = oldPlayer;
        }
    }
}
