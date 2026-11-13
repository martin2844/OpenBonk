using System;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class BorgarDropInventoryPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BorgarDropInventoryPortCheck: " + message); }
    public static void Run()
    {
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Borgar.asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Borgar.prefab");
        Require(asset != null && asset.isEnabled && asset.eItem == EItem.Borgar && prefab != null, "original enabled asset and pickup prefab");
        var root = new GameObject("borgar drop fixture"); root.SetActive(false);
        var manager = root.AddComponent<PoolManager>(); PoolManager.Instance = manager;
        GameObject spawned = null; int created = 0;
        manager.borgorPool = new ObjectPool<GameObject>(() => { created++; spawned = UnityEngine.Object.Instantiate(prefab); spawned.SetActive(false); return spawned; });
        var enemyObject = new GameObject("target"); enemyObject.transform.position = new Vector3(3, 4, 5);
        var enemy = enemyObject.AddComponent<Enemy>(); enemy.enemyMovement = enemyObject.AddComponent<EnemyMovementRb>();
        var inventory = new ItemInventory();
        try
        {
            inventory.AddItem(EItem.Borgar, 2);
            var item = inventory.GetItem(EItem.Borgar);
            typeof(Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations.ItemBorgor).GetField("chance", Private).SetValue(item, 1f);
            enemy.enemyMovement.distanceToTarget = 35f;
            Enemy.A_EnemyDied?.Invoke(enemy, null);
            Require(created == 0, "distance boundary excludes the drop");
            enemy.enemyMovement.distanceToTarget = 34.9f;
            Enemy.A_EnemyDied?.Invoke(enemy, null);
            Require(created == 1 && spawned.activeSelf && spawned.transform.position == enemy.transform.position, "near death spawns original pickup at target");
            var pickup = spawned.GetComponent<Borgar>();
            Require((int)typeof(Borgar).GetField("flatHeal", Private).GetValue(pickup) == 12 && Math.Abs((float)typeof(Borgar).GetField("ratioHeal", Private).GetValue(pickup) - .08f) < .0001f, "two-item healing values");
            inventory.RemoveItem(EItem.Borgar, false); inventory.RemoveItem(EItem.Borgar, false);
            Enemy.A_EnemyDied?.Invoke(enemy, null);
            Require(created == 1, "last removal unsubscribes from deaths");
            Debug.Log("BorgarDropInventoryPortCheck passed: original assets, death event, distance, healing and removal");
        }
        finally { inventory.Cleanup(); if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(root); PoolManager.Instance = null; }
    }
}
