using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class CowardsCloakPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("CowardsCloakPortCheck: " + message); }
    static void Near(float a, float b, string message) => Require(Mathf.Abs(a - b) < .0001f, message + ": " + a + " expected " + b);
    public static void Run()
    {
        var root = new GameObject("cloak test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.Armor] = 0; stats.stats[EStat.DamageReductionMultiplier] = 0; inv.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 200; inv.playerHealth = health;
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Cowards Cloak.asset"); Require(asset != null && asset.eItem == EItem.CowardsCloak, "original cloak asset");
        ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string desc = asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(desc.Contains("+5%") && !desc.Contains("{"), "localized description");
            inv.itemInventory.AddItem(EItem.CowardsCloak, 2); var cloak = inv.itemInventory.GetItem(EItem.CowardsCloak);
            Func<EStatModifyType, float> modifier = t => cloak.statModifiers[EStat.MoveSpeedMultiplier].GetModifiers().Single(m => m.modifyType == t).modification;
            Near(modifier(EStatModifyType.Flat), .1f, "permanent bonus for two items"); Near(modifier(EStatModifyType.Multiplication), 1f, "no temporary stacks initially");
            MyTime.time = 10f; PlayerHealth.A_TakeDamage?.Invoke(health, new DamageContainer(0f, "test") { damage = 0f }, false); Near(modifier(EStatModifyType.Multiplication), 1f, "zero damage ignored");
            health.DamagePlayerExternal(10, 0, Vector3.zero, damageSource: "test", flags: DcFlags.BypassEvade); Near(modifier(EStatModifyType.Multiplication), 1.3f, "actual damage adds first stack");
            MyTime.time = 10.5f; health.DamagePlayerExternal(10, 0, Vector3.zero, damageSource: "test", flags: DcFlags.BypassEvade); Near(modifier(EStatModifyType.Multiplication), 1.6f, "second damage adds stack");
            MyTime.time = 11f; health.DamagePlayerExternal(10, 0, Vector3.zero, damageSource: "test", flags: DcFlags.BypassEvade); Near(modifier(EStatModifyType.Multiplication), 1.6f, "stacks capped at two");
            MyTime.time = 14f; inv.itemInventory.Tick(); Near(modifier(EStatModifyType.Multiplication), 1.6f, "stack lasts at expiry boundary");
            MyTime.time = 14.01f; inv.itemInventory.Tick(); Near(modifier(EStatModifyType.Multiplication), 1f, "stack expires after refreshed duration");
            inv.itemInventory.RemoveItem(EItem.CowardsCloak); Near(modifier(EStatModifyType.Flat), .05f, "permanent bonus updates on removal");
            inv.itemInventory.RemoveItem(EItem.CowardsCloak); PlayerHealth.A_TakeDamage?.Invoke(health, new DamageContainer(0f, "test") { damage = 1f }, false); Near(modifier(EStatModifyType.Multiplication), 1f, "last removal unsubscribes");
            Debug.Log("CowardsCloakPortCheck passed: original asset, speed bonuses, actual damage, cap, expiry and cleanup");
        }
        finally { inv.itemInventory.Cleanup(); inv.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
