using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class HasteTimingPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("HasteTimingPortCheck: " + message); }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;
    public static void Run()
    {
        var emptyStatus = new PlayerStatusEffects();
        var previousPlayer = MyPlayer.Instance;
        try { MyPlayer.Instance = null; emptyStatus.Tick(); }
        finally { MyPlayer.Instance = previousPlayer; emptyStatus.OnDestroy(); }
        var root = new GameObject("haste timing fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.PowerupBoostMultiplier] = 1f }; inventory.playerStats = stats;
        inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        var status = new PlayerStatusEffects(); inventory.statusEffects = status;
        float oldTime = MyTime.time;
        try
        {
            MyTime.time = 10f; status.TestPickup(EPickup.Haste);
            var first = status.statusEffects[EStatusEffect.Haste];
            Check(Near(first.expirationTime, 30f), "default duration");
            Check(first.modifiers.Length == 1 && first.modifiers[0].stat == EStat.MoveSpeedMultiplier && first.modifiers[0].modifyType == EStatModifyType.Multiplication && Near(first.modifiers[0].modification, 1.75f), "default movement multiplier");
            MyTime.time = 15f; status.TestPickup(EPickup.Haste);
            Check(ReferenceEquals(first, status.statusEffects[EStatusEffect.Haste]) && Near(first.expirationTime, 35f), "repeat pickup refreshes the existing effect");
            MyTime.time = 34.9f; status.Tick(); Check(status.HasStatusEffect(EStatusEffect.Haste), "effect remains before expiration");
            MyTime.time = 35f; status.Tick(); Check(!status.HasStatusEffect(EStatusEffect.Haste), "effect expires at deadline");
            stats.stats[EStat.PowerupBoostMultiplier] = 2f;
            MyTime.time = 40f; status.TestPickup(EPickup.Haste);
            var boosted = status.statusEffects[EStatusEffect.Haste];
            Check(Near(boosted.expirationTime, 80f) && Near(boosted.modifiers[0].modification, 3.5f), "powerup boost scales duration and multiplier");
            status.RemoveAllStatusEffects();
            var removed = new List<EStatusEffect>();
            Action<EStatusEffect> handler = effect => {
                removed.Add(effect);
                if (effect == EStatusEffect.Haste) status.RemoveAllStatusEffects();
            };
            PlayerStatusEffects.A_StatusEffectRemoved += handler;
            try
            {
                MyTime.time = 100f;
                status.AddNewEffect(new StatusEffect(EStatusEffect.Haste, 100f, Array.Empty<StatModifier>()), 0f);
                status.AddNewEffect(new StatusEffect(EStatusEffect.Shield, 100f, Array.Empty<StatModifier>()), 0f);
                status.Tick();
                Check(removed.Count == 3 && removed[0] == EStatusEffect.Haste && removed[1] == EStatusEffect.Shield && removed[2] == EStatusEffect.Shield,
                    "snapshot expiration still notifies after a callback removes the second effect");
            }
            finally { PlayerStatusEffects.A_StatusEffectRemoved -= handler; }
            Debug.Log("HasteTimingPortCheck passed: default, refresh, expiry and boosted values");
        }
        finally { status.OnDestroy(); MyTime.time = oldTime; MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); }
    }
}
