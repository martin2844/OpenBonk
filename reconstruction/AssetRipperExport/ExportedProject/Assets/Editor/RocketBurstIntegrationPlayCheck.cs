using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RocketBurstIntegrationPlayCheck
{
    const string Pending = "RocketBurstIntegrationPlayCheck.pending";
    static RocketBurstIntegrationPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("rocket burst runner").AddComponent<RocketBurstIntegrationRunner>().Begin(); } }
}
public class RocketBurstIntegrationRunner : MonoBehaviour
{
    class HalfRandom : System.Random { public override double NextDouble() => .5; }
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RocketBurstIntegrationPlayCheck: " + message); }
    static T Field<T>(Rocket rocket, string name) => (T)typeof(Rocket).GetField(name,P).GetValue(rocket);
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try
        {
            var state = new GameObject("state"); state.SetActive(false);
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(4,0,0);
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>{{EStat.ProjectileSpeedMultiplier,1f}}; inv.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; var enemies = state.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.rocket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Rocket2.prefab"); Check(pool.rocket != null,"original rocket prefab"); typeof(PoolManager).GetMethod("Start",P).Invoke(pool,null);
            var effects = new GameObject("effect manager").AddComponent<EffectManager>(); global::Utility.MyRandom.random = new HalfRandom(); MyTime.time = 10f; MyTime.paused = true;
            effects.SpawnRockets(2, 23f, .3f, "burst test"); Check(pool.rocketPool.CountAll == 0,"no launch while paused");
            yield return null; yield return null; Check(pool.rocketPool.CountAll == 0,"pause holds burst across frames");
            MyTime.paused = false; yield return null; MyTime.paused = true;
            Check(pool.rocketPool.CountAll == 1,"first launch after unpause");
            var first = FindObjectOfType<Rocket>(); Check(first != null && Mathf.Abs(Field<float>(first,"damage")-23f)<.001f && Mathf.Abs(Field<float>(first,"procCoefficient")-.3f)<.001f && Field<string>(first,"damageSource")=="burst test","damage, proc and source forwarded");
            Check(Vector3.Distance(first.transform.position,player.transform.position + Vector3.up * 2f)<.5f,"launch near player plus two units");
            yield return new WaitForSecondsRealtime(.25f); Check(pool.rocketPool.CountAll == 1,"second launch waits while paused");
            MyTime.paused = false; yield return new WaitForSecondsRealtime(.25f); MyTime.paused = true;
            Check(pool.rocketPool.CountAll == 2,"second launch after unpause");
            effects.SpawnRockets(0,1,0,"empty"); yield return null; Check(pool.rocketPool.CountAll == 2,"zero count does not launch");
            Debug.Log("RocketBurstIntegrationPlayCheck passed: pause gates, actual coroutine, pool, parameters and two-shot burst"); EditorApplication.Exit(0);
        }
        finally { MyTime.paused = false; }
    }
}
