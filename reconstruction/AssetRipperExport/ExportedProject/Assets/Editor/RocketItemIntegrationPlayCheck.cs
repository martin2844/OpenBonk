using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
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
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RocketItemIntegrationPlayCheck
{
    const string Pending = "RocketItemIntegrationPlayCheck.pending";
    static RocketItemIntegrationPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("rocket test runner").AddComponent<RocketItemIntegrationRunner>().Begin(); } }
}
public class RocketItemIntegrationRunner : MonoBehaviour
{
    class AlwaysProc : System.Random { public override double NextDouble() => 0; }
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RocketItemIntegrationPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try
        {
            var state = new GameObject("state"); state.SetActive(false);
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.ProjectileSpeedMultiplier] = 1f }; inventory.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game;
            var enemyGo = new GameObject("target actor"); enemyGo.SetActive(false); enemyGo.transform.position = new Vector3(0,1,5);
            var enemy = enemyGo.AddComponent<Enemy>(); enemy.hp = 100f; typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 100f); var enemyData = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{enemyData});
            var hitGo = new GameObject("target collider"); hitGo.layer = LayerMask.NameToLayer("Enemy"); hitGo.transform.position = enemyGo.transform.position; var collider = hitGo.AddComponent<SphereCollider>(); collider.radius = .5f;
            game.whatIsEnemy = 1 << hitGo.layer; game.whatIsProjectileRaycast = 1 << hitGo.layer; game.whatIsProjectileObstruction = 0;
            var enemies = state.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; enemies.collidersToEnemies.Add(collider,enemy);
            var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
            var itemData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Slutty Cannon.asset"); Check(itemData != null && itemData.eItem == EItem.SluttyCannon, "original item asset");
            typeof(DataManager).GetField("itemData",P).SetValue(data,new Dictionary<EItem,ItemData>{{EItem.SluttyCannon,itemData}});
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.rocket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Rocket2.prefab"); pool.explosionFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketExplosion.prefab");
            Check(pool.rocket != null && pool.explosionFx != null, "original rocket and explosion prefabs"); typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null); Check(pool.rocketPool != null, "rocket pool initialized");
            global::Utility.MyRandom.random = new AlwaysProc(); UnityEngine.Random.InitState(7); MyTime.paused = false; MyTime.time = 10f; Physics.SyncTransforms();
            inventory.itemInventory.AddItem(EItem.SluttyCannon); var item = inventory.itemInventory.GetItem(EItem.SluttyCannon) as ItemSluttyCannon;
            Check(item != null && item.HasOnHitEffectProc() && Mathf.Abs(item.damageRatio - 1.4f) < .001f, "item registration and stack scaling");
            var dc = new DamageContainer(1f,"test") { damage = 20f, enemy = enemy };
            for (int i=0;i<5;i++) inventory.itemInventory.PostDamage(dc);
            Check(pool.rocketPool.CountAll == 4 && pool.rocketPool.CountActive == 4, "four-proc tick limit");
            Rocket rocket = FindObjectOfType<Rocket>(); Check(rocket != null, "original rocket launched");
            for (int i=0;i<100 && rocket.gameObject.activeSelf;i++) { MyTime.time += .02f; typeof(Rocket).GetMethod("FixedUpdate",P).Invoke(rocket,null); }
            Check(Mathf.Abs(enemy.hp - 72f) < .001f && pool.rocketPool.CountInactive == 1, "automatic target, flight, scaled damage and pool return");
            inventory.itemInventory.Tick(); inventory.itemInventory.PostDamage(dc); Check(pool.rocketPool.CountActive == 4, "tick resets proc limit and reuses rocket");
            inventory.itemInventory.Cleanup(); Debug.Log("RocketItemIntegrationPlayCheck passed: item proc, tick cap, original pool/prefab, autonomous target, damage and reuse"); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
