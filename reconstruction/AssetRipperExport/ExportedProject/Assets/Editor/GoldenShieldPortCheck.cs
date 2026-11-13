using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class GoldenShieldPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("GoldenShieldPortCheck: " + reason); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldPool = PoolManager.Instance; float oldTime = MyTime.time;
        var root = new GameObject("golden shield test"); root.SetActive(false); var drops = new List<GameObject>(); PlayerInventory inv = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.playerXp = new PlayerXp { level = 50 }; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f;
            inv.playerStats.stats[EStat.MaxHealth] = 100f; inv.playerStats.stats[EStat.Shield] = 0f; inv.playerStats.stats[EStat.Armor] = 0f; inv.playerStats.stats[EStat.DamageReductionMultiplier] = 0f;
            inv.playerHealth = new PlayerHealth(inv.playerStats);
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/MoneyFlying.prefab"); Require(prefab != null && prefab.GetComponent<MoneyFlying>() != null, "original money prefab");
            pool.goldPool = new ObjectPool<GameObject>(() => { var obj = UnityEngine.Object.Instantiate(prefab); drops.Add(obj); return obj; }, null, obj => obj.SetActive(false), null, true, 10, 30);
            inv.itemInventory.AddItem(EItem.GoldenShield, 2); var item = (ItemGoldenShield)inv.itemInventory.GetItem(EItem.GoldenShield);
            Require((int)typeof(ItemGoldenShield).GetMethod("GetGold", Private).Invoke(item, null) == 32, "two shields at level fifty yield 32 gold");
            int Value() => drops.Where(x => x.activeSelf).Sum(x => x.GetComponent<MoneyFlying>().value);
            void Hit(string source) => inv.playerHealth.DamagePlayerExternal(1, 0, Vector3.zero, true, source, DcFlags.BypassAll);
            MyTime.time = 10f; Hit("fixture"); Require(Value() == 32, "real hit spawns 32 gold in original coin objects");
            MyTime.time = 10.09f; Hit("fixture"); Require(Value() == 32, "normal cooldown blocks hit");
            MyTime.time = 10.1f; Hit("fixture"); Require(Value() == 64, "normal cooldown boundary triggers");
            MyTime.time = 10.21f; Hit(PlayerHealth.healthRegenDamageSource); Require(Value() == 67, "self damage spawns reduced rounded gold");
            MyTime.time = 10.36f; Hit(PlayerHealth.healthRegenDamageSource); Require(Value() == 67, "separate self damage cooldown blocks hit");
            MyTime.time = 10.42f; Hit(PlayerHealth.healthRegenDamageSource); Require(Value() == 70, "self damage cooldown expiry triggers");
            inv.itemInventory.RemoveItem(EItem.GoldenShield); inv.itemInventory.RemoveItem(EItem.GoldenShield);
            MyTime.time = 11f; Hit("fixture"); Require(Value() == 70, "removing last shield stops drops");
            Debug.Log("GoldenShieldPortCheck passed: real damage, original money drops, cooldowns and cleanup");
        }
        finally
        {
            inv?.playerHealth?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            foreach (var obj in drops) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; PoolManager.Instance = oldPool; MyTime.time = oldTime;
        }
    }
}
