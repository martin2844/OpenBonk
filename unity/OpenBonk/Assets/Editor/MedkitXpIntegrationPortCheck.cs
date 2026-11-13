using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class MedkitXpIntegrationPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("MedkitXpIntegrationPortCheck: " + message); }
    static void Near(float a, float b, string message) => Check(Mathf.Abs(a - b) < .001f, message + $" actual={a} expected={b}");
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldData = DataManager.Instance;
        float oldTime = MyTime.time, oldDelta = MyTime.fixedDeltaTime; bool oldPaused = MyTime.paused;
        var root = new GameObject("medkit xp fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>(); var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        inventory.playerXp = new PlayerXp { level = 9999 }; inventory.playerStats = new PlayerStatsNew(inventory, true); inventory.playerHealth = new PlayerHealth(inventory.playerStats);
        var items = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var medkit = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Medkit.asset"); var bracelet = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/TimeBracelet.asset");
        try
        {
            Check(medkit != null && medkit.eItem == EItem.Medkit && bracelet != null && bracelet.eItem == EItem.TimeBracelet, "original item assets");
            items.Add(EItem.Medkit, medkit); items.Add(EItem.TimeBracelet, bracelet);
            XpUtility.Init(); MyTime.paused = false; MyTime.fixedDeltaTime = .02f; MyTime.time = 1f;
            inventory.playerHealth.hp = 50;
            inventory.itemInventory.AddItem(EItem.Medkit, 2); inventory.itemInventory.AddItem(EItem.TimeBracelet, 2); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetStat(EStat.HealthRegen), 100f, "two medkits regeneration per minute");
            Near(inventory.playerStats.GetStat(EStat.XpIncreaseMultiplier), 1.16f, "two bracelets XP multiplier");
            for (int i = 0; i < 150; i++) { MyTime.time += .02f; inventory.playerHealth.Tick(); }
            Check(inventory.playerHealth.hp >= 54 && inventory.playerHealth.hp <= 55, "normal health ticks heal at expected rate");
            inventory.playerXp.AddXp(25); Check(inventory.playerXp.xp == 29, "XP collection applies multiplier");
            inventory.itemInventory.RemoveItem(EItem.Medkit); inventory.itemInventory.RemoveItem(EItem.Medkit);
            inventory.itemInventory.RemoveItem(EItem.TimeBracelet); inventory.itemInventory.RemoveItem(EItem.TimeBracelet); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetStat(EStat.HealthRegen), 10f, "regeneration restored after removal");
            Near(inventory.playerStats.GetStat(EStat.XpIncreaseMultiplier), 1f, "XP multiplier restored after removal");
            int before = inventory.playerXp.xp; inventory.playerXp.AddXp(25); Check(inventory.playerXp.xp - before == 25, "base XP restored after removal");
            Debug.Log("MedkitXpIntegrationPortCheck passed: original items, health regeneration, XP gain and removal");
        }
        finally
        {
            inventory.playerHealth.OnDestroy(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup();
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; DataManager.Instance = oldData; MyTime.time = oldTime; MyTime.fixedDeltaTime = oldDelta; MyTime.paused = oldPaused;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
