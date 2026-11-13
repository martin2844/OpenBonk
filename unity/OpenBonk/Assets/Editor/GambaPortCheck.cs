using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.UI.HUD;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEditor;
using UnityEngine;

public static class GambaPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("GambaPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("Gamba test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        inventory.characterData = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
        inventory.tomeInventory = new TomeInventory(); inventory.itemInventory = new ItemInventory(); inventory.statusEffects = new PlayerStatusEffects(); inventory.statInventory = new StatInventory(); inventory.playerXp = new PlayerXp { level = 1 };
        var passive = new PassiveAbilityGamba(); inventory.passiveAbility = passive; inventory.playerStats = new PlayerStatsNew(inventory, true);
        var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui; ui.scoreUi = root.AddComponent<ScoreUi>();
        var oldRandom = global::Utility.MyRandom.random;
        var oldUnityRandom = UnityEngine.Random.state;
        try
        {
            global::Utility.MyRandom.random = new System.Random(1234);
            UnityEngine.Random.InitState(1234);
            var expected = EncounterUtility.GetRandomStatOffers(1, false, false)[0].effects[0].statModifier;
            float expectedValue = StatUtility.GetRarityValue(expected.modification, ERarity.Common, 3) * .75f;
            global::Utility.MyRandom.random = new System.Random(1234);
            UnityEngine.Random.InitState(1234);
            passive.Init(); PlayerXp.A_LevelUp?.Invoke(1);
            var first = inventory.statInventory.permanentChanges[expected.stat][0];
            Require(first.modifyType == expected.modifyType && Mathf.Abs(first.modification - expectedValue) < .00001f, "seeded first upgrade and value");
            PlayerXp.A_LevelUp?.Invoke(12);
            Require(inventory.statInventory.permanentChanges.Values.Sum(v => v.Count) == 12, "one upgrade per missing level");
            Require(ui.scoreUi.GetQueueCount() == 10, "popup queue limit");
            PlayerXp.A_LevelUp?.Invoke(5);
            Require(inventory.statInventory.permanentChanges.Values.Sum(v => v.Count) == 12, "old level does not reroll");
            typeof(PassiveAbilityGamba).GetField("currentLevel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(passive, 50);
            global::Utility.MyRandom.random = new System.Random(5678); UnityEngine.Random.InitState(5678);
            var later = EncounterUtility.GetRandomStatOffers(1, false, false)[0].effects[0].statModifier;
            float laterValue = StatUtility.GetRarityValue(later.modification, ERarity.Common, 3) * .375f;
            global::Utility.MyRandom.random = new System.Random(5678); UnityEngine.Random.InitState(5678);
            PlayerXp.A_LevelUp?.Invoke(51);
            Require(Mathf.Abs(inventory.statInventory.permanentChanges[later.stat].Last().modification - laterValue) < .00001f, "level 50 diminishing multiplier");
            passive.Cleanup(); PlayerXp.A_LevelUp?.Invoke(52);
            Require(inventory.statInventory.permanentChanges.Values.Sum(v => v.Count) == 13, "cleanup unsubscribes");
            Debug.Log("GambaPortCheck passed: seeded upgrade, level catch-up, popup cap and cleanup");
        }
        finally { passive.Cleanup(); global::Utility.MyRandom.random = oldRandom; UnityEngine.Random.state = oldUnityRandom; inventory.playerStats.OnDestroy(); inventory.statusEffects.OnDestroy(); inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; UiManager.Instance = null; GameManager.Instance = null; }
    }
}
