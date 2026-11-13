using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class RollerbladesPortCheck
{
    static void Near(float actual, float expected, string message) { if (Mathf.Abs(actual - expected) > .0001f) throw new Exception(message + ": " + actual + " expected " + expected); }
    public static void Run()
    {
        var root = new GameObject("rollerblades check"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 };
        inventory.playerStats = new PlayerStatsNew(inventory, true);
        player.playerMovement = root.AddComponent<PlayerMovement>(); var body = root.AddComponent<Rigidbody>();
        typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player.playerMovement, body);
        typeof(MyPlayer).GetProperty("baseMovementSpeed").GetSetMethod(true).Invoke(player, new object[] { 10f });
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Turbo Skates.asset");
        if (asset == null || asset.eItem != EItem.Rollerblades) throw new Exception("original Turbo Skates asset");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data)).Add(asset.eItem, asset);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = asset.GetDummyItem().GetDescription(asset.localizedDescription);
            if (!description.Contains("40%") || description.Contains("{")) throw new Exception("localized Turbo Skates description: " + description);
            inventory.itemInventory.AddItem(EItem.Rollerblades, 2); var item = inventory.itemInventory.GetItem(EItem.Rollerblades);
            body.velocity = new Vector3(10f, 20f, 0); MyTime.time = 10f; inventory.itemInventory.Tick();
            Func<float> bonus = () => item.statModifiers[EStat.AttackSpeed].GetModifiers().Single().modification;
            Near(bonus(), 0f, "base horizontal speed and vertical exclusion");
            inventory.playerStats.Tick(); float baseAttackSpeed = inventory.playerStats.GetStat(EStat.AttackSpeed);
            body.velocity = new Vector3(13f, 0, 0); MyTime.time = 10.2f; inventory.itemInventory.Tick(); Near(bonus(), 0f, "quarter-second refresh delay");
            MyTime.time = 10.25f; inventory.itemInventory.Tick(); Near(bonus(), .6f, "two stacks at 130 percent speed");
            inventory.playerStats.Tick(); Near(inventory.playerStats.GetStat(EStat.AttackSpeed) - baseAttackSpeed, .6f, "movement bonus reaches player stats");
            body.velocity = new Vector3(25f, 0, 0); MyTime.time = 10.5f; inventory.itemInventory.Tick(); Near(bonus(), 1.6f, "two-stack cap");
            inventory.itemInventory.RemoveItem(EItem.Rollerblades); MyTime.time = 10.75f; inventory.itemInventory.Tick(); Near(bonus(), .4f, "cap and bonus recalculate after removal");
            Debug.Log("RollerbladesPortCheck passed: original description, movement scaling, timing, cap and stack removal");
        }
        finally { inventory.itemInventory.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
