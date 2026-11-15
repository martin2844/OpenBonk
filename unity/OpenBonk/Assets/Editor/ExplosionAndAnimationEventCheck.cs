using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using UnityEngine;

public static class ExplosionAndAnimationEventCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    public static void Run()
    {
        var oldGame = GameManager.Instance;
        var oldEnemies = EnemyManager.Instance;
        var root = new GameObject("ExplosionAndAnimationEventCheck");
        try
        {
            var game = root.AddComponent<GameManager>();
            var manager = root.AddComponent<EnemyManager>();
            GameManager.Instance = game;
            EnemyManager.Instance = manager;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            Require(enemyLayer >= 0, "Enemy layer is missing");
            game.whatIsEnemy = (1 << enemyLayer) | 1;
            Vector3 origin = new Vector3(10000, 10000, 10000);
            root.transform.position = origin;

            Enemy CreateEnemy(string name, Vector3 offset, int layer)
            {
                var obj = new GameObject(name);
                obj.transform.SetParent(root.transform);
                obj.transform.position = origin + offset;
                obj.layer = layer;
                var body = obj.AddComponent<Rigidbody>();
                body.useGravity = false;
                var collider = obj.AddComponent<SphereCollider>();
                collider.radius = 1f;
                var enemy = obj.AddComponent<Enemy>();
                typeof(Enemy).GetProperty("hp").SetValue(enemy, 100f);
                var movement = obj.AddComponent<EnemyMovementRb>();
                movement.rb = body;
                movement.enemy = enemy;
                typeof(EnemyMovementRb).GetField("lastFoundKnockbackResTime", Private).SetValue(movement, float.MaxValue);
                enemy.enemyMovement = movement;
                manager.collidersToEnemies.Add(collider, enemy);
                return enemy;
            }
            var near = CreateEnemy("near", new Vector3(2, 0, 0), enemyLayer);
            var far = CreateEnemy("far", new Vector3(10.5f, 0, 0), enemyLayer);
            var ignored = CreateEnemy("other layer", new Vector3(0, 0, 2), 0);
            var trigger = root.AddComponent<ExplodeEnemyTrigger>();
            trigger.radius = 10f;
            Physics.SyncTransforms();
            typeof(ExplodeEnemyTrigger).GetMethod("Start", Private).Invoke(trigger, null);
            var velocity = typeof(EnemyMovementRb).GetField("knockbackVelocity", Private);
            Vector3 nearVelocity = (Vector3)velocity.GetValue(near.enemyMovement);
            Vector3 farVelocity = (Vector3)velocity.GetValue(far.enemyMovement);
            Vector3 ignoredVelocity = (Vector3)velocity.GetValue(ignored.enemyMovement);
            Require(Mathf.Abs(nearVelocity.magnitude - 50f) < .05f, "near enemy knockback differs");
            Require(Mathf.Abs(farVelocity.magnitude - 40f) < .05f, "far enemy knockback differs");
            Require(nearVelocity.x > 0f && nearVelocity.y > 0f, "knockback direction differs");
            Require(ignoredVelocity == Vector3.zero, "non-enemy layer was affected");

            var animated = root.AddComponent<AnimatedMesh>();
            AnimatedMesh.AnimationEndEvent first = _ => { };
            AnimatedMesh.AnimationEndEvent second = _ => { };
            animated.OnAnimationEnd += first;
            animated.OnAnimationEnd += second;
            var field = typeof(AnimatedMesh).GetField("OnAnimationEnd", Private);
            Require(field != null && ((Delegate)field.GetValue(animated)).GetInvocationList().Length == 2, "event add lost a listener");
            animated.OnAnimationEnd -= first;
            Require(((Delegate)field.GetValue(animated)).GetInvocationList().Length == 1, "event remove lost the wrong listeners");
            animated.OnAnimationEnd -= second;
            Require(field.GetValue(animated) == null, "event remove did not clear the last listener");
            Debug.Log("ExplosionAndAnimationEventCheck PASS");
        }
        finally
        {
            GameManager.Instance = oldGame;
            EnemyManager.Instance = oldEnemies;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
