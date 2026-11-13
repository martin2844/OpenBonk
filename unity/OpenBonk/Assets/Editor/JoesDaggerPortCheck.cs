using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class JoesDaggerPortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double v) { value = v; } public override double NextDouble() => value; }
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string m) { if (!b) throw new Exception("JoesDaggerPortCheck: " + m); }
    static void Near(float a, float b, string m) { Check(Mathf.Abs(a - b) < .0001f, m + ": " + a + " expected " + b); }
    public static void Run()
    {
        var oldRandom = global::Utility.MyRandom.random; var oldData = DataManager.Instance; var oldTime = MyTime.time; var oldPaused = MyTime.paused;
        var root = new GameObject("Joe dagger fixture"); root.SetActive(false); var items = new ItemInventory();
        try
        {
            MyTime.paused = false; MyTime.time = 10f; var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var asset = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Joe's Dagger.asset"); Check(asset != null && asset.eItem == EItem.JoesDagger, "original item");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(asset.eItem, asset);
            var enemy = root.AddComponent<Enemy>(); var boss = new GameObject("boss").AddComponent<Enemy>(); boss.transform.SetParent(root.transform); typeof(Enemy).GetField("enemyFlag", P).SetValue(boss, EEnemyFlag.Boss);
            items.AddItem(EItem.JoesDagger); var item = (ItemJoesDagger)items.GetItem(EItem.JoesDagger); Check(item.HasPreAttackProc(), "pre-attack registered");
            var modifiers = new StatComponents(); var hit = new DamageContainer(1f, "weapon") { enemy = enemy };
            global::Utility.MyRandom.random = new FixedRandom(0); items.PreAttack(hit, modifiers); Check(!hit.isExecute, "ineligible attack");
            hit.canProcJoe = true; global::Utility.MyRandom.random = new FixedRandom(.01); items.PreAttack(hit, modifiers); Check(!hit.isExecute, "roll at chance misses");
            global::Utility.MyRandom.random = new FixedRandom(0); items.PreAttack(hit, modifiers); Check(hit.isExecute && hit.damageSource == "JoesDagger", "successful roll marks execute");
            var next = new DamageContainer(1f, "weapon") { canProcJoe = true }; MyTime.time = 10.2f; items.PreAttack(next, modifiers); Check(!next.isExecute, "successful roll cooldown");
            MyTime.time = 10f; Enemy.A_Damage?.Invoke(enemy, hit); Enemy.A_Damage?.Invoke(enemy, hit); Enemy.A_Damage?.Invoke(boss, hit);
            items.Tick(); Near(item.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, .01f, "one execute bonus");
            MyTime.time = 10.31f; Enemy.A_Damage?.Invoke(boss, hit); Enemy.A_Damage?.Invoke(enemy, new DamageContainer(1f, "weapon")); Enemy.A_Damage?.Invoke(enemy, hit);
            items.Tick(); Near(item.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, .01f, "one-second stat refresh");
            MyTime.time = 11f; items.Tick(); Near(item.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, .02f, "second non-boss execute");
            items.AddItem(EItem.JoesDagger); MyTime.time = 11.1f; Enemy.A_Damage?.Invoke(enemy, hit); MyTime.time = 12f; items.Tick(); Near(item.statModifiers[EStat.DamageMultiplier].GetModifiers().Single().modification, .04f, "two-stack future bonus");
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset"); string description = item.GetDescription(asset.localizedDescription); Check(description.Contains("1%") && description.Contains("2%") && !description.Contains("{"), "localized description: " + description);
            items.RemoveItem(EItem.JoesDagger); items.RemoveItem(EItem.JoesDagger); var before = (int)typeof(ItemJoesDagger).GetField("stacks", P).GetValue(item); MyTime.time = 13f; Enemy.A_Damage?.Invoke(enemy, hit); Check((int)typeof(ItemJoesDagger).GetField("stacks", P).GetValue(item) == before, "removal unsubscribes");
            Debug.Log("JoesDaggerPortCheck passed: original asset, attack eligibility and chance, independent cooldowns, boss exclusion, stacking, stat updates, localization and removal");
        }
        finally { items.Cleanup(); UnityEngine.Object.DestroyImmediate(root); DataManager.Instance = oldData; global::Utility.MyRandom.random = oldRandom; MyTime.time = oldTime; MyTime.paused = oldPaused; }
    }
}
