using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class BorgarEatEffectPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BorgarEatEffectPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("borgar fixture"); root.SetActive(false);
        GameManager.Instance = root.AddComponent<GameManager>();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float>(); stats.stats[EStat.HealingMultiplier] = 1f; player.inventory.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 50; health.maxHp = 100; player.inventory.playerHealth = health;
        var manager = root.AddComponent<PoolManager>(); PoolManager.Instance = manager;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EatFx.prefab"); Require(prefab != null, "original eat effect prefab");
        GameObject effect = null; int returned = 0;
        manager.eatPool = new ObjectPool<GameObject>(() => { effect = UnityEngine.Object.Instantiate(prefab); effect.SetActive(false); return effect; });
        manager.borgorPool = new ObjectPool<GameObject>(() => new GameObject("unused"), actionOnRelease: _ => returned++);
        var pickupObj = new GameObject("borgar fixture pickup"); pickupObj.transform.position = new Vector3(3, 5, 7); var pickup = pickupObj.AddComponent<Borgar>(); pickup.Set(10, .08f);
        try
        {
            typeof(Borgar).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pickup, null);
            Require(health.hp == 60 && returned == 1 && !pickupObj.activeSelf, "heals and returns pickup");
            Require(effect != null && effect.activeSelf && effect.transform.position == new Vector3(3, 5, 7), "original eat effect activated at pickup position");
            Debug.Log("BorgarEatEffectPortCheck passed: healing, pool return and original eat effect position");
        }
        finally { if (effect != null) UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(pickupObj); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; PoolManager.Instance = null; GameManager.Instance = null; }
    }
}
