using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GrandmasTonicPlayCheck
{
    const string Pending = "GrandmasTonicPlayCheck.pending";
    static GrandmasTonicPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("tonic test runner").AddComponent<GrandmasTonicPlayRunner>().Begin(); } }
}

public class GrandmasTonicPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message) { if (!value) throw new Exception("GrandmasTonicPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var root = new GameObject("tonic test state"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var assets = AssetDatabase.FindAssets("t:ItemData"); ItemData tonic = null; foreach (string guid in assets) { var a = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)); if (a != null && a.eItem == EItem.GrandmasSecretTonic) { tonic = a; break; } }
        Check(tonic != null, "original item asset"); ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(tonic.eItem, tonic);
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.grandmaTonicFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GrandmaCrit.prefab");
        Check(pool.grandmaTonicFx != null, "tonic effect prefab"); typeof(PoolManager).GetMethod("Start", P).Invoke(pool, null);
        var origin = CreateEnemy(enemies, Vector3.zero); var near = CreateEnemy(enemies, new Vector3(3f, 0f, 0f)); var far = CreateEnemy(enemies, new Vector3(10f, 0f, 0f)); Physics.SyncTransforms();
        player.inventory.itemInventory.AddItem(EItem.GrandmasSecretTonic); var item = player.inventory.itemInventory.GetItem(EItem.GrandmasSecretTonic);
        Check(item.HasOnHitEffectProc() && Mathf.Abs((float)item.GetType().GetField("radius", P).GetValue(item) - 4f) < .001f, "on-hit registration and radius");
        var dc = new DamageContainer(4f, "test") { damage = 20f, enemy = origin, crit = false }; item.ProcOnHitEffects(dc); item.LateFixedUpdate(); Check(near.hp == 1000f, "ordinary hit does not spread");
        dc.crit = true; dc.procCoefficient = 0f; item.ProcOnHitEffects(dc); item.LateFixedUpdate(); Check(near.hp == 1000f, "zero coefficient does not spread");
        dc.procCoefficient = 4f; item.ProcOnHitEffects(dc); item.ProcOnHitEffects(dc); Check(near.hp == 1000f, "spread waits for late physics update"); item.LateFixedUpdate();
        Check(origin.hp == 980f && near.hp == 980f && far.hp == 1000f && pool.grandmaTonicPool.CountActive == 2, "batched half damage, range and effect pool");
        item.LateFixedUpdate(); Check(near.hp == 980f, "queue cleared after dispatch");
        var counter = item.GetType().GetField("numProcsThisTick", P); counter.SetValue(item, 100); item.ProcOnHitEffects(dc); item.LateFixedUpdate(); Check(near.hp == 980f, "per-tick cap");
        item.Tick(); item.ProcOnHitEffects(dc); item.LateFixedUpdate(); Check(near.hp == 970f, "tick resets cap");
        stats.stats[EStat.SizeMultiplier] = 2f; player.inventory.itemInventory.AddItem(EItem.GrandmasSecretTonic); Check((float)item.GetType().GetField("radius", P).GetValue(item) == 8f, "stacked radius cap");
        item.Tick();
        player.inventory.itemInventory.PostDamage(dc);
        MyTime.paused = true; LateFixedUpdate.A_LateUpdate?.Invoke();
        Check(near.hp == 970f, "inventory retains pending spread while paused");
        MyTime.paused = false; LateFixedUpdate.A_LateUpdate?.Invoke();
        Check(near.hp == 960f && far.hp == 1000f, "inventory damage dispatch and late update apply one spread at the stacked radius");
        LateFixedUpdate.A_LateUpdate?.Invoke(); Check(near.hp == 960f, "inventory does not dispatch the same spread twice");
        player.inventory.itemInventory.RemoveItem(EItem.GrandmasSecretTonic);
        player.inventory.itemInventory.RemoveItem(EItem.GrandmasSecretTonic);
        player.inventory.itemInventory.PostDamage(dc); LateFixedUpdate.A_LateUpdate?.Invoke();
        Check(near.hp == 960f, "removed item no longer receives on-hit dispatch");
        player.inventory.itemInventory.Cleanup();
        Debug.Log("GrandmasTonicPlayCheck passed: critical-only proc, batch damage, radius, pool and cap"); EditorApplication.Exit(0); yield break;
    }
    static Enemy CreateEnemy(EnemyManager manager, Vector3 position)
    {
        var o = new GameObject("tonic target"); o.SetActive(false); o.layer = LayerMask.NameToLayer("Enemy"); o.transform.position = position;
        var col = o.AddComponent<CapsuleCollider>(); col.radius = .5f; col.height = 2f; var e = o.AddComponent<Enemy>(); e.enabled = false; e.collider = col; e.renderer = o.AddComponent<MeshRenderer>(); e.hp = e.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(e, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(e, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); manager.collidersToEnemies.Add(col, e); o.SetActive(true); return e;
    }
}
