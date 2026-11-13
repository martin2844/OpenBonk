using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class ZaWarudoPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ZaWarudoPortCheck: " + message); }
    public static void Run() {
        var root = new GameObject("revival fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.Armor] = 0, [EStat.DamageReductionMultiplier] = 0 }; inventory.playerStats = stats;
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = health.maxHp = 100; inventory.playerHealth = health;
        int deaths = 0, taken = 0, removed = 0; Action onDeath = () => deaths++; Action<PlayerHealth,DamageContainer,bool> onDamage = (_,__,___) => taken++; Action<EItem,bool> onRemove = (item,_) => { if (item == EItem.ZaWarudo) removed++; };
        PlayerHealth.A_Died += onDeath; PlayerHealth.A_TakeDamage += onDamage; ItemInventory.A_ItemRemoved += onRemove;
        try {
            inventory.itemInventory.AddItem(EItem.ZaWarudo, 2); MyTime.time = 100;
            health.shield = 5; health.DamagePlayerExternal(20, 0, Vector3.zero, false, "test", DcFlags.BypassEvade);
            Require(health.hp == 100 && health.shield == 0 && inventory.itemInventory.GetAmount(EItem.ZaWarudo) == 2, "shield hit does not consume charge");
            health.overheal = 10; health.maxOverheal = 10; health.DamagePlayerExternal(20, 0, Vector3.zero, false, "test", DcFlags.BypassEvade);
            Require(health.hp == 90 && health.overheal == 0 && inventory.itemInventory.GetAmount(EItem.ZaWarudo) == 2, "nonlethal hit respects overheal");
            health.DamagePlayerExternal(200, 0, Vector3.zero, false, "test", DcFlags.BypassEvade);
            Require(health.hp == 100 && deaths == 0 && removed == 1 && inventory.itemInventory.GetAmount(EItem.ZaWarudo) == 1, "lethal damage restores health and consumes one");
            Require(inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze) && inventory.statusEffects.statusEffects[EStatusEffect.TimeFreeze].expirationTime == 115f, "fifteen second freeze");
            health.DamagePlayerExternal(200, 0, Vector3.zero, false, "test", DcFlags.BypassEvade); Require(health.hp == 100 && removed == 1, "freeze stops subsequent damage");
            MyTime.time = 115; inventory.statusEffects.Tick(); Require(!inventory.statusEffects.HasStatusEffect(EStatusEffect.TimeFreeze), "freeze expires");
            health.DamagePlayerExternal(200, 0, Vector3.zero, false, "test", DcFlags.BypassEvade); Require(health.hp == 100 && removed == 2 && deaths == 0, "second charge can revive");
            MyTime.time = 130; inventory.statusEffects.Tick(); health.DamagePlayerExternal(200, 0, Vector3.zero, false, "test", DcFlags.BypassEvade);
            Require(health.hp == 0 && deaths == 1 && taken == 5, "without charges death and damage event occur");
            Debug.Log("ZaWarudoPortCheck passed: shield, overheal, lethal revival, freeze, stacking and final death");
        } finally { PlayerHealth.A_Died -= onDeath; PlayerHealth.A_TakeDamage -= onDamage; ItemInventory.A_ItemRemoved -= onRemove; inventory.itemInventory.Cleanup(); inventory.statusEffects.OnDestroy(); MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
