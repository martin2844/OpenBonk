using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Player.Movement;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class SpeedDemonPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("SpeedDemonPortCheck: " + message); }
    static void Near(float actual, float expected, string message) => Require(Mathf.Abs(actual - expected) < .0003f, message + ": " + actual + " expected " + expected);
    public static void Run()
    {
        var root = new GameObject("Speed Demon test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 20 };
        var passive = new PassiveAbilitySpeedDemon(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var body = root.AddComponent<Rigidbody>(); body.velocity = new Vector3(10f, 0f, 0f);
        player.playerMovement = root.AddComponent<PlayerMovement>(); typeof(PlayerMovement).GetField("rb", Private).SetValue(player.playerMovement, body);
        player.playerMovement.movementValues = new PlayerMovementValues(); player.playerMovement.movementValues.CreateMovement(body, ECharacter.Fox);
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            var passiveData = AssetDatabase.LoadAssetAtPath<PassiveData>("Assets/MonoBehaviour/Speed Demon.asset"); passiveData.Init();
            Require(passiveData.GetDescription() == "Speed builds up over time, but is halved when you take damage. Also gain damage based on speed.", "original inherited description");
            float baseSpeed = inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier);
            float baseDamage = inventory.playerStats.GetRawStat(EStat.DamageMultiplier);
            MyTime.time = 10f; passive.Init(); PlayerXp.A_LevelUp?.Invoke(20); passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed, "first speed increase waits a second");
            float firstBonus = ((baseSpeed - 1f) * .5f + 20f * .0075f) * Mathf.Clamp01(7.5f / (10f * baseSpeed));
            Near(inventory.playerStats.GetRawStat(EStat.DamageMultiplier), baseDamage + firstBonus, "movement-derived damage");
            for (int t = 11; t <= 110; t++) { MyTime.time = t; passive.Tick(); inventory.playerStats.Tick(); }
            Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed * 1.85f, "speed builds over time");
            foreach (string source in new[] { PlayerHealth.fallDamageSource, PlayerHealth.healthRegenDamageSource, DebuffPoison.poisonDamageSource })
                PlayerHealth.A_TakeDamage?.Invoke(null, new DamageContainer(0f, source), false);
            inventory.playerStats.Tick(); Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed * 1.85f, "fall, poison and regeneration damage do not reduce speed");
            PlayerHealth.A_TakeDamage?.Invoke(null, new DamageContainer(0f, "Enemy"), false); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed * 1.6f, "level 20 hit removes one sixth of the cap");
            typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(GameManager.Instance, new object[] { true });
            MyTime.time = 112f; passive.Tick(); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed * 1.6f, "crypt pauses speed build");
            passive.Cleanup(); PlayerHealth.A_TakeDamage?.Invoke(null, new DamageContainer(0f, "Enemy"), false); inventory.playerStats.Tick();
            Near(inventory.playerStats.GetRawStat(EStat.MoveSpeedMultiplier), baseSpeed * 1.6f, "cleanup removes damage handler");
            Debug.Log("SpeedDemonPortCheck passed: timed speed, movement damage, hit exclusions, crypt and cleanup");
        }
        finally { passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
