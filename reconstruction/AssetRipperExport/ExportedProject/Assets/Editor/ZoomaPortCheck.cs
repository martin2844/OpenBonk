using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class ZoomaPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ZoomaPortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var obj = new GameObject(name); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<SphereCollider>(); var enemy = obj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = obj.AddComponent<Rigidbody>(); enemy.enemyMovement = obj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        enemy.renderer = obj.AddComponent<MeshRenderer>(); manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var root = new GameObject("Zap test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; var effect = root.AddComponent<EffectManager>(); EffectManager.Instance = effect;
        effect.zapEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ZapLightningFx.prefab"); Require(effect.zapEffect != null, "original effect prefab");
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 10 };
        var passive = new PassiveAbilityZooma(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var hit = Target(manager, "hit", Vector3.zero); var near = Target(manager, "near", Vector3.right * 3f); var far = Target(manager, "far", Vector3.right * 8f);
        int zaps = 0; Action<Enemy, DamageContainer> observer = (e, dc) => { if (dc.element == EElement.Lightning) zaps++; };
        try
        {
            passive.Init(); Enemy.A_Damage += observer; PlayerXp.A_LevelUp?.Invoke(10);
            Require(Mathf.Abs(passive.statModifiers[EStat.PickupRange].GetModifiers().Single().modification - .2f) < .0001f, "level pickup range");
            MyTime.time = 10f; passive.Tick(); player.transform.position = Vector3.right * 100f;
            MyTime.time = 10.25f; passive.Tick(); Require(passive.GetProgress() == 0f, "charge check interval");
            MyTime.time = 10.5f; passive.Tick(); Require(Mathf.Abs(passive.GetProgress() - .6f) < .0001f, "distance charging");
            player.transform.position = Vector3.right * 200f; MyTime.time = 11f; passive.Tick(); Require(passive.GetProgress() == 1f, "charge caps at one");
            Physics.SyncTransforms(); hit.DamageFromPlayerOther(new DamageContainer(0f, "test") { enemy = hit, damage = 1f });
            Require(passive.GetProgress() == 0f && hit.hp < 999f && near.hp < 1000f && far.hp == 1000f, "real hit discharges into nearby enemies only");
            Require(zaps == 2, "one lightning damage event per nearby enemy without recursion");
            var visual = GameObject.Find(effect.zapEffect.name + "(Clone)"); Require(visual != null && visual.transform.position == hit.GetCenterPosition(), "original effect appears at struck enemy");
            UnityEngine.Object.DestroyImmediate(visual); passive.Cleanup(); PlayerXp.A_LevelUp?.Invoke(20);
            Require(Mathf.Abs(passive.statModifiers[EStat.PickupRange].GetModifiers().Single().modification - .2f) < .0001f, "cleanup removes level handler");
            Debug.Log("ZoomaPortCheck passed: movement charging, real hit, area lightning, effect and cleanup");
        }
        finally { Enemy.A_Damage -= observer; passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(hit.enemyData); UnityEngine.Object.DestroyImmediate(near.enemyData); UnityEngine.Object.DestroyImmediate(far.enemyData); UnityEngine.Object.DestroyImmediate(hit.gameObject); UnityEngine.Object.DestroyImmediate(near.gameObject); UnityEngine.Object.DestroyImmediate(far.gameObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; EffectManager.Instance = null; }
    }
}
