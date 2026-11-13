using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class ElectricPlugPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool ok, string text) { if (!ok) throw new Exception("ElectricPlugPortCheck: " + text); }
    static Enemy Target(EnemyManager manager, Vector3 pos) {
        var go = new GameObject("plug target"); go.layer = LayerMask.NameToLayer("Enemy"); go.transform.position = pos;
        var col = go.AddComponent<SphereCollider>(); var enemy = go.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.renderer = go.AddComponent<MeshRenderer>(); manager.collidersToEnemies.Add(col, enemy); return enemy;
    }
    public static void Run() {
        var root = new GameObject("plug fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; typeof(MyPlayer).GetField("baseDamage", P).SetValue(player, 10f);
        GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.SizeMultiplier] = 1, [EStat.DamageMultiplier] = 2, [EStat.KnockbackMultiplier] = 0, [EStat.CritChance] = 0 }; inventory.playerStats = stats;
        var targets = new List<Enemy>(); for (int i = 0; i < 10; i++) targets.Add(Target(enemies, new Vector3(2+i,0,1)));
        var outside = Target(enemies, new Vector3(30,0,0)); var plug = new ItemElectricPlug(inventory.itemInventory); int hits = 0;
        Action<Enemy,DamageContainer> observe = (enemy, dc) => { hits++; Require(dc.element == EElement.Lightning && dc.procCoefficient == .5f && dc.damageSource == "ElectricPlug", "lightning damage metadata"); }; Enemy.A_Damage += observe;
        try {
            plug.Init(); plug.AddAmount(); Require((float)typeof(ItemElectricPlug).GetField("radius", P).GetValue(plug) == 16 && (int)typeof(ItemElectricPlug).GetField("targets", P).GetValue(plug) == 6, "first stack range and target count");
            MyTime.time = 10; Physics.SyncTransforms();
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 0;
            typeof(PlayerHealth).GetMethod("Damage", P).Invoke(health, new object[] { new DamageContainer(0, "test") { damage = 5 }, false });
            Require(hits == 6, "damage call triggers six targets even when player cannot take damage");
            int damaged = 0; foreach (var e in targets) if (e.hp == 984) damaged++; Require(damaged == 6 && outside.hp == 1000, "scaled damage and range");
            plug.AddAmount(); Require((int)typeof(ItemElectricPlug).GetField("targets", P).GetValue(plug) == 10, "second stack targets");
            PlayerHealth.A_DamagePlayerCalled?.Invoke(); Require(hits == 16, "second stack hits ten targets");
            plug.Cleanup(); PlayerHealth.A_DamagePlayerCalled?.Invoke(); Require(hits == 16, "cleanup unsubscribes");
            var effectManager = root.AddComponent<EffectManager>(); effectManager.electricPlugFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ElectricalPlugEffect.prefab"); Require(effectManager.electricPlugFx != null, "original effect prefab");
            effectManager.ElectricalPlugEffect(); var fx = (EffectPlayer)typeof(EffectManager).GetField("electricPlugEffect", P).GetValue(effectManager);
            Require(fx != null && fx.transform.parent == player.transform && fx.transform.localPosition == Vector3.zero, "effect attaches to player");
            effectManager.ElectricalPlugEffect(); Require((EffectPlayer)typeof(EffectManager).GetField("electricPlugEffect", P).GetValue(effectManager) == fx, "effect reused");
            UnityEngine.Object.DestroyImmediate(fx.gameObject);
            Debug.Log("ElectricPlugPortCheck passed: damage event, target count, scaling, lightning metadata and effect reuse");
        } finally { plug.Cleanup(); Enemy.A_Damage -= observe; foreach (var e in targets) { UnityEngine.Object.DestroyImmediate(e.enemyData); UnityEngine.Object.DestroyImmediate(e.gameObject); } UnityEngine.Object.DestroyImmediate(outside.enemyData); UnityEngine.Object.DestroyImmediate(outside.gameObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; }
    }
}
