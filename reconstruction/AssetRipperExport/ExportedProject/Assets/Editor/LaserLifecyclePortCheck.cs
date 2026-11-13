using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class LaserLifecyclePortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("LaserLifecyclePortCheck: " + message); }
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, P).GetValue(obj);
    static void Invoke(object obj, string name) => obj.GetType().GetMethod(name, P).Invoke(obj, null);
    public static void Run()
    {
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.playerRenderer = new GameObject("renderer").AddComponent<PlayerRenderer>(); player.playerRenderer.hips = new GameObject("hips").transform; player.playerRenderer.hips.position = Vector3.up;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        var targetObject = new GameObject("target"); targetObject.layer = LayerMask.NameToLayer("Enemy"); targetObject.transform.position = Vector3.forward * 5f;
        var collider = targetObject.AddComponent<CapsuleCollider>(); var enemy = targetObject.AddComponent<Enemy>(); enemy.collider = collider; enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); enemy.renderer = targetObject.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = targetObject.AddComponent<Rigidbody>(); enemy.enemyMovement = targetObject.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb; manager.collidersToEnemies.Add(collider, enemy);
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSpaceNoodle.asset"); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LaserBeamAttack.prefab");
        Require(data != null && prefab != null, "original weapon and laser prefab"); data.Init();
        var laser = UnityEngine.Object.Instantiate(prefab).GetComponent<LaserBeamAttack>(); laser.transform.position = Vector3.zero; laser.Set(new WeaponBase(data));
        typeof(LaserBeamAttack).GetMethod("OnWeaponToggled", P).Invoke(laser, new object[] { null });
        Require(!Field<bool>(laser, "isShooting"), "toggle with no target leaves idle laser stopped");
        MyTime.time = 10f; Physics.SyncTransforms(); Invoke(laser, "StartLaser");
        Require(Field<Enemy>(laser, "target") == enemy && Field<bool>(laser, "isShooting") && laser.linerenderer.enabled && laser.laserStart.activeSelf && laser.laserEnd.activeSelf, "original target begins firing");
        Require(enemy.debuffsToAdd.TryGetValue(EDebuff.Echo, out var pending) && Mathf.Abs(pending.duration - 8f) < .001f, "laser queues original echo duration");
        Require(Mathf.Abs(Field<float>(laser, "laserStopTime") - 14f) < .001f && Mathf.Abs(Field<float>(laser, "laserReadyTime") - 16f) < .001f, "original firing duration and cooldown");
        var otherObject = new GameObject("intervening enemy"); otherObject.layer = targetObject.layer; otherObject.transform.position = new Vector3(0f, .5f, 2.5f);
        var otherCollider = otherObject.AddComponent<CapsuleCollider>(); var other = otherObject.AddComponent<Enemy>(); other.collider = otherCollider; other.hp = other.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(other, 1000f); other.renderer = otherObject.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(other, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        other.rb = otherObject.AddComponent<Rigidbody>(); other.enemyMovement = otherObject.AddComponent<EnemyMovementRb>(); other.enemyMovement.enemy = other; other.enemyMovement.rb = other.rb; manager.collidersToEnemies.Add(otherCollider, other);
        Physics.SyncTransforms(); Invoke(laser, "FixedUpdate"); float afterHit = other.hp; Require(afterHit < 1000f && enemy.hp == 1000f, "beam hits intervening enemy but excludes target");
        MyTime.time = 10.1f; Invoke(laser, "FixedUpdate"); Require(other.hp == afterHit, "beam hit cooldown");
        MyTime.time = 10.6f; Invoke(laser, "FixedUpdate"); Require(other.hp < afterHit, "beam damage resumes after cooldown");
        laser.whipSegments = 4;
        typeof(LaserBeamAttack).GetField("whipAnimationTime", P).SetValue(laser, .1f);
        Invoke(laser, "RenderLaser");
        var firstBuffer = Field<Vector3[]>(laser, "whipPoints");
        Require(firstBuffer.Length == 5 && laser.linerenderer.positionCount == 5, "whip uses requested segment count");
        Require(Vector3.Distance(firstBuffer[1], Vector3.Lerp(firstBuffer[0], firstBuffer[4], .25f)) > .001f,
            "whip bends away from a straight segment");
        for (int i = 0; i < firstBuffer.Length; i++)
            Require(Vector3.Distance(firstBuffer[i], laser.linerenderer.GetPosition(i)) < .0001f, "whip renderer receives every point");
        Invoke(laser, "RenderLaser");
        Require(ReferenceEquals(firstBuffer, Field<Vector3[]>(laser, "whipPoints")), "whip reuses buffer for the same segment count");
        laser.whipSegments = 7; Invoke(laser, "RenderLaser");
        Require(Field<Vector3[]>(laser, "whipPoints").Length == 8 && laser.linerenderer.positionCount == 8, "whip buffer resizes when segment count changes");
        typeof(LaserBeamAttack).GetField("whipAnimationTime", P).SetValue(laser, laser.animateWhipTime); Invoke(laser, "RenderLaser");
        Require(laser.linerenderer.positionCount == 2 && Vector3.Distance(laser.linerenderer.GetPosition(0), player.playerRenderer.hips.position) < .001f && Vector3.Distance(laser.linerenderer.GetPosition(1), laser.laserEnd.transform.position) < .001f, "straight beam renders between effects");
        otherCollider.enabled = false;
        enemy.debuffsToAdd.Clear(); typeof(Enemy).GetMethod("AddDebuffImplementation", P).Invoke(enemy, new object[] { pending });
        enemy.DamageExternal(new DamageContainer(0f, "test") { enemy = enemy, damage = 20f }); Require(enemy.hp == 1000f, "active echo stores damage");
        MyTime.time = 14f; Invoke(laser, "FixedUpdate"); Require(Field<bool>(laser, "isShooting"), "exact stop boundary remains active");
        MyTime.time = 14.01f; Invoke(laser, "FixedUpdate");
        Require(!Field<bool>(laser, "isShooting") && !enemy.HasDebuff(EDebuff.Echo) && Mathf.Abs(enemy.hp - 880f) < .001f && laser.explosionFx.activeSelf, "stop removes echo and delivers original final burst");
        collider.enabled = false; Physics.SyncTransforms(); MyTime.time = 20f; Invoke(laser, "StartLaser");
        Require(!Field<bool>(laser, "isShooting") && Mathf.Abs(Field<float>(laser, "laserReadyTime") - 16.5f) < .001f, "no target delays next attempt");
        typeof(LaserBeamAttack).GetField("isShooting", P).SetValue(laser, true);
        laser.linerenderer.enabled = true; laser.laserStart.SetActive(true); laser.laserEnd.SetActive(true);
        Invoke(laser, "RenderLaser");
        Require(!Field<bool>(laser, "isShooting") && !laser.linerenderer.enabled && !laser.laserStart.activeSelf && !laser.laserEnd.activeSelf, "lost target stops and hides beam");
        Debug.Log("LaserLifecyclePortCheck passed: original Space Noodle prefab target, echo, timing and final burst");
    }
}
