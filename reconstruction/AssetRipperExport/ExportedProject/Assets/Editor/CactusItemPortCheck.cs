using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;

public static class CactusItemPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string text) { if (!value) throw new Exception("CactusItemPortCheck: " + text); }
    static Enemy Target(EnemyManager manager, Vector3 position)
    {
        var go = new GameObject("target"); go.layer = LayerMask.NameToLayer("Enemy"); go.transform.position = position;
        var col = go.AddComponent<SphereCollider>(); var enemy = go.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); enemy.renderer = go.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = go.AddComponent<Rigidbody>(); enemy.enemyMovement = go.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        manager.collidersToEnemies.Add(col, enemy); return enemy;
    }
    public static void Run()
    {
        var root = new GameObject("cactus item test"); root.SetActive(false);
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>();
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
        inv.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.Thorns] = 3f; stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Armor] = 0f; stats.stats[EStat.DamageReductionMultiplier] = 0f; inv.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 100; health.maxHp = 100; inv.playerHealth = health;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Cactus.asset"); Require(asset != null && asset.eItem == EItem.Cactus, "original Cactus asset");
        ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CactusProjectile.prefab"); Require(prefab != null, "original Cactus effect prefab");
        GameObject effect = null;
        pool.cactusPool = new ObjectPool<GameObject>(() => effect = UnityEngine.Object.Instantiate(prefab));
        var forward = Target(manager, Vector3.forward * 6f);
        var side = Target(manager, Quaternion.AngleAxis(144f, Vector3.up) * Vector3.forward * 6f);
        var far = Target(manager, Vector3.forward * 18f);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string desc = asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(!string.IsNullOrEmpty(desc) && !desc.Contains("{"), "localized description");
            inv.itemInventory.AddItem(EItem.Cactus, 2); var item = (ItemCactus)inv.itemInventory.GetItem(EItem.Cactus);
            Require((int)typeof(ItemCactus).GetField("numProjectiles", P).GetValue(item) == 5, "two items yield five directions");
            Physics.SyncTransforms(); PlayerHealth.A_TakeDamage?.Invoke(null, new DamageContainer(0f, "test"), false);
            Require(Mathf.Abs(forward.hp - 987f) < .001f && Mathf.Abs(side.hp - 987f) < .001f && far.hp == 1000, "real radial casts apply base plus thorns damage within range");
            Require(effect != null && effect.GetComponent<ParticleSystem>().emission.GetBurst(0).count.constant == 5f, "original effect burst matches projectile count");
            health.DamagePlayerExternal(10f, 0f, Vector3.zero, flags: DcFlags.BypassEvade);
            Require(health.hp == 90 && Mathf.Abs(forward.hp - 974f) < .001f && Mathf.Abs(side.hp - 974f) < .001f, "actual player-health damage triggers Cactus retaliation");
            inv.itemInventory.RemoveItem(EItem.Cactus); inv.itemInventory.RemoveItem(EItem.Cactus);
            float hp = forward.hp; PlayerHealth.A_TakeDamage?.Invoke(null, new DamageContainer(0f, "test"), false); Require(forward.hp == hp, "last removal unsubscribes damage event");
            Debug.Log("CactusItemPortCheck passed: original asset, radial hits, thorns, stack count, effect burst and event cleanup");
        }
        finally { inv.itemInventory.Cleanup(); inv.statusEffects.OnDestroy(); foreach (var enemy in new[]{forward, side, far}) { UnityEngine.Object.DestroyImmediate(enemy.enemyData); UnityEngine.Object.DestroyImmediate(enemy.gameObject); } if (effect != null) UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(root); GameManager.Instance = null; MyPlayer.Instance = null; EnemyManager.Instance = null; PoolManager.Instance = null; DataManager.Instance = null; }
    }
}
