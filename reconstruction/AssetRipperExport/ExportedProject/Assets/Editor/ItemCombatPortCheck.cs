using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class ItemCombatPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("ItemCombatPortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0001f, message + ": " + actual + " expected " + expected);
    public static void Run()
    {
        var root = new GameObject("item combat test"); root.SetActive(false); var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var items = (Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
        var assets = new[] { "Tactical Glasses", "Boss Buster", "Turbo Socks", "Ice Cube" };
        var loaded = new List<ItemData>();
        foreach (string name in assets) { var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/" + name + ".asset"); Require(item != null, name + " asset"); loaded.Add(item); items[item.eItem] = item; }
        var inventory = new ItemInventory(); var enemy = root.AddComponent<Enemy>(); enemy.maxHp = 100f; enemy.hp = 90f; var dc = new DamageContainer(1f, "test") { enemy = enemy }; var modifiers = new StatComponents();
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string[] values = { "20%", "15%", "15%", "20%" };
            for (int i = 0; i < loaded.Count; i++)
            {
                string text = loaded[i].GetDummyItem().GetDescription(loaded[i].localizedDescription);
                Require(text != null && text.Contains(values[i]) && !text.Contains("{"), assets[i] + " description: " + text);
            }
            inventory.AddItem(EItem.TacticalGlasses, 2); inventory.AddItem(EItem.BossBuster, 2);
            inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, .4f, "90% health tactical bonus");
            enemy.hp = 89f; inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, 0f, "below 90% without boss flag");
            typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, EEnemyFlag.Elite);
            inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, .3f, "elite boss buster bonus");
            enemy.hp = 100f; inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, .7f, "bonuses combine");
            typeof(Enemy).GetField("enemyFlag", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, EEnemyFlag.Boss);
            inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, .7f, "boss bonus");
            dc.enemy = null; inventory.PreAttack(dc, modifiers); Near(modifiers.additiveValue, 0f, "no target no bonus");
            Debug.Log("ItemCombatPortCheck passed: original descriptions, stacked tactical and boss pre-attack bonuses");
        }
        finally { inventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); DataManager.Instance = null; }
    }
}
