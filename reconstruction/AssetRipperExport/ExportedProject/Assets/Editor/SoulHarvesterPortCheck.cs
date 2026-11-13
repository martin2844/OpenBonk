using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class SoulHarvesterPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string why) { if (!b) throw new Exception("SoulHarvesterPortCheck: " + why); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldPool = PoolManager.Instance; var oldData = DataManager.Instance;
        var root = new GameObject("soul harvester check"); root.SetActive(false); var projectiles = new List<GameObject>(); ItemInventory items = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            typeof(MyPlayer).GetField("baseDamage", P).SetValue(player, 10f);
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            inv.playerStats.stats = new Dictionary<EStat, float> { [EStat.ProjectileSpeedMultiplier] = 1f, [EStat.DurationMultiplier] = 1f };
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/SoulHarvester.asset"); Check(asset != null && asset.eItem == EItem.SoulHarvester, "original item asset");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/AngrySoulProjectile.prefab"); Check(prefab != null && prefab.GetComponent<ItemProjectile>() != null, "original projectile prefab");
            pool.angrySoulPool = new ObjectPool<GameObject>(() => { var go = UnityEngine.Object.Instantiate(prefab); projectiles.Add(go); return go; }, go => go.SetActive(true), go => go.SetActive(false), null, true, 10, 100);
            var enemy = root.AddComponent<Enemy>(); root.transform.position = new Vector3(10, 5, 2);
            items = inv.itemInventory = new ItemInventory(); items.AddItem(EItem.SoulHarvester);
            var item = (ItemSoulHarvester)items.GetItem(EItem.SoulHarvester); Check(item != null, "inventory acquisition");
            Enemy.A_EnemyReleasedFromPool?.Invoke(enemy);
            Check(projectiles.Count == 2, "two projectiles on enemy release");
            foreach (var go in projectiles) { var p = go.GetComponent<ItemProjectile>(); Check(go.activeSelf && go.transform.position == root.transform.position, "projectile active at enemy position"); Check(Mathf.Abs((float)typeof(ItemProjectile).GetField("damage", P).GetValue(p) - 8.5f) < .001f, "one-stack damage"); Check((string)typeof(ItemProjectile).GetField("damageSource", P).GetValue(p) == "SoulHarvester", "damage source"); }
            items.AddItem(EItem.SoulHarvester); Enemy.A_EnemyReleasedFromPool?.Invoke(enemy);
            Check(projectiles.Count == 4 && Mathf.Abs((float)typeof(ItemProjectile).GetField("damage", P).GetValue(projectiles[2].GetComponent<ItemProjectile>()) - 26f) < .001f, "stacked damage");
            items.RemoveItem(EItem.SoulHarvester); items.RemoveItem(EItem.SoulHarvester); Enemy.A_EnemyReleasedFromPool?.Invoke(enemy); Check(projectiles.Count == 4, "unsubscribe on removal");
            Debug.Log("SoulHarvesterPortCheck passed: original assets, inventory, release event, spawn position, projectile damage, stacking and cleanup");
        }
        finally { items?.Cleanup(); foreach (var go in projectiles) UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; PoolManager.Instance = oldPool; DataManager.Instance = oldData; }
    }
}
