using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Pickups;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class EnemyDropPlayCheck
{
    const string Pending = "EnemyDropPlayCheck.pending";
    static EnemyDropPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("enemy drop runner").AddComponent<EnemyDropRunner>().Begin(); } }
}
public class EnemyDropRunner : MonoBehaviour
{
    sealed class FixedRandom : System.Random { public override double NextDouble() => 0; public override int Next(int min, int max) => min; }
    static void Require(bool b, string s) { if (!b) throw new Exception("EnemyDropPlayCheck: " + s); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        PickupManager pickups = null; PlayerStatusEffects effects = null;
        try
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "ground"; ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = new Vector3(0,-.5f,0); ground.transform.localScale = new Vector3(100,1,100);
            var state = new GameObject("test state"); state.SetActive(false);
            var manager = state.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsGround = 1 << ground.layer; manager.whatIsGroundAndObjects = manager.whatIsGround;
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.PowerupChance] = 1, [EStat.PowerupBoostMultiplier] = 1 }; player.inventory.playerStats = stats;
            effects = new PlayerStatusEffects(); player.inventory.statusEffects = effects;
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var xpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab");
            var powerupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GenericPowerup.prefab");
            var orbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PickupOrb.prefab");
            Require(xpPrefab && powerupPrefab && orbPrefab, "original prefabs loaded");
            pool.xpPool = new ObjectPool<GameObject>(() => Instantiate(xpPrefab), o => o.SetActive(true), o => o.SetActive(false));
            pool.powerupPool = new ObjectPool<GameObject>(() => Instantiate(powerupPrefab), o => o.SetActive(true), o => o.SetActive(false));
            var effect = state.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.pickupOrbFx = orbPrefab;
            pickups = new GameObject("pickup manager").AddComponent<PickupManager>(); pickups.whatIsPickups = 0;
            var enemyObj = new GameObject("dead enemy"); enemyObj.SetActive(false); var enemy = enemyObj.AddComponent<Enemy>(); enemyObj.transform.position = new Vector3(0,2,0);
            var data = ScriptableObject.CreateInstance<EnemyData>(); data.xp = 12;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            typeof(Enemy).GetField("basePowerupDropChance", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 1f);
            global::Utility.MyRandom.random = new FixedRandom(); MyTime.time = 120f; MyTime.fixedDeltaTime = .02f;
            Enemy.A_EnemyDied(enemy, null);
            Require(pool.xpPool.CountAll == 1, "enemy death created XP from original prefab");
            var xp = FindObjectOfType<XpVisuals>(); Require(xp != null && xp.pickup.GetValue() == 12, "XP value and original visual component");
            var orb = FindObjectOfType<PickupOrb>(); Require(orb != null && orb.ePickup == EPickup.Haste, "enemy death spawned haste orb");
            orb.transform.position = new Vector3(0, 1.5f, 0); orb.rb.velocity = Vector3.down * 10f; Physics.SyncTransforms();
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        float timeout = Time.realtimeSinceStartup + 6f;
        while (Time.realtimeSinceStartup < timeout && PoolManager.Instance.powerupPool.CountAll == 0) yield return new WaitForFixedUpdate();
        try
        {
            Require(PoolManager.Instance.powerupPool.CountAll == 1, "orb collision spawned original powerup prefab");
            var generic = FindObjectOfType<GenericPowerupPrefab>(); Require(generic != null && generic.pickup.ePickup == EPickup.Haste, "correct dropped powerup");
            MyPlayer.Instance.transform.position = generic.pickup.transform.position;
            generic.pickup.StartFollowingPlayer(MyPlayer.Instance.transform);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try
        {
            Require(effects.HasStatusEffect(EStatusEffect.Haste), "collecting powerup activates haste");
            Require(PoolManager.Instance.powerupPool.CountInactive == 1 && pickups.CanSpawnPowerup(), "collected powerup returned to pool");
            Debug.Log("EnemyDropPlayCheck passed: death, original XP, orb collision, haste pickup and pool return"); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
