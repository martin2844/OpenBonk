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

public static class PhantomShroudPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("PhantomShroudPortCheck: " + message); }
    static void Near(float a, float b, string message) => Require(Mathf.Abs(a - b) < .0001f, message + ": " + a + " expected " + b);
    public static void Run()
    {
        var root = new GameObject("phantom shroud test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
        var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
        var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/PhantomShroud.asset"); Require(asset != null && asset.eItem == EItem.PhantomShroud, "original item asset");
        ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string desc = asset.GetDummyItem().GetDescription(asset.localizedDescription); Require(desc.Contains("2x") && !desc.Contains("{"), "localized description arguments");
            inv.itemInventory.AddItem(EItem.PhantomShroud, 2); var item = inv.itemInventory.GetItem(EItem.PhantomShroud);
            Func<EStat, float> modifier = stat => item.statModifiers[stat].GetModifiers().Single().modification;
            Near(modifier(EStat.Evasion), .1f, "two-item evasion");
            var attack = new StatComponents(); attack.Recycle(); inv.itemInventory.PreAttack(new DamageContainer(0f, "test"), attack); Near(attack.multiplicativeValue, 1f, "no evade means no attack bonus");
            MyTime.time = 10f; PlayerHealth.A_Evaded?.Invoke(null);
            Near(modifier(EStat.MoveSpeedMultiplier), .65f, "movement bonus"); Near(modifier(EStat.DamageMultiplier), .5f, "first evade damage stack"); Near(modifier(EStat.AttackSpeed), .25f, "first evade speed stack");
            inv.itemInventory.PreAttack(new DamageContainer(0f, "test"), attack); Near(attack.multiplicativeValue, 2.5f, "one-shot next-attack bonus");
            inv.itemInventory.PreAttack(new DamageContainer(0f, "test"), attack); Near(attack.multiplicativeValue, 1f, "next-attack bonus consumed");
            for (int i = 0; i < 12; i++) PlayerHealth.A_Evaded?.Invoke(null);
            Near(modifier(EStat.DamageMultiplier), 4f, "eight-stack damage cap"); Near(modifier(EStat.AttackSpeed), 2f, "eight-stack speed cap");
            MyTime.time = 13.5f; inv.itemInventory.Tick(); Near(modifier(EStat.DamageMultiplier), 4f, "buff active at boundary");
            MyTime.time = 13.501f; inv.itemInventory.Tick(); Near(modifier(EStat.DamageMultiplier), 0f, "damage expires after boundary"); Near(modifier(EStat.AttackSpeed), 0f, "speed expires"); Near(modifier(EStat.MoveSpeedMultiplier), 0f, "movement expires");
            inv.itemInventory.PreAttack(new DamageContainer(0f, "test"), attack); Near(attack.multiplicativeValue, 2.5f, "unused next-attack bonus persists after timed buffs");
            inv.itemInventory.RemoveItem(EItem.PhantomShroud); inv.itemInventory.RemoveItem(EItem.PhantomShroud); PlayerHealth.A_Evaded?.Invoke(null);
            Near(modifier(EStat.DamageMultiplier), 0f, "removed item unsubscribed");
            Debug.Log("PhantomShroudPortCheck passed: item acquisition, evasion, attack proc, cap, timeout and cleanup");
        }
        finally { inv.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; DataManager.Instance = null; }
    }
}
