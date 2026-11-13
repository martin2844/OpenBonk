using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class EnergyCorePortCheck
{
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("EnergyCorePortCheck: " + reason); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldManager = EnemyManager.Instance; var oldPool = PoolManager.Instance; var oldEffect = EffectManager.Instance; float oldTime = MyTime.time; bool oldPaused = MyTime.paused;
        var root = new GameObject("energy core test"); root.SetActive(false); var enemyObj = new GameObject("energy target"); var orbs = new List<GameObject>(); PlayerInventory inv = null; EnemyData data = null;
        try
        {
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 10f);
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
            var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; EffectManager.Instance = null;
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f;
            inv.playerStats.stats[EStat.CritChance] = 0f;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/HomingOrb.prefab"); Require(prefab != null && prefab.GetComponent<ItemProjectile>() != null, "original orb prefab");
            pool.orbPool = new ObjectPool<GameObject>(() => { var obj = UnityEngine.Object.Instantiate(prefab); orbs.Add(obj); return obj; }, obj => obj.SetActive(true), obj => obj.SetActive(false), null, true, 10, 200);
            enemyObj.layer = LayerMask.NameToLayer("Enemy"); enemyObj.transform.position = new Vector3(30, 0, 0);
            var collider = enemyObj.AddComponent<SphereCollider>(); var enemy = enemyObj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 100f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 100f);
            data = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); manager.collidersToEnemies.Add(collider, enemy); Physics.SyncTransforms();
            inv.itemInventory.AddItem(EItem.EnergyCore); var core = (ItemEnergyCore)inv.itemInventory.GetItem(EItem.EnergyCore);
            MyTime.paused = false; MyTime.time = 10f; inv.itemInventory.Tick();
            Require(orbs.Count == 1 && pool.orbPool.CountActive == 1, "one-stack burst starts immediately");
            var shot = orbs[0].GetComponent<ItemProjectile>();
            Require((int)typeof(ItemProjectile).GetField("projectileIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shot) == 0 && (int)typeof(ItemProjectile).GetField("projectilesCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shot) == 7, "first orb has index and count");
            Require(Mathf.Abs((float)typeof(ItemProjectile).GetField("damage", Private).GetValue(shot) - 28f) < .001f && Mathf.Abs((float)typeof(ItemProjectile).GetField("range", Private).GetValue(shot) - 70f) < .001f, "projectile damage and range");
            float next = (float)typeof(ItemEnergyCore).GetField("nextOrbTime", Private).GetValue(core); MyTime.time = next - .001f; inv.itemInventory.Tick(); Require(orbs.Count == 1, "spacing delays next orb");
            for (int i = 1; i < 7; i++) { MyTime.time = (float)typeof(ItemEnergyCore).GetField("nextOrbTime", Private).GetValue(core); inv.itemInventory.Tick(); }
            Require(orbs.Count == 7 && pool.orbPool.CountActive == 7 && (int)typeof(ItemEnergyCore).GetField("orbsLeftToShoot", Private).GetValue(core) == 0, "seven spaced orbs emitted");
            bool collision = (bool)typeof(ItemProjectile).GetMethod("CheckCollision", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shot, new object[] { collider, Vector3.zero });
            Require(collision && Mathf.Abs(enemy.hp - 72f) < .001f && pool.orbPool.CountInactive == 1, "orb hits enemy for 28 and returns to pool");
            MyTime.time = 13.99f; inv.itemInventory.Tick(); Require(pool.orbPool.CountActive == 6, "burst cooldown prevents early restart");
            MyTime.time = 14f; inv.itemInventory.Tick(); Require(pool.orbPool.CountActive == 7 && orbs.Count == 7, "next burst reuses returned orb");
            Debug.Log("EnergyCorePortCheck passed: original orb, timing, damage and reuse");
        }
        finally
        {
            inv?.itemInventory?.Cleanup(); foreach (var orb in orbs) UnityEngine.Object.DestroyImmediate(orb);
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(enemyObj); UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; EnemyManager.Instance = oldManager; PoolManager.Instance = oldPool; EffectManager.Instance = oldEffect; MyTime.time = oldTime; MyTime.paused = oldPaused;
        }
    }
}
