using System;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class PowerupCombatStatsPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("PowerupCombatStatsPortCheck: " + message); }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;
    public static void Run()
    {
        var root = new GameObject("powerup combat test"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; float oldTime = MyTime.time;
        PlayerInventory inv = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
            inv.tomeInventory = new TomeInventory(); inv.itemInventory = new ItemInventory(); inv.statusEffects = new PlayerStatusEffects(); inv.statInventory = new StatInventory(); inv.playerXp = new PlayerXp { level = 1 };
            inv.playerStats = new PlayerStatsNew(inv, true); inv.playerHealth = new PlayerHealth(inv.playerStats);
            float damageBefore = inv.playerStats.GetStat(EStat.DamageMultiplier), speedBefore = inv.playerStats.GetStat(EStat.AttackSpeed);
            int hpBefore = inv.playerHealth.hp;
            MyTime.time = 10f; inv.statusEffects.TestPickup(EPickup.Shield); inv.statusEffects.TestPickup(EPickup.Rage); inv.playerStats.Tick();
            Check(Near(inv.playerStats.GetStat(EStat.DamageReductionMultiplier), 1f), "shield reaches damage reduction stat");
            Check(Near(inv.playerStats.GetStat(EStat.DamageMultiplier), damageBefore * 1.5f) && Near(inv.playerStats.GetStat(EStat.AttackSpeed), speedBefore * 2f), "rage reaches damage and attack speed stats");
            inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.zero, false, "fixture", DcFlags.BypassAll);
            Check(inv.playerHealth.hp == hpBefore, "shield prevents incoming damage");
            MyTime.time = 25f; inv.statusEffects.Tick(); inv.playerStats.Tick();
            Check(Near(inv.playerStats.GetStat(EStat.DamageReductionMultiplier), 0f) && Near(inv.playerStats.GetStat(EStat.DamageMultiplier), damageBefore) && Near(inv.playerStats.GetStat(EStat.AttackSpeed), speedBefore), "expired effects restore stats");
            inv.playerHealth.DamagePlayerExternal(10f, 0f, Vector3.zero, false, "fixture", DcFlags.BypassAll);
            Check(inv.playerHealth.hp == hpBefore - 10, "damage resumes after expiration");
            Debug.Log("PowerupCombatStatsPortCheck passed: shield mitigation, rage stats and expiry");
        }
        finally
        {
            inv?.playerHealth?.OnDestroy(); inv?.playerStats?.OnDestroy(); inv?.statusEffects?.OnDestroy(); inv?.itemInventory?.Cleanup();
            MyTime.time = oldTime; MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
