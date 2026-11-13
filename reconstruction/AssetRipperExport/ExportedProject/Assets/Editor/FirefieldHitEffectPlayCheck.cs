using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FirefieldHitEffectPlayCheck
{
    const string Pending = "FirefieldHitEffectPlayCheck.pending";
    static FirefieldHitEffectPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("mine test runner").AddComponent<FirefieldHitEffectPlayRunner>().Begin(); } }
}
public class FirefieldHitEffectPlayRunner : MonoBehaviour
{
    ProjectileBase shot; string error;
    static void Check(bool b, string s) { if (!b) throw new Exception("FirefieldHitEffectPlayCheck: " + s); }
    void Capture(string m, string s, LogType t) { if (t == LogType.Exception || t == LogType.Error || t == LogType.Assert) error = m + "\n" + s; }
    public void Begin() => StartCoroutine(Guard());
    IEnumerator Guard() { var test = Run(); while (true) { bool more; try { more = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; } }
    void Update() { MyTime.deltaTime = Time.deltaTime; }
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Application.logMessageReceived += Capture; var root = new GameObject("test state"); root.SetActive(false); root.transform.position = new Vector3(0f, .5f, 0f);
        var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); gm.whatIsGroundAndObjects = 1 << LayerMask.NameToLayer("Ground");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies; var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>(); player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; pool.firefield = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/FirefieldProc.prefab"); pool.defaultHitPrefab = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GeneralProjectileHit.prefab")); pool.defaultHitPrefab.SetActive(false); pool.defaultHitPrefab.GetComponent<RandomSfx>().playOnAwake = false; pool.defaultHitPrefab.GetComponent<AudioSource>().clip = null; pool.explosionFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketExplosion.prefab"); typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null); EffectManager.Instance = root.AddComponent<EffectManager>();
        var floor = new GameObject("floor"); floor.layer = LayerMask.NameToLayer("Ground"); floor.transform.position = new Vector3(0f, -.5f, 0f); floor.AddComponent<BoxCollider>().size = new Vector3(50f, 1f, 50f);
        var target = new GameObject("stationary enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0f, .6f, -2f);
        var collider = target.AddComponent<CapsuleCollider>(); collider.radius = .8f; collider.height = 2f; var enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = target.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
        Enemy nearby = CreateEnemy(enemies, new Vector3(0f, .6f, -3f));
        Enemy distant = CreateEnemy(enemies, new Vector3(0f, .6f, 20f));
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponFlameWalker.asset"); Check(data != null, "original data"); data.Init(); Check(data.attack != null && data.attack.GetComponent<WeaponAttack>().prefabProjectile != null, "original attack references");
        var weapon = new WeaponBase(data); WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn; MyTime.time = 10f; MyTime.paused = false; Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        float deadline = Time.realtimeSinceStartup + 5f; while (Time.realtimeSinceStartup < deadline && shot == null) yield return null;
        Check(shot is ProjectileFirefield, "original firefield projectile spawned");
        var flame = (ProjectileFirefield)shot; Check(flame.firefield != null, "original field component assigned");
        var radius = (float)typeof(Firefield).GetField("spawnRadius", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flame.firefield);
        Check(Mathf.Abs(radius - 4f) < .01f && Mathf.Abs(flame.transform.position.z + 2f) < .1f, "field positioned behind player with original radius");
        while (Time.realtimeSinceStartup < deadline && (enemy.hp >= 1000f || nearby.hp >= 1000f)) yield return null;
        Check(enemy.hp < 1000f && nearby.hp < 1000f && distant.hp == 1000f, "firefield damages nearby enemies only");
        Check(pool.projectileHitPools != null && pool.projectileHitPools.TryGetValue(EWeapon.Flamewalker.ToString(), out var hitPool) && hitPool.CountAll >= 2, "weapon field spawned hit effects for both enemies");
        int activeEffects = 0, armedSounds = 0;
        foreach (AttackHit hit in FindObjectsOfType<AttackHit>())
        {
            if (hit.pool != pool.projectileHitPools[EWeapon.Flamewalker.ToString()]) continue;
            activeEffects++;
            if (hit.randomSfx != null && hit.randomSfx.s != null && hit.randomSfx.s.clip != null) armedSounds++;
        }
        Debug.Log($"Firefield audio diagnostic: active={activeEffects}, clips={armedSounds}");
        Check(activeEffects == 2 && armedSounds == 1, "only first hit in batch starts hit audio");
        Vector3 visualScale = flame.firefield.transform.localScale;
        Check(visualScale.x > 0f && Mathf.Abs(visualScale.x - visualScale.y) < .001f && Mathf.Abs(visualScale.x - visualScale.z) < .001f, "field grows uniformly from reset scale");
        while (Time.realtimeSinceStartup < deadline && pool.projectilePools[data.eWeapon].CountInactive == 0) yield return null;
        Check(pool.projectilePools[data.eWeapon].CountInactive > 0, "field projectile returned after duration"); Check(error == null, "unexpected error: " + error);
        var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/FirefieldProc.prefab"); Check(itemPrefab != null, "original item firefield prefab");
        var itemObj = Instantiate(itemPrefab); var itemField = itemObj.GetComponent<Firefield>(); Check(itemField != null, "item field component"); itemField.enabled = false;
        MyTime.time += 2f; float before = enemy.hp; int activeBefore = pool.projectileHitPools[EWeapon.Flamewalker.ToString()].CountActive; itemField.Set(new Vector3(0f, .5f, -2f), Vector3.zero, 4f, 2f, 7f, null, "Dragonfire-test"); Physics.SyncTransforms();
        typeof(Firefield).GetMethod("CheckDamage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(itemField, null);
        typeof(Firefield).GetMethod("TryPopDamage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(itemField, null);
        Check(enemy.hp < before, "item firefield damages target");
        Check(pool.projectileHitPools[EWeapon.Flamewalker.ToString()].CountActive >= activeBefore + 2, "item field spawns effects in shared Flamewalker pool"); Check(error == null, "unexpected effect error: " + error);
        // Both sources share the static hit accumulator. Exercise the two possible
        // first LateUpdate callers with genuine field instances and damage events.
        var weaponObj = Instantiate(itemPrefab); var weaponField = weaponObj.GetComponent<Firefield>(); weaponField.enabled = false;
        var checkDamage = typeof(Firefield).GetMethod("CheckDamage", BindingFlags.Instance | BindingFlags.NonPublic);
        var popDamage = typeof(Firefield).GetMethod("TryPopDamage", BindingFlags.Instance | BindingFlags.NonPublic);
        string observedSource = null; int observedHits = 0; float observedDamage = 0f;
        Action<Enemy, DamageContainer> captureDamage = (hitEnemy, dc) => { if (hitEnemy == enemy) { observedSource = dc.damageSource; observedDamage = dc.damage; observedHits++; } };
        Enemy.A_Damage += captureDamage;
        foreach (bool weaponFirst in new[] { true, false })
        {
            MyTime.time += 1f;
            weaponField.Set(new Vector3(0f, .5f, -2f), Vector3.zero, 4f, 2f, 7f, weapon, data.damageSourceName);
            itemField.Set(new Vector3(0f, .5f, -2f), Vector3.zero, 4f, 2f, 7f, null, "Dragonfire-test");
            Physics.SyncTransforms();
            observedSource = null; observedHits = 0;
            checkDamage.Invoke(weaponField, null); checkDamage.Invoke(itemField, null);
            popDamage.Invoke(weaponFirst ? weaponField : itemField, null);
            popDamage.Invoke(weaponFirst ? itemField : weaponField, null);
            Check(observedHits == 1 && observedSource == (weaponFirst ? data.damageSourceName : "Dragonfire-test"), "overlap is batched under the first field to pop it");
            if (!weaponFirst) Check(Mathf.Abs(observedDamage - 14f) < .01f, "item damage is multiplied by both overlapping fields");
            Debug.Log($"Firefield mixed-source overlap: first={(weaponFirst ? "weapon" : "item")}, targetEvents={observedHits}, source={observedSource}, damage={observedDamage}");
        }
        Enemy.A_Damage -= captureDamage;
        // Exercise the actual Dragonfire item with a guaranteed proc; the collision and
        // field lifetime then run through ordinary Unity updates and the real pool.
        var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
        var dragonfireData = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Dragonfire.asset");
        Check(dragonfireData != null, "original Dragonfire item data");
        typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dataManager, new Dictionary<EItem, ItemData> { [EItem.Dragonfire] = dragonfireData });
        var itemInventory = player.inventory.itemInventory;
        itemInventory.AddItem(EItem.Dragonfire);
        Check(itemInventory.GetItem(EItem.Dragonfire) is ItemDragonfire, "inventory acquired Dragonfire");
        var incoming = new DamageContainer(100f, "controlled hit") { enemy = enemy, damage = 7f };
        int priorActive = pool.firefieldPool.CountActive; float hpBeforeProc = enemy.hp;
        itemInventory.PostDamage(incoming);
        Check(pool.firefieldPool.CountActive == priorActive + 1, "Dragonfire proc spawned a pooled field");
        deadline = Time.realtimeSinceStartup + 6f;
        while (Time.realtimeSinceStartup < deadline && (enemy.hp >= hpBeforeProc || pool.firefieldPool.CountInactive == 0)) yield return null;
        Check(enemy.hp < hpBeforeProc, "Dragonfire field damaged target during normal updates");
        Check(pool.firefieldPool.CountInactive > 0 && pool.firefieldPool.CountActive == priorActive, "Dragonfire field returned to pool after expiration");
        itemInventory.RemoveItem(EItem.Dragonfire, false);
        itemInventory.PostDamage(incoming);
        Check(pool.firefieldPool.CountActive == priorActive, "removed Dragonfire no longer procs");
        Check(error == null, "unexpected Dragonfire error: " + error);
        WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; Application.logMessageReceived -= Capture; Debug.Log("FirefieldHitEffectPlayCheck passed: weapon and item firefields, Dragonfire proc, damage and pool return"); EditorApplication.Exit(0);
    }
    static Enemy CreateEnemy(EnemyManager enemies, Vector3 position)
    {
        var obj = new GameObject("additional enemy"); obj.SetActive(false); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<CapsuleCollider>(); collider.radius = .5f; collider.height = 2f;
        var enemy = obj.AddComponent<Enemy>(); enemy.enabled = false; enemy.collider = collider; enemy.renderer = obj.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemies.collidersToEnemies.Add(collider, enemy); obj.SetActive(true); return enemy;
    }
    void OnSpawn(ProjectileBase p) { shot = p; }
}
