using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class MirrorPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("MirrorPortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 pos) {
        var go = new GameObject(name); go.layer = LayerMask.NameToLayer("Enemy"); go.transform.position = pos;
        var col = go.AddComponent<SphereCollider>(); var enemy = go.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.renderer = go.AddComponent<MeshRenderer>(); manager.collidersToEnemies.Add(col, enemy); return enemy;
    }
    public static void Run() {
        var root = new GameObject("mirror fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.SizeMultiplier] = 1, [EStat.DamageMultiplier] = 2, [EStat.KnockbackMultiplier] = 0 }; inventory.playerStats = stats;
        var attacker = Target(manager, "attacker", Vector3.right * 3); var near = Target(manager, "near", new Vector3(4,0,1)); var behind = Target(manager, "behind", Vector3.left * 3);
        var mirror = new ItemMirror(inventory.itemInventory); int ready = 0, unready = 0; Action<bool> observer = value => { if (value) ready++; else unready++; }; ItemMirror.A_MirrorReady += observer;
        try {
            MyTime.time = 0; mirror.Init(); mirror.AddAmount(); Require(mirror.cooldown == 7f && ready == 1, "first stack and readiness notification");
            mirror.Tick(); MyTime.time = 7f; mirror.Tick();
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
            var incoming = new DamageContainer(1, "test") { enemy = attacker, damage = 20, flags = DcFlags.BypassEvade }; Require(!health.CheckStopDamage(incoming, false), "not blocked before cooldown");
            Require(incoming.damage == 20, "strict cooldown boundary");
            MyTime.time = 7.01f; mirror.Tick(); Require(ready == 2, "ready after cooldown");
            var noSource = new DamageContainer(1, "test") { damage = 20 }; PlayerHealth.A_CheckStopDamage?.Invoke(noSource, false); Require(noSource.damage == 20, "source-less damage does not consume mirror");
            Physics.SyncTransforms(); Require(health.CheckStopDamage(incoming, false), "health pipeline reports reflected damage stopped");
            Require(incoming.damage == 0 && attacker.hp == 950f && near.hp == 950f && behind.hp == 1000f, "normal damage blocked and cone reflects scaled damage");
            Require(inventory.statusEffects.HasStatusEffect(EStatusEffect.Invulnerability) && unready == 1, "brief invulnerability and readiness off");
            var cooldownHit = new DamageContainer(1, "test") { enemy = attacker, damage = 20 }; PlayerHealth.A_CheckStopDamage?.Invoke(cooldownHit, false); Require(cooldownHit.damage == 20, "cannot reflect during cooldown");
            MyTime.time = 14.02f; mirror.Tick(); var bossHit = new DamageContainer(1, "test") { enemy = attacker, damage = 20, flags = DcFlags.BossDamage }; PlayerHealth.A_CheckStopDamage?.Invoke(bossHit, false);
            Require(bossHit.damage == 5 && attacker.hp == 900f, "boss damage reduced to quarter and reflected");
            mirror.Cleanup(); var after = new DamageContainer(1, "test") { enemy = attacker, damage = 10 }; PlayerHealth.A_CheckStopDamage?.Invoke(after, false); Require(after.damage == 10, "cleanup unsubscribes");
            var effectManager = root.AddComponent<EffectManager>(); effectManager.mirrorFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MirrorEffect.prefab"); Require(effectManager.mirrorFx != null, "original mirror prefab");
            effectManager.SpawnMirrorFx(Vector3.right, Vector3.forward, 5f);
            var effect = (EffectPlayer)typeof(EffectManager).GetField("activeMirrorFx", P).GetValue(effectManager);
            Require(effect != null && effect.transform.position == Vector3.right && effect.transform.localScale == Vector3.one * 5, "effect placement and scale");
            effectManager.SpawnMirrorFx(Vector3.left, Vector3.right, 2f);
            Require((EffectPlayer)typeof(EffectManager).GetField("activeMirrorFx", P).GetValue(effectManager) == effect && effect.transform.position == Vector3.left && effect.transform.localScale == Vector3.one * 2, "effect reuse");
            UnityEngine.Object.DestroyImmediate(effect.gameObject);
            Debug.Log("MirrorPortCheck passed: cooldown, cone, reflected damage, boss reduction, invulnerability, cleanup and original effect reuse");
        } finally { mirror.Cleanup(); ItemMirror.A_MirrorReady -= observer; inventory.statusEffects.OnDestroy(); foreach (var e in new[] {attacker, near, behind}) { UnityEngine.Object.DestroyImmediate(e.enemyData); UnityEngine.Object.DestroyImmediate(e.gameObject); } UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; }
    }
}
