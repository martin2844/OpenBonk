using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NukePickupPlayCheck
{
    const string Pending = "NukePickupPlayCheck.pending";
    static NukePickupPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("nuke check runner").AddComponent<NukePickupRunner>().Begin(); } }
}
public class NukePickupRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("NukePickupPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        Enemy enemy = null;
        try
        {
            var root = new GameObject("test state"); root.SetActive(false);
            var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.PowerupBoostMultiplier] = 1f }; player.inventory.playerStats = stats;
            var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
	            var bossObj = new GameObject("boss"); bossObj.SetActive(false); bossObj.layer = LayerMask.NameToLayer("Enemy"); bossObj.transform.position = Vector3.right * 4f;
            var collider = bossObj.AddComponent<SphereCollider>(); var body = bossObj.AddComponent<Rigidbody>(); body.isKinematic = true;
            enemy = bossObj.AddComponent<Enemy>(); enemy.enabled = false; enemy.hp = enemy.maxHp = 1000;
            typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f);
            typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, EEnemyFlag.Boss);
            var data = ScriptableObject.CreateInstance<EnemyData>(); data.enemyName = EEnemy.Goblin;
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            enemies.collidersToEnemies.Add(collider, enemy); bossObj.SetActive(true);
            var effectObj = new GameObject("effect manager"); var effects = effectObj.AddComponent<EffectManager>();
            effects.nukePickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/NukePickupEffect.prefab");
            Require(effects.nukePickup != null && effects.nukePickup.GetComponent<KillEnemyTrigger>() != null, "original nuke prefab and trigger");
            var pickupObj = new GameObject("nuke pickup"); pickupObj.SetActive(false); var pickup = pickupObj.AddComponent<Pickup>(); pickup.ePickup = EPickup.Nuke;
            MyTime.fixedDeltaTime = .02f; MyTime.time = 10f;
            Pickup.A_PickupTriggered(pickup);
            Require(FindObjectOfType<Explosion>() != null, "pickup event spawned explosion");
            Physics.SyncTransforms();
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
        float timeout = Time.realtimeSinceStartup + 4f;
        while (Time.realtimeSinceStartup < timeout && enemy.hp == 1000f) yield return new WaitForFixedUpdate();
        try
        {
            Require(Mathf.Abs(enemy.hp - 850f) < .01f, "expanding explosion dealt 15 percent boss maximum health");
            Debug.Log("NukePickupPlayCheck passed: original explosion spawned and damaged a boss via physics"); EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
