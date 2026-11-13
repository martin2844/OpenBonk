using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QuinsMaskPlayCheck
{
    const string Pending = "QuinsMaskPlayCheck.pending";
    static QuinsMaskPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("mask check runner").AddComponent<QuinsMaskPlayRunner>().Begin(); } }
}
public class QuinsMaskPlayRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message) { if (!value) throw new Exception("QuinsMaskPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        var root = new GameObject("mask test state"); root.SetActive(false);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var mask = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Quins Mask.asset"); var aegis = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponAegis.asset"); Check(mask != null && aegis != null, "original assets");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(mask.eItem, mask); ((Dictionary<EWeapon, WeaponData>)typeof(DataManager).GetField("weapons", P).GetValue(data)).Add(EWeapon.Aegis, aegis);
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.quinMaskFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuinsMask.prefab"); Check(pool.quinMaskFx != null, "original effect"); typeof(PoolManager).GetMethod("Start", P).Invoke(pool, null);
        var origin = CreateEnemy(enemies, Vector3.zero); var near = CreateEnemy(enemies, new Vector3(3f, 0f, 0f)); var far = CreateEnemy(enemies, new Vector3(12f, 0f, 0f)); Physics.SyncTransforms();
        player.inventory.itemInventory.AddItem(EItem.QuinsMask); var item = player.inventory.itemInventory.GetItem(EItem.QuinsMask);
        Check(item.HasOnHitEffectProc() && (float)item.GetType().GetField("radius", P).GetValue(item) == 6f, "registration and radius");
        var hit = new DamageContainer(4f, "unrelated") { enemy = origin, damage = 20f }; item.ProcOnHitEffects(hit); Check(near.hp == 1000f, "unrelated damage excluded");
        foreach (string source in new[] { PlayerHealth.thornsDamageSource, ItemCactus.damageSource, aegis.damageSourceName, ItemElectricPlug.damageSource })
        { hit.damageSource = source; item.ProcOnHitEffects(hit); }
        Check(origin.hp == 960f && near.hp == 960f && far.hp == 1000f && pool.quinMaskPool.CountActive == 4, "four eligible sources, damage, range and effects");
        var counter = item.GetType().GetField("numProcsThisTick", P); counter.SetValue(item, 100); item.ProcOnHitEffects(hit); Check(near.hp == 960f, "cap"); item.Tick(); item.ProcOnHitEffects(hit); Check(near.hp == 950f, "cap reset");
        player.inventory.itemInventory.AddItem(EItem.QuinsMask); var modifiers = item.statModifiers[EStat.Thorns].GetModifiers(); float value = 0f; foreach (var modifier in modifiers) value = modifier.modification;
        Check(value == 40f && (float)item.GetType().GetField("radius", P).GetValue(item) == 7f, "stacked thorns and radius");
        Debug.Log("QuinsMaskPlayCheck passed: eligible sources, damage, effects, stacking and cap"); EditorApplication.Exit(0); yield break;
    }
    static Enemy CreateEnemy(EnemyManager manager, Vector3 position)
    {
        var o = new GameObject("mask target"); o.SetActive(false); o.layer = LayerMask.NameToLayer("Enemy"); o.transform.position = position;
        var col = o.AddComponent<CapsuleCollider>(); col.radius = .5f; col.height = 2f; var e = o.AddComponent<Enemy>(); e.enabled = false; e.collider = col; e.renderer = o.AddComponent<MeshRenderer>(); e.hp = e.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(e, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(e, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); manager.collidersToEnemies.Add(col, e); o.SetActive(true); return e;
    }
}
