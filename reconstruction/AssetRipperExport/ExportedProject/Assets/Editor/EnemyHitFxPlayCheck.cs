using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class EnemyHitFxPlayCheck
{
    const string Pending = "EnemyHitFxPlayCheck.pending";
    static EnemyHitFxPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("hit fx runner").AddComponent<EnemyHitFxPlayRunner>().Begin(); } }
}
public class EnemyHitFxPlayRunner : MonoBehaviour
{
    string error;
    void OnLog(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) error = message + "\n" + stack; }
    static void Require(bool ok, string message) { if (!ok) throw new Exception("EnemyHitFxPlayCheck: " + message); }
    public void Begin() { Application.logMessageReceived += OnLog; StartCoroutine(Run()); }
    void Update() { MyTime.time += Time.deltaTime; MyTime.deltaTime = Time.deltaTime; }
    static ReturnToPool Find(ObjectPool<GameObject> pool)
    {
        foreach (var item in FindObjectsOfType<ReturnToPool>()) if (item.pool == pool) return item;
        return null;
    }
    IEnumerator Run()
    {
        PoolManager pool = null; EffectManager effect = null; Enemy enemy = null;
        try
        {
            new GameObject("listener").AddComponent<AudioListener>();
            var root = new GameObject("state"); root.SetActive(false);
            pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.critEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CritEffect.prefab");
            pool.megaCritFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MegacritEffect.prefab");
            pool.executeFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ExecuteEffect.prefab");
            pool.bloodmark = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BloodmarkFx.prefab");
            Require(pool.critEffectPrefab && pool.megaCritFx && pool.executeFx && pool.bloodmark, "original effect prefabs are present");
            typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var target = new GameObject("enemy"); target.SetActive(false); target.transform.position = Vector3.forward * 4f;
            enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = target.AddComponent<CapsuleCollider>(); enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 100f;
            typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 100f);
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
            target.SetActive(true); Physics.SyncTransforms();
            MyTime.time = 10f; effect = new GameObject("effects").AddComponent<EffectManager>();
            enemy.DamageExternal(new DamageContainer(0f, "test") { crit = true, damage = 5f });
            Enemy.A_Damage?.Invoke(enemy, new DamageContainer(0f, "test") { damageEffect = EDamageEffect.Megacrit });
            Enemy.A_Damage?.Invoke(enemy, new DamageContainer(0f, "test") { damageEffect = EDamageEffect.Execute, crit = true });
            Enemy.A_Damage?.Invoke(enemy, new DamageContainer(0f, "test") { damageEffect = EDamageEffect.Bloodmark });
            Require(pool.critEffectPool.CountActive == 1 && pool.megaCritPool.CountActive == 1 && pool.executePool.CountActive == 1 && pool.bloodmarkPool.CountActive == 1, "each damage type takes its own effect");
            Require(enemy.hp == 95f, "critical effect follows actual enemy damage");
            var crit = Find(pool.critEffectPool); var mega = Find(pool.megaCritPool); var execute = Find(pool.executePool); var blood = Find(pool.bloodmarkPool);
            Require(crit && mega && execute && blood, "effects have return timers");
            Vector3 expected = Vector3.Lerp(enemy.collider.ClosestPoint(player.transform.position), enemy.transform.position, .5f);
            Require(Vector3.Distance(crit.transform.position, expected) < .001f && Vector3.Distance(mega.transform.position, expected) < .001f, "critical effects position between collider and enemy center");
            Require(Vector3.Distance(execute.transform.position, enemy.transform.position + Vector3.up * 3f) < .001f && Vector3.Distance(blood.transform.position, enemy.GetCenterPosition()) < .001f, "execution and bloodmark positions");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float timeout = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < timeout && (pool.critEffectPool.CountActive != 0 || pool.megaCritPool.CountActive != 0 || pool.executePool.CountActive != 0 || pool.bloodmarkPool.CountActive != 0) && error == null) yield return null;
        try
        {
            Require(error == null, "unexpected Unity error: " + error);
            Require(pool.critEffectPool.CountInactive == 1 && pool.megaCritPool.CountInactive == 1 && pool.executePool.CountInactive == 1 && pool.bloodmarkPool.CountInactive == 1, "effects return on original timers");
            UnityEngine.Object.Destroy(effect.gameObject);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return null;
        try
        {
            Require(Enemy.A_Damage == null, "destroyed manager unsubscribes from enemy damage");
            Debug.Log("EnemyHitFxPlayCheck passed: original crit, megacrit, execute and bloodmark effects, positions, pool returns and unsubscribe"); Application.logMessageReceived -= OnLog; EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
