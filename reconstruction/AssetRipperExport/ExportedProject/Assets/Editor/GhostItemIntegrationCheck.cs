using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public class GhostTestInteractable : BaseInteractable
{
    public bool success;
    public override bool Interact() => success;
    public override string GetInteractString() => "test";
}
public static class GhostItemIntegrationCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("GhostItemIntegrationCheck: " + message); }
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name,P).GetValue(obj);
    public static void Run()
    {
        var state = new GameObject("ghost test state"); state.SetActive(false);
        var enemyGo = new GameObject("enemy"); enemyGo.SetActive(false); enemyGo.transform.position = new Vector3(0,0,5);
        var target = new GameObject("target collider"); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = enemyGo.transform.position;
        var collider = target.AddComponent<SphereCollider>(); collider.enabled = false;
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        var spawned = new List<GameObject>();
        ItemInventory items = null;
        try
        {
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; items = inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 0f;
            stats.stats[EStat.DamageMultiplier] = 1f; stats.stats[EStat.ProjectileSpeedMultiplier] = 1f; stats.stats[EStat.DurationMultiplier] = 1.5f; inventory.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << target.layer; game.whatIsProjectileRaycast = game.whatIsEnemy; game.whatIsProjectileObstruction = 0;
            var enemies = state.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            var enemy = enemyGo.AddComponent<Enemy>(); enemy.hp = 100f; typeof(Enemy).GetField("controlHp",P).SetValue(enemy,100f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{enemyData}); enemies.collidersToEnemies.Add(collider,enemy);
            var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
            var itemData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Ghost_1.asset"); Check(itemData != null && itemData.eItem == EItem.Ghost, "original item asset");
            typeof(DataManager).GetField("itemData",P).SetValue(data,new Dictionary<EItem,ItemData>{{EItem.Ghost,itemData}});
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.ghost = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GhostProjectile.prefab"); Check(pool.ghost != null,"original projectile prefab");
            typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null); Check(pool.ghostPool != null,"ghost pool initialized");
            var effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects;
            var detector = state.AddComponent<DetectInteractables>(); var interaction = state.AddComponent<GhostTestInteractable>(); typeof(DetectInteractables).GetField("currentInteractable",P).SetValue(detector,interaction);
            global::Utility.MyRandom.random = new System.Random(4); UnityEngine.Random.InitState(4); MyTime.paused = false; MyTime.time = 10f; Physics.SyncTransforms();
            items.AddItem(EItem.Ghost); interaction.success = false; detector.TryInteract(); Check(pool.ghostPool.CountAll == 0,"failed interaction does not spawn");
            interaction.success = true; detector.TryInteract(); Check(pool.ghostPool.CountActive == 6,"one stack produces six projectiles");
            var dict = Get<Dictionary<GameObject,ItemProjectile>>(effects,"activeGhostProjectiles"); Check(dict.Count == 6,"projectiles recorded");
            ItemProjectile projectile = null; foreach (var pair in dict) { spawned.Add(pair.Key); projectile = pair.Value; }
            Check(Mathf.Abs(Get<float>(projectile,"damage") - 16f) < .001f && Get<string>(projectile,"damageSource") == "Ghost", "base damage and attribution");
            Check(Mathf.Abs(Get<float>(projectile,"expirationTime") - 77.5f) < .001f,"duration scaled by item and projectile");
            collider.enabled = true; Physics.SyncTransforms();
            for (int i=0;i<160 && projectile.gameObject.activeSelf;i++) { MyTime.time += .02f; typeof(ItemProjectile).GetMethod("FixedUpdate",P).Invoke(projectile,null); }
            Check(Mathf.Abs(enemy.hp-84f)<.001f && pool.ghostPool.CountInactive == 1,"interaction projectile targets and damages enemy");
            collider.enabled = false; items.AddItem(EItem.Ghost); MyTime.time += 1f; detector.TryInteract(); Check(pool.ghostPool.CountActive == 17 && pool.ghostPool.CountAll == 17,"two stacks spawn twelve and reuse returned projectile");
            foreach (var pair in dict) if (!spawned.Contains(pair.Key)) spawned.Add(pair.Key);
            items.RemoveItem(EItem.Ghost); items.RemoveItem(EItem.Ghost); int before = pool.ghostPool.CountActive; detector.TryInteract(); Check(pool.ghostPool.CountActive == before,"removal unsubscribes interaction");
            while (pool.ghostPool.CountAll < 180) effects.SpawnGhostProjectile(1f, 30f, "Ghost");
            float totalDamage = 0f; foreach (var value in dict.Values) totalDamage += Get<float>(value,"damage");
            effects.SpawnGhostProjectile(7f, 30f, "Ghost");
            float boostedDamage = 0f; foreach (var value in dict.Values) boostedDamage += Get<float>(value,"damage");
            Check(dict.Count == 180 && Mathf.Abs(boostedDamage - totalDamage - 7f) < .001f, "full pool boosts an existing projectile");
            Debug.Log("GhostItemIntegrationCheck passed: interaction success, item stacks, original pool and prefab, target hit, reuse and cleanup");
        }
        finally
        {
            items?.Cleanup(); var effects = EffectManager.Instance;
            if (effects != null) foreach (var go in Get<Dictionary<GameObject,ItemProjectile>>(effects,"activeGhostProjectiles").Keys) if (go != null && !spawned.Contains(go)) spawned.Add(go);
            foreach (var go in spawned) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            EnemyManager.Instance = null; GameManager.Instance = null; MyPlayer.Instance = null; DataManager.Instance = null; PoolManager.Instance = null; EffectManager.Instance = null;
            UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(enemyGo); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(enemyData);
        }
    }
}
