using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Spawning.New.Timelines;
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
public static class BloodmarkLifecyclePlayCheck
{
    const string Pending = "BloodmarkLifecyclePlayCheck.pending";
    static BloodmarkLifecyclePlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("bloodmark runner").AddComponent<BloodmarkLifecycleRunner>().Begin(); } }
}
public class BloodmarkLifecycleRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BloodmarkLifecyclePlayCheck: " + message); }
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, P).SetValue(obj, value);
    static Enemy CreateEnemy(EnemyManager manager, uint id, float hp, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
        var obj = Instantiate(prefab, position, Quaternion.identity); var enemy = obj.GetComponent<Enemy>();
        foreach (var component in obj.GetComponentsInChildren<MonoBehaviour>(true)) if (component != enemy && component != enemy.enemyMovement) component.enabled = false;
        var data = ScriptableObject.CreateInstance<EnemyData>(); data.enemyName = EEnemy.Skeleton;
        Set(enemy, "<enemyData>k__BackingField", data); Set(enemy, "controlHp", hp); Set(enemy, "nextVerifyTime", 1000f);
        enemy.hp = enemy.maxHp = hp; enemy.state = EEnemyState.Default; enemy.rb.useGravity = false; enemy.dissolve.Reset();
        manager.enemies.Add(id, enemy); manager.collidersToEnemies.Add(enemy.collider, enemy); return enemy;
    }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        EnemyManager manager = null; ItemInventory items = null; Enemy expired = null; Enemy threshold = null; Enemy immediate = null; int deaths = 0;
        try
        {
            var state = new GameObject("runtime state"); state.SetActive(false);
            GameManager.Instance = state.AddComponent<GameManager>(); GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); Set(GameManager.Instance, "gameTimer", 1f);
            MyPlayer.Instance = state.AddComponent<MyPlayer>(); Set(MyPlayer.Instance, "baseDamage", 5f);
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            inventory.itemInventory = items = new ItemInventory(); inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); inventory.playerHealth.hp = 50; inventory.playerHealth.maxHp = 100;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 1f; stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; inventory.playerStats = stats;
            EffectManager.Instance = state.AddComponent<EffectManager>(); EffectManager.Instance.gloveBlood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BloodyGlovesFx.prefab");
            var stage = ScriptableObject.CreateInstance<StageData>(); stage.stageTimeline = new StageTimeline();
            typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
            typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
            manager = new GameObject("enemy manager").AddComponent<EnemyManager>();
            expired = CreateEnemy(manager, 1, 100f, Vector3.zero); threshold = CreateEnemy(manager, 2, 40f, Vector3.right * 30f); immediate = CreateEnemy(manager, 3, 35f, Vector3.right * 60f);
            Enemy.A_EnemyDied += (enemy, dc) => { if (enemy == threshold || enemy == immediate) { deaths++; Require(dc.damageSource == "Bloodmark", "death records bloodmark source"); } };
            items.AddItem(EItem.GloveBlood, 2); MyTime.time = 10f; MyTime.fixedDeltaTime = .02f; Physics.SyncTransforms();
            items.PostDamage(new DamageContainer(1f, "test") { enemy = expired });
            Require(Mathf.Abs(expired.hp - 68.5f) < .01f && expired.debuffsToAdd.ContainsKey(EDebuff.Bloodmark), "hit queues bloodmark on original enemy prefab");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try { Require(expired.HasDebuff(EDebuff.Bloodmark), "enemy manager applies queued mark"); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        for (int i = 0; i < 10; i++) { MyTime.time += .5f; yield return new WaitForFixedUpdate(); }
        try
        {
            Require(!expired.HasDebuff(EDebuff.Bloodmark) && Mathf.Abs(expired.hp - 61f) < .01f, "manager ticks expire bloodmark and apply damage");
            MyTime.time = 20f; items.PostDamage(new DamageContainer(1f, "test") { enemy = threshold });
            Require(Mathf.Abs(threshold.hp - 8.5f) < .01f, "threshold target hit");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try
        {
            Require(threshold.HasDebuff(EDebuff.Bloodmark), "threshold mark is active");
            threshold.DamageExternal(new DamageContainer(0f, "test") { enemy = threshold, damage = 1f });
            Require(threshold.debuffsToRemove.Contains(EDebuff.Bloodmark), "damage queues lethal mark"); MyTime.time += .5f;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try
        {
            Require(threshold.hp == 0f && deaths == 1 && !threshold.HasDebuff(EDebuff.Bloodmark), "manager processes lethal threshold and death");
            MyTime.time = 30f; items.PostDamage(new DamageContainer(1f, "test") { enemy = immediate }); Require(Mathf.Abs(immediate.hp - 3.5f) < .01f, "immediate target receives initial hit");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForFixedUpdate();
        try { Require(immediate.hp == 0f && deaths == 2, "mark immediately kills during normal enemy update"); Debug.Log("BloodmarkLifecyclePlayCheck passed: manager-driven expiry, threshold death and immediate lethal mark on original enemy prefab"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
