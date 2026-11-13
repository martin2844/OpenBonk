using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Extra;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class BullseyePortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("BullseyePortCheck: " + message); }
    static Enemy Target(EnemyManager manager, string name, Vector3 position)
    {
        var obj = new GameObject(name); obj.layer = LayerMask.NameToLayer("Enemy"); obj.transform.position = position;
        var collider = obj.AddComponent<SphereCollider>(); var enemy = obj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = obj.AddComponent<Rigidbody>(); enemy.enemyMovement = obj.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.rb = enemy.rb; enemy.enemyMovement.enemy = enemy;
        enemy.renderer = obj.AddComponent<MeshRenderer>(); manager.collidersToEnemies.Add(collider, enemy); return enemy;
    }
    public static void Run()
    {
        var root = new GameObject("Bullseye test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        pool.bullseye = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Bullseye.prefab"); pool.explosionFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/RocketExplosion.prefab");
        Require(pool.bullseye != null && pool.explosionFx != null, "original marker and explosion prefabs"); typeof(PoolManager).GetMethod("Start", P).Invoke(pool, null);
        var audio = root.AddComponent<AudioManager>(); audio.xp = root.AddComponent<AudioSource>(); audio.gold = root.AddComponent<AudioSource>(); audio.xp.volume = .4f; audio.gold.volume = .6f; audio.bullseye = root.AddComponent<RandomSfx>();
        typeof(AudioManager).GetMethod("Awake", P).Invoke(audio, null);
        Require(AudioManager.Instance == audio && Mathf.Abs((float)typeof(AudioManager).GetField("baseXpVolume", P).GetValue(audio) - .4f) < .0001f && Mathf.Abs((float)typeof(AudioManager).GetField("baseGoldVolume", P).GetValue(audio) - .6f) < .0001f, "audio manager startup and source volumes");
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 20 };
        var passive = new PassiveAbilityBullseye(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var hit = Target(manager, "hit", Vector3.zero); var near = Target(manager, "near", Vector3.right * 3f); var far = Target(manager, "far", Vector3.right * 25f);
        int blasts = 0; Action<Enemy, DamageContainer> observer = (e, dc) => { if (dc.damageSource == "Bullseye") blasts++; };
        try
        {
            MyTime.time = 10f; passive.Init(); Enemy.A_Damage += observer; PlayerXp.A_LevelUp?.Invoke(20); Require(Mathf.Abs(passive.statModifiers[EStat.CritDamage].GetModifiers().Single().modification - .2f) < .0001f, "level crit damage");
            MyTime.time = 14f; Enemy.A_EnemySpawned?.Invoke(hit); Require(!PassiveAbilityBullseye.IsMarkedEnemy(hit), "initial mark cooldown");
            MyTime.time = 15f; Enemy.A_EnemySpawned?.Invoke(hit); Require(PassiveAbilityBullseye.IsMarkedEnemy(hit) && pool.bullseyePool.CountActive == 1, "spawn marks enemy with original marker");
            Physics.SyncTransforms(); hit.DamageFromPlayerOther(new DamageContainer(0f, "test") { enemy = hit, damage = 10f });
            Require(!PassiveAbilityBullseye.IsMarkedEnemy(hit) && hit.hp == 980f && near.hp == 990f && far.hp == 1000f && blasts == 2, "real hit detonates once within radius");
            var marker = GameObject.Find(pool.bullseye.name + "(Clone)"); var fx = GameObject.Find(pool.explosionFx.name + "(Clone)");
            Require(marker != null && fx != null && Mathf.Abs(fx.transform.localScale.x - 10f) < .001f, "original explosion effect and size");
            MyTime.time = 20f; Enemy.A_EnemySpawned?.Invoke(near); Require(PassiveAbilityBullseye.IsMarkedEnemy(near), "next mark");
            Enemy.A_EnemyReleasedFromPool?.Invoke(near); Require(!PassiveAbilityBullseye.IsMarkedEnemy(near), "release clears mark");
            MyTime.time = 25f; Enemy.A_EnemySpawned?.Invoke(far); Require(PassiveAbilityBullseye.IsMarkedEnemy(far), "subsequent mark");
            MyTime.time = 55f; Require(!PassiveAbilityBullseye.IsMarkedEnemy(far), "mark expires at boundary");
            GameManager.A_StageStarted?.Invoke(); Require(!PassiveAbilityBullseye.IsMarkedEnemy(far), "stage resets marks");
            passive.Cleanup(); PlayerXp.A_LevelUp?.Invoke(30); Require(Mathf.Abs(passive.statModifiers[EStat.CritDamage].GetModifiers().Single().modification - .2f) < .0001f, "cleanup removes level handler");
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<BullseyeMarker>()) UnityEngine.Object.DestroyImmediate(obj.gameObject);
            UnityEngine.Object.DestroyImmediate(fx);
            Debug.Log("BullseyePortCheck passed: original marker, real hit explosion, expiry, release and reset");
        }
        finally { Enemy.A_Damage -= observer; passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(hit.enemyData); UnityEngine.Object.DestroyImmediate(near.enemyData); UnityEngine.Object.DestroyImmediate(far.enemyData); UnityEngine.Object.DestroyImmediate(hit.gameObject); UnityEngine.Object.DestroyImmediate(near.gameObject); UnityEngine.Object.DestroyImmediate(far.gameObject); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; PoolManager.Instance = null; AudioManager.Instance = null; }
    }
}
