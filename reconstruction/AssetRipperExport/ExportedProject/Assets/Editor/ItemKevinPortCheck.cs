using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public static class ItemKevinPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var playerObject = new GameObject("kevin test player"); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var managerObject = new GameObject("game manager"); GameManager.Instance = managerObject.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        player.inventory.statusEffects = new PlayerStatusEffects();
        player.inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        player.inventory.playerStats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) player.inventory.playerStats.stats[stat] = 0f;
        var health = player.inventory.playerHealth; health.hp = health.maxHp = 10; health.shield = 5;
        var items = new ItemInventory(); player.inventory.itemInventory = items; items.AddItem(EItem.Kevin, 4);
        var item = (ItemKevin)items.GetItem(EItem.Kevin); Check(item != null && item.amount == 4, "Kevin is created through inventory factory");
        int events = 0, punchTotal = 0; Action<int> onPunch = n => { events++; punchTotal += n; }; ItemKevin.A_PunchedByKevin += onPunch;
        string source = null; Action<PlayerHealth, DamageContainer, bool> onDamage = (h, dc, shielded) => { source = dc.damageSource; Check(!shielded, "Kevin damage ignores shield"); }; PlayerHealth.A_TakeDamage += onDamage;
        Enemy.A_Damage?.Invoke(null, null); Enemy.A_Damage?.Invoke(null, null); Enemy.A_Damage?.Invoke(null, null); items.Tick();
        Check(health.hp == 7 && health.shield == 5 && source == "Kevin", "three hits at four stacks cause three self damage");
        Check(events == 1 && punchTotal == 1, "original batched punch event");
        items.Tick(); Check(events == 2 && punchTotal == 1 && health.hp == 7, "empty tick reports zero without damage");
        items.AddItem(EItem.Kevin, 4);
        for (int i = 0; i < 5; i++) Enemy.A_Damage?.Invoke(null, null); items.Tick();
        Check(health.hp == 1 && punchTotal == 2, "damage from multiple stacks is capped at one health");
        Enemy.A_Damage?.Invoke(null, null); items.Tick(); Check(health.hp == 1 && events == 3, "no hits or events accumulated at one health");
        for (int i = 0; i < 8; i++) items.RemoveItem(EItem.Kevin); health.hp = 5; Enemy.A_Damage?.Invoke(null, null); items.Tick(); Check(health.hp == 5 && punchTotal == 2, "removal detaches enemy event");
        items.Cleanup();
        PlayerHealth.A_TakeDamage -= onDamage; ItemKevin.A_PunchedByKevin -= onPunch; player.inventory.statusEffects.OnDestroy(); MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(managerObject);
        Debug.Log("ItemKevinPortCheck passed: real self-damage path, stacks, shield bypass, health cap, batch event and cleanup");
    }
}
