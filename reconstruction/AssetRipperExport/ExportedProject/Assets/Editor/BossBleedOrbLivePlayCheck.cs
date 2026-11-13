using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BossBleedOrbLivePlayCheck
{
    const string Pending = "BossBleedOrbLivePlayCheck.pending";
    static BossBleedOrbLivePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("bleed orb runner").AddComponent<BossBleedOrbLiveRunner>().Begin(); } }
}
public class BossBleedOrbLiveRunner : MonoBehaviour
{
    string failure; int hits;
    void Log(string message, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) failure = message + "\n" + stack; }
    static void Check(bool value, string text) { if (!value) throw new Exception("BossBleedOrbLivePlayCheck: " + text); }
    public void Begin() { Application.logMessageReceived += Log; StartCoroutine(Run()); }
    IEnumerator Run()
    {
        PlayerHealth health = null; PlayerStatusEffects status = null;
        try {
            var managers = new GameObject("managers"); managers.SetActive(false); GameManager.Instance = managers.AddComponent<GameManager>();
            int playerLayer = LayerMask.NameToLayer("Player"); Check(playerLayer >= 0, "Player layer exists"); GameManager.Instance.whatIsPlayer = 1 << playerLayer;
            var playerObj = new GameObject("player singleton"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerMovement = playerObj.AddComponent<PlayerMovement>(); var body = playerObj.AddComponent<Rigidbody>(); typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player.playerMovement, body);
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); status = new PlayerStatusEffects(); inv.statusEffects = status;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 0; stats.stats[EStat.EnemyDamageMultiplier] = 1; stats.stats[EStat.EnemyScalingMultiplier] = 1; stats.stats[EStat.DamageCooldownMultiplier] = 1; inv.playerStats = stats;
            health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 1000; inv.playerHealth = health; PlayerHealth.A_TakeDamage += (_, __, ___) => hits++;
            MapController.runConfig = new RunConfig { mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset") }; MyTime.time = 0; MyTime.fixedDeltaTime = .02f; MyTime.difficultyTimer = MyTime.finalSwarmTimer = 0;
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere); target.layer = playerLayer; target.transform.position = Vector3.zero;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossOrbBleed.prefab"); Check(prefab != null, "original prefab"); var orb = Instantiate(prefab, Vector3.back * 3, Quaternion.identity).GetComponent<BossOrbBleed>();
            foreach (var component in orb.GetComponentsInChildren<MonoBehaviour>(true)) if (component != orb) component.enabled = false;
            Physics.IgnoreLayerCollision(orb.gameObject.layer, playerLayer, false); orb.rb.useGravity = false; orb.rb.isKinematic = false; orb.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var bossObj = new GameObject("boss"); bossObj.SetActive(false); var boss = bossObj.AddComponent<Enemy>(); orb.Set(boss, 0, 1, 0); typeof(BossOrbBleed).GetMethod("ShootOrb", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(orb, null); orb.rb.velocity = Vector3.forward * 12; Physics.SyncTransforms();
        } catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        for (int i=0; i<100 && hits == 0 && failure == null; i++) { MyTime.fixedDeltaTime = .02f; yield return new WaitForFixedUpdate(); }
        try { Check(failure == null, "runtime error: " + failure); Check(hits == 1 && health.hp < 1000, "physical hit damages player"); Check(status.HasStatusEffect(EStatusEffect.Bleed), "physical hit applies bleed"); Debug.Log("BossBleedOrbLivePlayCheck passed: original orb prefab physical collision, player damage and bleed"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
