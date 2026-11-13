using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EnergyCoreFlightPlayCheck
{
    const string Pending = "EnergyCoreFlightPlayCheck.pending";
    static EnergyCoreFlightPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("energy flight runner").AddComponent<EnergyCoreFlightPlayRunner>().Begin(); } }
}
public class EnergyCoreFlightPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("EnergyCoreFlightPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var state = new GameObject("energy game state"); state.SetActive(false);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 10f);
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
        inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f; inv.playerStats.stats[EStat.CritChance] = 0f;
        var game = state.AddComponent<GameManager>(); GameManager.Instance = game; var enemies = state.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; EffectManager.Instance = null;
        var targetObject = new GameObject("energy target actor"); targetObject.SetActive(false); targetObject.transform.position = new Vector3(0, 1f, 7f);
        var enemy = targetObject.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 100f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 100f);
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
        var collisionObject = new GameObject("energy target collider"); collisionObject.layer = LayerMask.NameToLayer("Enemy"); collisionObject.transform.position = targetObject.transform.position;
        var collider = collisionObject.AddComponent<SphereCollider>(); collider.radius = 1f; enemies.collidersToEnemies.Add(collider, enemy);
        game.whatIsEnemy = 1 << collisionObject.layer; game.whatIsProjectileRaycast = 1 << collisionObject.layer; game.whatIsProjectileObstruction = 0;
        var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.orb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/HomingOrb.prefab");
        Check(pool.orb != null && pool.orb.GetComponent<ItemProjectile>() != null, "original orb prefab");
        typeof(PoolManager).GetMethod("Start", Private).Invoke(pool, null); Check(pool.orbPool != null, "actual orb pool initialization");
        UnityEngine.Random.InitState(7); MyTime.paused = false; MyTime.time = 10f; MyTime.fixedDeltaTime = .02f; Physics.SyncTransforms();
        inv.itemInventory.AddItem(EItem.EnergyCore); inv.itemInventory.Tick();
        var shot = FindAnyObjectByType<ItemProjectile>(); Check(shot != null && pool.orbPool.CountActive == 1, "inventory launched original projectile");
        Vector3 start = shot.transform.position; bool acquired = false, moved = false;
        for (int i = 0; i < 200 && shot.gameObject.activeSelf; i++)
        {
            MyTime.time += Time.fixedDeltaTime; yield return new WaitForFixedUpdate();
            acquired |= (Enemy)typeof(ItemProjectile).GetField("targetEnemy", Private).GetValue(shot) == enemy;
            moved |= Vector3.Distance(start, shot.transform.position) > .2f;
        }
        Check(acquired && moved, "projectile autonomously targeted and moved");
        Check(Mathf.Abs(enemy.hp - 72f) < .001f && pool.orbPool.CountInactive == 1, "automatic hit applies damage and returns projectile");
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture; inv.itemInventory.Cleanup();
        Debug.Log("EnergyCoreFlightPlayCheck passed: original pool, autonomous target, live physics collision and return"); EditorApplication.Exit(0);
    }
}
