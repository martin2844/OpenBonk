using System;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using UnityEngine;

public static class EnemyTargetingPortCheck
{
    private static Enemy MakeEnemy(EnemyManager manager, string name, Vector3 pos, int layer)
    {
        var obj = new GameObject(name);
        obj.layer = layer;
        obj.transform.position = pos;
        var collider = obj.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        var enemy = obj.AddComponent<Enemy>();
        enemy.hp = 10f;
        manager.collidersToEnemies.Add(collider, enemy);
        return enemy;
    }
    private static void Expect(Enemy actual, Enemy expected, string label)
    {
        if (actual != expected) throw new Exception(label + ": got " + (actual ? actual.name : "null"));
    }
    public static void Run()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) throw new Exception("enemy layer missing");
        var gm = new GameObject("manager").AddComponent<GameManager>();
        GameManager.Instance = gm;
        gm.whatIsEnemy = 1 << enemyLayer;
        gm.whatIsProjectileObstruction = 1 << 0;
        var manager = new GameObject("enemy-manager").AddComponent<EnemyManager>();
        EnemyManager.Instance = manager;
        var save = new GameObject("save").AddComponent<SaveManager>();
        save.Init(); save.config = new ConfigSaveFile();
        var player = new GameObject("player").AddComponent<MyPlayer>();
        MyPlayer.Instance = player;
        player.playerRenderer = new GameObject("visual").AddComponent<PlayerRenderer>();
        var closeBehind = MakeEnemy(manager, "close behind", new Vector3(0f, 0f, -3f), enemyLayer);
        var ahead = MakeEnemy(manager, "ahead", new Vector3(0f, 0f, 5f), enemyLayer);
        Physics.SyncTransforms();
        var cfg = save.config.cfGameSettings;
        cfg.enemy_targeting_mode = 1;
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, null), closeBehind, "nearest");
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, closeBehind.gameObject), ahead, "excluded nearest");
        cfg.enemy_targeting_mode = 0;
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, null), closeBehind, "smart nearby behind bonus");
        closeBehind.transform.position = new Vector3(0f, 0f, 2f);
        typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ahead, EEnemyFlag.Boss);
        Physics.SyncTransforms();
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, null), ahead, "smart boss priority");
        typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ahead, EEnemyFlag.None);
        closeBehind.transform.position = new Vector3(0f, 0f, -3f);
        Physics.SyncTransforms();
        closeBehind.hp = 0f;
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, null), ahead, "dead target skipped");
        closeBehind.hp = 10f;
        cfg.enemy_targeting_mode = 1;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0f, 0f, -1.5f);
        wall.transform.localScale = new Vector3(2f, 2f, 0.25f);
        Physics.SyncTransforms();
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, true, null), ahead, "blocked closest target");
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, false, null), closeBehind, "vision disabled");
        Expect(EnemyTargeting.GetRandomEnemyInRadius(Vector3.zero, 20f, true, null), ahead, "random visible target");
        cfg.enemy_targeting_mode = 0;
        Expect(EnemyTargeting.GetEnemy(Vector3.zero, 20f, 0, true, null), ahead, "smart visible target");
        var pick = typeof(EnemyTargeting).GetMethod("ShouldPickRandom", BindingFlags.Static | BindingFlags.NonPublic);
        bool Pick(int index, float ratio) => (bool)pick.Invoke(null, new object[] {index, ratio});
        if (Pick(0, 1f) || Pick(1, 0f) || !Pick(1, 1f) || Pick(2, 0.5f) || !Pick(3, 0.5f)) throw new Exception("random targeting cadence");
        closeBehind.hp = 0f;
        var weaponData = ScriptableObject.CreateInstance<WeaponData>();
        weaponData.eWeapon = EWeapon.FireStaff; weaponData.spawnProjectileRange = 20f;
        weaponData.spawnOffset = new Vector3(0f, 0f, 1f); weaponData.useVision = true; weaponData.Init();
        var projectile = new GameObject("fireball").AddComponent<ProjectileExploding>();
        projectile.weaponBase = new WeaponBase(weaponData);
        var exploded = typeof(ProjectileExploding).GetField("exploded", BindingFlags.Instance | BindingFlags.NonPublic);
        exploded.SetValue(projectile, true);
        var init = typeof(ProjectileExploding).GetMethod("TryInit", BindingFlags.Instance | BindingFlags.NonPublic);
        if (!(bool)init.Invoke(projectile, new object[] {0}) || (bool)exploded.GetValue(projectile)) throw new Exception("fireball initialization");
        if (Vector3.Distance(projectile.direction, Vector3.forward) > 0.0001f || Vector3.Distance(projectile.transform.position, Vector3.forward) > 0.0001f) throw new Exception("fireball direction and spawn offset");
        typeof(ProjectileBase).GetField("projectileSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(projectile, 0.5f);
        typeof(ProjectileBase).GetMethod("StepMovement", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectile, null);
        if (Vector3.Distance(projectile.transform.position, Vector3.forward * 1.5f) > 0.0001f) throw new Exception("fireball movement step");
        var homing = new GameObject("homing").AddComponent<ProjectileHoming>();
        homing.weaponBase = new WeaponBase(weaponData);
        var homingInit = typeof(ProjectileHoming).GetMethod("TryInit", BindingFlags.Instance | BindingFlags.NonPublic);
        var homingDirection = typeof(ProjectileHoming).GetMethod("GetMovementDirection", BindingFlags.Instance | BindingFlags.NonPublic);
        if (!(bool)homingInit.Invoke(homing, new object[] { 0 }) || Vector3.Distance(homing.direction, Vector3.forward) > 0.0001f) throw new Exception("homing initialization and initial direction");
        ahead.transform.position = new Vector3(5f, 0f, 0f);
        if (Vector3.Distance((Vector3)homingDirection.Invoke(homing, null), Vector3.right) > 0.0001f) throw new Exception("homing follows a moving target");
        var hasBounces = typeof(ProjectileHoming).GetMethod("HasBounces", BindingFlags.Instance | BindingFlags.NonPublic);
        homing.bounces = 1; homing.maxBounces = 2;
        if (!(bool)hasBounces.Invoke(homing, null)) throw new Exception("homing bounce remaining");
        homing.bounces = 2;
        if ((bool)hasBounces.Invoke(homing, null)) throw new Exception("homing bounce limit");
        ahead.hp = 0f;
        homing.gameObject.SetActive(false);
        typeof(ProjectileHoming).GetMethod("FindMovementDirection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(homing, null);
        if (homing.direction != Vector3.zero) throw new Exception("homing retarget with no enemies");
        if ((bool)init.Invoke(projectile, new object[] {0})) throw new Exception("fireball should not spawn without target");
        typeof(ProjectileBasic).GetMethod("FindMovementDirection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(projectile, null);
        if (projectile.direction != Vector3.zero) throw new Exception("fireball retarget with no enemies");
        Debug.Log("ENEMY_TARGETING_PORT_CHECK_OK");
    }
}
