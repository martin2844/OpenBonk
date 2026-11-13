using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class ShadowstepPassivePortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ShadowstepPassivePortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Shadowstep test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory();
        inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory();
        var passive = new PassiveAbilityShadowstep(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var enemy = root.AddComponent<Enemy>(); var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
        enemy.hp = 400f; enemy.maxHp = 1000f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 400f);
        DamageContainer captured = null; enemy.A_DamageNonStatic += (_, dc) => captured = dc;
        try
        {
            LocalizationSettings.SelectedLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/English (en).asset");
            string description = passive.GetDescription(new LocalizedString("PassiveAbilities", "SHADOWSTEP_DESC"));
            Require(description.Contains("Execute") && description.Contains("0.5%") && !description.Contains("{"), "description: " + description);
            float evasion = inventory.playerStats.GetRawStat(EStat.Evasion); passive.Init(); PlayerXp.A_LevelUp?.Invoke(3); inventory.playerStats.Tick();
            Require(Mathf.Abs(inventory.playerStats.GetRawStat(EStat.Evasion) - evasion - .015f) < .00001f, "level-up evasion");
            PlayerHealth.A_Evaded?.Invoke(null);
            data.canBeExecuted = false; PlayerHealth.A_Evaded?.Invoke(enemy);
            Require(enemy.hp == 380f && captured != null && captured.damage == 20f && captured.isExecute && captured.damageEffect == EDamageEffect.Execute && captured.procCoefficient == .1f, "protected enemy receives 2% max HP through damage pipeline");
            enemy.hp = 400f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 400f); data.canBeExecuted = true;
            typeof(Enemy).GetField("enemyFlag", Private).SetValue(enemy, EEnemyFlag.Boss); PlayerHealth.A_Evaded?.Invoke(enemy);
            Require(enemy.hp == 380f, "boss receives 2% max HP instead of full execution");
            typeof(Enemy).GetField("enemyFlag", Private).SetValue(enemy, EEnemyFlag.None); enemy.hp = 400f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 400f);
            enemy.debuffs.Add(EDebuff.Echo, null); PlayerHealth.A_Evaded?.Invoke(enemy);
            Require((float)typeof(Enemy).GetField("echoDamage", Private).GetValue(enemy) == 400f, "executable enemy receives its current HP as execute damage");
            passive.Cleanup(); enemy.debuffs.Clear(); enemy.hp = 400f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 400f);
            PlayerHealth.A_Evaded?.Invoke(enemy); Require(enemy.hp == 400f, "cleanup removes evade handler");
            Debug.Log("ShadowstepPassivePortCheck passed: evasion, executable/protected/boss damage and cleanup");
        }
        finally { passive.Cleanup(); inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; }
    }
}
