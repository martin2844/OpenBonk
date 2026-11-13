using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class SkeletonRewardPlayCheck
{
    const string Pending = "SkeletonRewardPlayCheck.pending";
    static SkeletonRewardPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("skeleton reward runner").AddComponent<SkeletonRewardRunner>().Begin(); } }
}
public class SkeletonRewardRunner : MonoBehaviour
{
    sealed class NoDropRandom : System.Random { public override double NextDouble() => .99; }
    static void Require(bool ok, string message) { if (!ok) throw new Exception("SkeletonRewardPlayCheck: " + message); }
    static Enemy MakeEnemy(EnemyData data)
    {
        var go = new GameObject("skeleton"); go.SetActive(false); go.transform.position = Vector3.up * 2;
        var enemy = go.AddComponent<Enemy>(); var movement = go.AddComponent<EnemyMovementRb>(); movement.rb = go.AddComponent<Rigidbody>(); enemy.enemyMovement = movement;
        var dissolve = go.AddComponent<EnemyDissolve>(); dissolve.enemyRenderer = go.AddComponent<MeshRenderer>(); dissolve.Reset(); enemy.dissolve = dissolve;
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        return enemy;
    }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = Vector3.down * .5f; ground.transform.localScale = new Vector3(100, 1, 100);
            var state = new GameObject("state"); state.SetActive(false);
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsGround = 1 << ground.layer;
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = Vector3.right * 20f;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
            inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.playerXp = new PlayerXp(); inventory.playerXp.xp = 7;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.PowerupChance] = 0, [EStat.PowerupBoostMultiplier] = 1, [EStat.ChestPriceMultiplier] = 1, [EStat.XpIncreaseMultiplier] = 1, [EStat.GoldIncreaseMultiplier] = 1 }; inventory.playerStats = stats;
            inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inventory.playerHealth.hp = 100;
            var audio = state.AddComponent<AudioManager>(); AudioManager.Instance = audio; var audioObject = new GameObject("audio sources"); audio.xp = audioObject.AddComponent<AudioSource>(); audio.gold = audioObject.AddComponent<AudioSource>();
            XpUtility.Init();
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var xpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab");
            var moneyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MoneyFlying.prefab");
            Require(xpPrefab != null && moneyPrefab != null, "original pickup prefabs");
            pool.xpPool = new ObjectPool<GameObject>(() => Instantiate(xpPrefab), o => o.SetActive(true), o => o.SetActive(false));
            pool.goldPool = new ObjectPool<GameObject>(() => Instantiate(moneyPrefab), o => o.SetActive(true), o => o.SetActive(false));
            var effects = state.AddComponent<EffectManager>(); typeof(EffectManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(effects, null);
            var pickups = new GameObject("pickup manager").AddComponent<PickupManager>(); pickups.whatIsPickups = 0;
            var data = ScriptableObject.CreateInstance<EnemyData>(); data.enemyName = EEnemy.XpSkeleton; data.xp = 2;
            var enemy = MakeEnemy(data);
            global::Utility.MyRandom.random = new NoDropRandom(); MyTime.time = 210f; MyTime.fixedDeltaTime = .02f;
            int required = XpUtility.XpToNextLevelTotal(inventory.playerXp.xp), count = Mathf.Min(required, 10), each = Mathf.CeilToInt((float)required / count);
            enemy.EnemyDied(new DamageContainer(0f, DamageContainer.unknownDamageSource));
            var list = (PickupStackableList)typeof(PickupManager).GetField("xpList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pickups);
            Require(list.pickupsList.Count == count + 1, "normal XP plus all special XP shards");
            int following = 0, specialValue = 0;
            foreach (var pickup in list.pickupsList) if (!pickup.collider.enabled) { following++; specialValue += pickup.GetValue(); }
            Require(following == count && specialValue == count * each, "special shards carry rounded XP and follow player");
            enemy.EnemyDied(new DamageContainer(0f, DamageContainer.unknownDamageSource)); Require(list.pickupsList.Count == count + 1, "repeated death does not duplicate reward");
            var goldData = ScriptableObject.CreateInstance<EnemyData>(); goldData.enemyName = EEnemy.GoldenSkeleton; goldData.xp = 2;
            MakeEnemy(goldData).EnemyDied(new DamageContainer(0f, DamageContainer.unknownDamageSource));
            int gold = 0; foreach (var money in FindObjectsOfType<MoneyFlying>()) gold += money.value;
            Require(pool.goldPool.CountAll > 0 && gold == 15, "gold skeleton creates half a chest price in original money objects");
            foreach (var pickup in new List<Pickup>(list.pickupsList)) if (!pickup.collider.enabled) {
                pickup.transform.position = player.transform.position;
                typeof(Pickup).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, null);
            }
            inventory.LateUpdate(); Require(inventory.playerXp.xp == 7 + specialValue, "collected shards add XP");
            foreach (var money in FindObjectsOfType<MoneyFlying>()) {
                money.transform.position = player.transform.position;
                typeof(MoneyFlying).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(money, null);
            }
            Require(inventory.goldInt == 15 && pool.goldPool.CountInactive == pool.goldPool.CountAll, "collected money adds gold and returns to pool");
            Debug.Log("SkeletonRewardPlayCheck passed: real enemy death, shard and money collection, XP/gold and duplicate-death guard"); EditorApplication.Exit(0);
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
