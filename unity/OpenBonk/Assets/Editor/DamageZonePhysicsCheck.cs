using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class DamageZonePhysicsCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var oldGame = GameManager.Instance;
        var oldPlayer = MyPlayer.Instance;
        var oldManager = EnemyManager.Instance;
        float oldTime = MyTime.time;
        var root = new GameObject("DamageZonePhysicsCheck");
        var data = ScriptableObject.CreateInstance<WeaponData>();
        WeaponBase weapon = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>();
            EnemyManager.Instance = root.AddComponent<EnemyManager>();
            MyPlayer.Instance = root.AddComponent<MyPlayer>();
            MyPlayer.Instance.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.SizeMultiplier] = 1.5f };
            MyPlayer.Instance.inventory.playerStats = stats;
            data.eWeapon = EWeapon.FireStaff;
            data.Init();
            weapon = new WeaponBase(data);

            int layer = LayerMask.NameToLayer("Enemy");
            Require(layer >= 0, "Enemy layer missing");
            GameManager.Instance.whatIsEnemy = 1 << layer;
            root.transform.position = new Vector3(10000, 10000, 10000);
            var zoneObject = new GameObject("zone");
            zoneObject.transform.SetParent(root.transform, false);
            var zone = zoneObject.AddComponent<DamageZone>();
            zone.defaultRadius = 4f;
            zone.cooldown = 2f;
            zone.trailRenderer = zoneObject.AddComponent<TrailRenderer>();
            zone.Set(weapon);
            Require(Mathf.Abs(zone.transform.localScale.x - 1.5f) < .001f, "scale differs");
            Require(Mathf.Abs(zone.trailRenderer.startWidth - 1.5f) < .001f, "trail width differs");
            float radius = (float)typeof(DamageZone).GetField("radius", Private).GetValue(zone);
            Require(Mathf.Abs(radius - 6f) < .001f, "radius differs");

            SphereCollider MakeCollider(string name, float distance, int objectLayer, bool trigger)
            {
                var obj = new GameObject(name);
                obj.transform.SetParent(root.transform, false);
                obj.transform.localPosition = new Vector3(distance, 0, 0);
                obj.layer = objectLayer;
                var collider = obj.AddComponent<SphereCollider>();
                collider.radius = .2f;
                collider.isTrigger = trigger;
                return collider;
            }
            var near = MakeCollider("dead enemy", 2f, layer, false);
            var deadEnemy = near.gameObject.AddComponent<Enemy>();
            typeof(Enemy).GetProperty("hp").SetValue(deadEnemy, 0f);
            EnemyManager.Instance.collidersToEnemies.Add(near, deadEnemy);
            MakeCollider("far enemy", 8f, layer, false);
            MakeCollider("other layer", 2f, 0, false);
            MakeCollider("trigger", 3f, layer, true);
            Physics.SyncTransforms();
            MyTime.time = 100f;
            int count = EnemyTargeting.GetEnemiesInRadiusSafe(zone, zone.transform.position, radius, out Collider[] found);
            Require(count == 1 && found[0] == near, "physics query included an out-of-range, other-layer or trigger collider");
            var tick = typeof(DamageZone).GetMethod("FixedUpdate", Private);
            var next = typeof(DamageZone).GetField("nextCheckDamageTime", Private);
            tick.Invoke(zone, null);
            Require((float)next.GetValue(zone) == 102f, "first cooldown deadline differs");
            MyTime.time = 101f;
            tick.Invoke(zone, null);
            Require((float)next.GetValue(zone) == 102f, "cooldown did not suppress query");
            MyTime.time = 102f;
            tick.Invoke(zone, null);
            Require((float)next.GetValue(zone) == 104f, "cooldown boundary differs");
            Debug.Log("DamageZonePhysicsCheck PASS");
        }
        finally
        {
            weapon?.Cleanup();
            GameManager.Instance = oldGame;
            MyPlayer.Instance = oldPlayer;
            EnemyManager.Instance = oldManager;
            MyTime.time = oldTime;
            UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
