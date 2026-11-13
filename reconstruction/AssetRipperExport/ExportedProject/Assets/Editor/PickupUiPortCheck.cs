using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.UI.HUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;

public static class PickupUiPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("PickupUiPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("pickup UI test"); root.SetActive(false);
        var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui; ui.scoreUi = root.AddComponent<ScoreUi>(); var pickup = root.AddComponent<Pickup>();
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); player.inventory.playerStats.stats = new Dictionary<EStat, float> { [EStat.PowerupBoostMultiplier] = 1f };
        player.inventory.statusEffects = new PlayerStatusEffects();
        var manager = root.AddComponent<PickupManager>(); PickupManager.Instance = manager; var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
        pool.powerupPool = new ObjectPool<GameObject>(() => root); pool.powerupPool.Get();
        var show = typeof(Pickup).GetMethod("ShowUi", BindingFlags.Instance | BindingFlags.NonPublic);
        var queue = (Queue<ScoreContainer>)typeof(ScoreUi).GetField("scoreQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui.scoreUi);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            foreach (var kind in new[] { EPickup.Xp, EPickup.Gold, EPickup.Health, EPickup.Silver }) show.Invoke(pickup, new object[] { kind });
            Require(queue.Count == 0, "non-powerups do not enqueue messages");
            foreach (var kind in new[] { EPickup.Nuke, EPickup.Time, EPickup.Shield, EPickup.Rage, EPickup.Haste, EPickup.Stonks, EPickup.Magnet }) show.Invoke(pickup, new object[] { kind });
            Require(queue.Count == 7, "all seven powerups enqueue messages");
            var nuke = queue.Dequeue(); Require(nuke.header == "Nuke" && nuke.description == "boom!" && nuke.isPositive && nuke.useSfx && nuke.sizeMultiplier == 1f, "original nuke strings and flags");
            ScoreContainer last = null; while (queue.Count > 0) last = queue.Dequeue();
            Require(last.header == "Magnet" && last.description == "drag in all xp", "original magnet strings");
            pickup.ePickup = EPickup.Haste; typeof(Pickup).GetMethod("ApplyPickup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, null);
            Require(player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Haste) && queue.Count == 1 && pool.powerupPool.CountInactive == 1, "actual haste pickup applies effect, announces and returns to pool");
            Debug.Log("PickupUiPortCheck passed: powerup notifications and original localized text");
        }
        finally { player.inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); UiManager.Instance = null; GameManager.Instance = null; MyPlayer.Instance = null; PickupManager.Instance = null; PoolManager.Instance = null; }
    }
}
