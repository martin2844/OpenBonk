#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.Localization;
using TMPro;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class OpenBonkItemRegistrationCheck
{
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        var previous = DataManager.Instance; var previousPlayer = MyPlayer.Instance; var oldEvent = DataManager.A_DataLoaded;
        var objects = new List<Object>(); ItemInventory inventory = null;
        try
        {
            DataManager.A_DataLoaded = null;
            var host = new GameObject("item registry fixture"); objects.Add(host);
            var manager = host.AddComponent<DataManager>();
            foreach (var field in typeof(DataManager).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (typeof(IList).IsAssignableFrom(field.FieldType) && field.GetValue(manager) == null) field.SetValue(manager, Activator.CreateInstance(field.FieldType));
            var vanilla = ScriptableObject.CreateInstance<ItemData>(); objects.Add(vanilla); vanilla.isEnabled = true; vanilla.eItem = EItem.Key;
            manager.unsortedItems.Add(vanilla); manager.Load();
            var texture = new Texture2D(2, 2); objects.Add(texture);
            var item = ScriptableObject.CreateInstance<OpenBonkItemData>(); objects.Add(item); item.isEnabled = true; item.inItemPool = true; item.icon = texture;
            item.externalId = "example:shoe"; item.displayName = "Fixture"; item.description = "Fixture text";
            item.bonuses = new[] {
                new OpenBonkItemBonus { stat = EStat.MoveSpeedMultiplier, modifyType = EStatModifyType.Addition, perStack = .1f },
                new OpenBonkItemBonus { stat = EStat.DurationMultiplier, modifyType = EStatModifyType.Flat, perStack = .02f }
            };
            var id = (EItem)OpenBonkContentIds.Item("example", "shoe");
            manager.RegisterExternalItem("example", "shoe", item);
            Require(manager.GetItem(id) == item && manager.GetItem(EItem.Key) == vanilla && manager.unsortedItems.Contains(item), "registry lookup");
            bool rejected = false; try { manager.RegisterExternalItem("example", "shoe", item); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && manager.unsortedItems.Count == 2, "duplicate registration");
            Require(ItemFactory.CreateItem(id, null) is OpenBonkStackingItem && ItemFactory.CreateItem(EItem.Key, null).GetType().Name == "ItemKey", "factory dispatch");
            inventory = new ItemInventory(); inventory.AddItem(id); inventory.AddItem(id);
            var active = inventory.GetItem(id);
            Require(active.amount == 2 && Math.Abs(active.statModifiers[EStat.MoveSpeedMultiplier].GetModifiers().Single().modification - .2f) < .00001f, "stacking");
            Require(Math.Abs(active.statModifiers[EStat.DurationMultiplier].GetModifiers().Single().modification - .04f) < .00001f, "second stat");
            var playerHost = new GameObject("damage source player"); playerHost.SetActive(false); objects.Add(playerHost);
            var player = playerHost.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = inventory;
            RunStats.Init();
            EnemyDamage(new DamageContainer(0f, "mod:example:shoe") { damage = .25f });
            EnemyDamage(new DamageContainer(0f, "mod:example:shoe") { damage = .5f });
            var damage = RunStats.damageSources["mod:example:shoe"];
            var rowHost = new GameObject("damage source row"); objects.Add(rowHost); var row = rowHost.AddComponent<DamageSourceEntry>();
            row.icon = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>(); row.icon.transform.SetParent(rowHost.transform);
            row.t_sourceName = new GameObject("name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); row.t_sourceName.transform.SetParent(rowHost.transform);
            row.t_lvl = new GameObject("level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); row.t_lvl.transform.SetParent(rowHost.transform);
            row.Set(damage);
            Require(Math.Abs(damage.damage - .75f) < .00001f && row.icon.texture == texture && row.t_sourceName.text == "Fixture" && row.t_lvl.text == "2", "mod damage row totals, icon, name and stack count");
            Require(LocalizationUtility.GetLocalizedDamageSource("mod:example:missing") == "example:missing", "missing mod source has safe fallback");
            inventory.RemoveItem(id); Require(active.amount == 1 && Math.Abs(active.statModifiers[EStat.MoveSpeedMultiplier].GetModifiers().Single().modification - .1f) < .00001f, "remove stack");
            inventory.RemoveItem(id); Require(inventory.GetItem(id) == null, "remove final stack");
            Debug.Log("OpenBonkItemRegistrationCheck PASS: registration, stacking, mod damage row and removal");
        }
        finally
        {
            RunStats.Cleanup(); inventory?.Cleanup(); MyPlayer.Instance = previousPlayer; DataManager.A_DataLoaded = oldEvent; DataManager.Instance = previous;
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        }
    }
    private static void EnemyDamage(DamageContainer damage) => Assets.Scripts.Actors.Enemies.Enemy.A_Damage?.Invoke(null, damage);
}
#endif
