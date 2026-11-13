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
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ShatteredWisdomPlayCheck
{
    const string Pending = "ShatteredWisdomPlayCheck.pending";
    static ShatteredWisdomPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("runner").AddComponent<ShatteredWisdomPlayRunner>().Begin(); } }
}
public class ShatteredWisdomPlayRunner : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    string error;
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) error = message + "\n" + stack; }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception("ShatteredWisdomPlayCheck: " + reason); }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture;
        var state = new GameObject("state"); state.SetActive(false);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", Private).SetValue(player, 10f);
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
        inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f; inv.playerStats.stats[EStat.CritChance] = 0f;
        var game = state.AddComponent<GameManager>(); GameManager.Instance = game; var enemies = state.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; EffectManager.Instance = null;
        var target = new GameObject("target"); target.SetActive(false); target.transform.position = new Vector3(2f, 0, 0);
        var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 200f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 200f);
        var data = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        var collisionObject = new GameObject("target collider"); collisionObject.layer = LayerMask.NameToLayer("Enemy"); collisionObject.transform.position = target.transform.position;
        var collider = collisionObject.AddComponent<SphereCollider>(); collider.radius = .3f; enemies.collidersToEnemies.Add(collider, enemy); game.whatIsEnemy = 1 << collisionObject.layer;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/XpPickup.prefab"); Check(prefab != null, "original XP prefab");
        var parent = new GameObject("inactive parent"); parent.SetActive(false); var xp = Instantiate(prefab, parent.transform); var pickup = xp.GetComponent<Pickup>(); var damager = xp.GetComponent<XpDamager>();
        Check(pickup != null && damager != null && damager.pickup == pickup, "original pickup and linked damage component");
        foreach (var behaviour in xp.GetComponentsInChildren<MonoBehaviour>(true)) if (behaviour != pickup && behaviour != damager) behaviour.enabled = false;
        pickup.animatePosition = false; pickup.animateRotation = false;
        MyTime.paused = false; MyTime.time = 10f; MyTime.fixedDeltaTime = .02f; parent.SetActive(true); pickup.Set(Vector3.zero, 2, 0); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); xp.transform.position = new Vector3(1.2f, 0, 0); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        Check(Mathf.Abs(enemy.hp - 200f) < .001f, "no item means no damage");
        parent.SetActive(false); xp.transform.position = Vector3.zero; inv.itemInventory.AddItem(EItem.ShatteredWisdom); inv.itemInventory.AddItem(EItem.ShatteredWisdom); MyTime.time = 20f; parent.SetActive(true); pickup.Set(Vector3.zero, 2, 0); Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); xp.transform.position = new Vector3(1.2f, 0, 0); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        Check(Mathf.Abs(enemy.hp - 160f) < .001f, "two stacks and XP value two deal 40; hp=" + enemy.hp);
        xp.transform.position = Vector3.zero; Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); MyTime.time = 20.1f; xp.transform.position = new Vector3(1.2f, 0, 0); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        Check(Mathf.Abs(enemy.hp - 160f) < .001f, "per-enemy cooldown");
        xp.transform.position = Vector3.zero; Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); MyTime.time = 20.3f; pickup.AddValue(1); xp.transform.position = new Vector3(1.2f, 0, 0); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        Check(Mathf.Abs(enemy.hp - 100f) < .001f, "value change updates damage after cooldown; hp=" + enemy.hp);
        Check(error == null, "unexpected Unity error: " + error); Application.logMessageReceived -= Capture;
        Debug.Log("ShatteredWisdomPlayCheck passed: original pickup, inventory, moving XP damage, cooldown and value update"); EditorApplication.Exit(0);
    }
}
