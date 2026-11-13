using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class BobDeadPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string message) { if (!b) throw new Exception("BobDeadPortCheck: " + message); }
    static void Near(float a, float b, string message) { Check(Mathf.Abs(a - b) < .001f, message + " " + a + " != " + b); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldPool = PoolManager.Instance; var oldData = DataManager.Instance; var oldEffect = EffectManager.Instance; float oldTime = MyTime.time;
        var root = new GameObject("Bob Dead check"); root.SetActive(false); var ghosts = new List<GameObject>(); ItemInventory items = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", P).SetValue(player, 10f);
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float> { [EStat.ProjectileSpeedMultiplier] = 1f, [EStat.DurationMultiplier] = 2f };
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Bob Dead.asset"); Check(asset != null && asset.eItem == EItem.BobDead, "original item asset");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; EffectManager.Instance = root.AddComponent<EffectManager>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GhostProjectile.prefab"); Check(prefab != null && prefab.GetComponent<ItemProjectile>() != null, "original ghost prefab");
            pool.ghostPool = new ObjectPool<GameObject>(() => { var go = UnityEngine.Object.Instantiate(prefab); ghosts.Add(go); return go; }, go => go.SetActive(true), go => go.SetActive(false), null, true, 10, 180);
            items = inv.itemInventory = new ItemInventory(); MyTime.time = 10f; player.transform.position = Vector3.zero; items.AddItem(EItem.BobDead); var item = (ItemBobDead)items.GetItem(EItem.BobDead);
            player.transform.position = Vector3.right * 7f; items.Tick(); Check(ghosts.Count == 0, "partial distance accumulates");
            player.transform.position = Vector3.right * 14f; MyTime.time = 10.04f; items.Tick(); Check(ghosts.Count == 0, "sample interval");
            MyTime.time = 10.051f; items.Tick(); Check(ghosts.Count == 1, "14 units produces one ghost");
            var projectile = ghosts[0].GetComponent<ItemProjectile>(); Near((float)typeof(ItemProjectile).GetField("damage", P).GetValue(projectile), 15f, "ghost damage");
            Check((string)typeof(ItemProjectile).GetField("damageSource", P).GetValue(projectile) == "BobDead", "damage source"); Near((float)typeof(ItemProjectile).GetField("expirationTime", P).GetValue(projectile), MyTime.time + 40f, "duration passed through projectile");
            player.transform.position = Vector3.right * 40f; items.AddItem(EItem.BobDead); MyTime.time = 10.11f; items.Tick(); Check(ghosts.Count == 1, "stack change resets movement origin");
            player.transform.position = Vector3.right * 54f; MyTime.time = 10.17f; items.Tick(); Check(ghosts.Count == 3, "two stacks produce two ghosts");
            player.transform.position = Vector3.right * 100f; MyTime.time = 10.23f; items.Tick(); Check(ghosts.Count == 5, "long movement produces one batch");
            items.RemoveItem(EItem.BobDead); items.RemoveItem(EItem.BobDead); player.transform.position = Vector3.right * 200f; MyTime.time = 11f; items.Tick(); Check(ghosts.Count == 5, "removal stops spawning");
            Debug.Log("BobDeadPortCheck passed: original item and ghost, movement threshold and sampling, damage, duration, stacking and removal");
        }
        finally { items?.Cleanup(); foreach (var go in ghosts) UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; PoolManager.Instance = oldPool; DataManager.Instance = oldData; EffectManager.Instance = oldEffect; MyTime.time = oldTime; }
    }
}
