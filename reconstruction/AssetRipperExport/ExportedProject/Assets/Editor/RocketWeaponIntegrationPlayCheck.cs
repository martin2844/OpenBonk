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
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RocketWeaponIntegrationPlayCheck
{
    const string Pending = "RocketWeaponIntegrationPlayCheck.pending";
    static RocketWeaponIntegrationPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("rocket weapon runner").AddComponent<RocketWeaponIntegrationRunner>().Begin(); } }
}
public class RocketWeaponIntegrationRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RocketWeaponIntegrationPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        try
        {
            var state = new GameObject("state"); state.SetActive(false);
            var save = state.AddComponent<SaveManager>(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save}); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach(EStat s in Enum.GetValues(typeof(EStat))) stats.stats[s] = 1f;
            stats.stats[EStat.CritChance] = 0; stats.stats[EStat.KnockbackMultiplier] = 0; stats.stats[EStat.Projectiles] = 0; stats.stats[EStat.ProjectileBounces] = 0; inv.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game;
            var enemyGo = new GameObject("target actor"); enemyGo.SetActive(false); enemyGo.transform.position = Vector3.forward * 5;
            var enemy = enemyGo.AddComponent<Enemy>(); enemy.hp = 1000; typeof(Enemy).GetField("controlHp",P).SetValue(enemy,1000f); var enemyData = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{enemyData});
            var hitGo = new GameObject("target collider"); hitGo.layer = LayerMask.NameToLayer("Enemy"); hitGo.transform.position = enemyGo.transform.position; var collider = hitGo.AddComponent<SphereCollider>(); collider.radius = .5f;
            game.whatIsEnemy = 1 << hitGo.layer; game.whatIsProjectileRaycast = 1 << hitGo.layer; game.whatIsProjectileObstruction = 0;
            var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; manager.collidersToEnemies.Add(collider,enemy);
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSluttyRocket.asset"); Check(data != null && data.eWeapon == EWeapon.Rockets,"original weapon data"); data.Init(); var weapon = new WeaponBase(data);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketProjectile.prefab"); var attackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketAttack.prefab"); Check(projectilePrefab && attackPrefab,"original weapon prefabs");
            var attack = Instantiate(attackPrefab).GetComponent<WeaponAttack>(); attack.weaponBase = weapon; attack.prefabProjectile = projectilePrefab; attack.prefabMuzzle = null; attack.prefabHit = null;
            MyTime.paused = false; MyTime.time = 10; UnityEngine.Random.InitState(7); Physics.SyncTransforms();
            var projectile = pool.GetProjectile(attack) as ProjectileRocket; Check(projectile != null && projectile.rocket != null,"pooled wrapper and rocket"); projectile.transform.position = Vector3.zero;
            DamageContainer hit = null; Enemy.A_Damage += (e,dc) => { if (e == enemy) hit = dc; };
            projectile.Set(weapon,attack,0); Check(projectile.gameObject.activeSelf,"launch with target");
            for(int i=0;i<100 && projectile.gameObject.activeSelf;i++){ MyTime.time += .02f; typeof(Rocket).GetMethod("FixedUpdate",P).Invoke(projectile.rocket,null); }
            Check(enemy.hp < 1000f && hit != null && hit.damageSource == EWeapon.Rockets.ToString(),"weapon damage and source");
            Check(!projectile.gameObject.activeSelf && pool.projectilePools[EWeapon.Rockets].CountInactive == 1,"wrapper returned to weapon pool");
            collider.enabled = false; MyTime.time += 1f; Physics.SyncTransforms(); projectile = pool.GetProjectile(attack) as ProjectileRocket; projectile.Set(weapon,attack,0);
            Check(!projectile.gameObject.activeSelf && pool.projectilePools[EWeapon.Rockets].CountInactive == 1,"no-target launch returned to pool");
            inv.itemInventory.Cleanup(); weapon.Cleanup(); Debug.Log("RocketWeaponIntegrationPlayCheck passed: original wrapper acquisition, weapon hit, callback pool return and no-target cleanup"); EditorApplication.Exit(0);
        }
        catch(Exception ex){ Debug.LogException(ex); EditorApplication.Exit(1); }
        yield break;
    }
}
