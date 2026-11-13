using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.HUD;
using UnityEditor;
using UnityEngine;

public static class SpecialTomesPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("SpecialTomesPortCheck: " + message); }
    static StatModifier Expected(int seed, bool chaos, ERarity rarity, out bool positive)
    {
        global::Utility.MyRandom.random = new System.Random(seed); UnityEngine.Random.InitState(173);
        var modifier = EncounterUtility.GetRandomStatOffers(1, false, !chaos)[0].effects[0].statModifier;
        modifier.modification = StatUtility.GetRarityValue(modifier.modification * (chaos ? 1.4f : 1.5f), rarity, 3);
        positive = chaos || global::Utility.MyRandom.random.NextDouble() > .25;
        if (!positive) modifier.modification = -modifier.modification;
        return modifier;
    }
    public static void Run()
    {
        var root = new GameObject("synthetic state"); root.SetActive(false);
        GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.Luck] = 1f }; inventory.playerStats = stats; inventory.statInventory = new StatInventory(); inventory.tomeInventory = new TomeInventory();
        var ui = root.AddComponent<UiManager>(); UiManager.Instance = ui; ui.scoreUi = root.AddComponent<ScoreUi>();
        var manager = root.AddComponent<DataManager>(); DataManager.Instance = manager;
        var tomeMap = (Dictionary<ETome,TomeData>)typeof(DataManager).GetField("tomeData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
        var chaos = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeChaos.asset"); var gambler = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeGamba.asset"); var hoarder = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeHoarder.asset");
        tomeMap[chaos.eTome] = chaos; tomeMap[gambler.eTome] = gambler; tomeMap[hoarder.eTome] = hoarder;
        int positiveSeed = -1, negativeSeed = -1;
        for (int seed = 0; seed < 100 && (positiveSeed < 0 || negativeSeed < 0); seed++)
        {
            var expected = Expected(seed, false, ERarity.Rare, out bool positive);
            if (expected.modification == 0f) continue;
            if (positive && positiveSeed < 0) positiveSeed = seed; else if (!positive && negativeSeed < 0) negativeSeed = seed;
        }
        Require(positiveSeed >= 0 && negativeSeed >= 0, "seeds cover both gambler outcomes");
        foreach (var sample in new[] { (data: chaos, seed: 31, chaos: true), (data: gambler, seed: positiveSeed, chaos: false), (data: gambler, seed: negativeSeed, chaos: false) })
        {
            var expected = Expected(sample.seed, sample.chaos, ERarity.Rare, out bool positive);
            inventory.statInventory = new StatInventory();
            global::Utility.MyRandom.random = new System.Random(sample.seed); UnityEngine.Random.InitState(173);
            inventory.tomeInventory.AddTome(sample.data, sample.data.GetUpgradeOffer(ERarity.Rare), ERarity.Rare);
            var actual = inventory.statInventory.permanentChanges[expected.stat];
            Require(actual.Count == 1 && actual[0].modifyType == expected.modifyType && Mathf.Abs(actual[0].modification - expected.modification) < .0001f, "tome adds the original scaled random modifier");
            if (!sample.chaos) Require(positive == (actual[0].modification > 0f), "gambler applies the seeded positive or negative outcome");
        }
        Require(inventory.tomeInventory.GetTomeLevel(chaos.eTome) == 1 && inventory.tomeInventory.GetTomeLevel(gambler.eTome) == 2, "tome levels advance");
        int groundLayer = LayerMask.NameToLayer("Ground"); var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "synthetic ground"; ground.layer = groundLayer; ground.transform.position = new Vector3(0f,-.5f,0f); ground.transform.localScale = new Vector3(10f,1f,10f);
        GameManager.Instance.whatIsGround = 1 << groundLayer; GameManager.Instance.whatIsGroundAndObjects = 1 << groundLayer; MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(2f,1f,2f); MyPlayer.Instance.transform.position = new Vector3(10f,1f,0f); Physics.SyncTransforms();
        var effectManager = root.AddComponent<EffectManager>(); EffectManager.Instance = effectManager; var chestTemplate = new GameObject("Hoarder test chest"); chestTemplate.transform.position = new Vector3(-100f,0f,0f); effectManager.openChestNormal = chestTemplate;
        inventory.tomeInventory.AddTome(hoarder, hoarder.GetUpgradeOffer(ERarity.Rare), ERarity.Rare);
        var chest = GameObject.Find("Hoarder test chest(Clone)"); Require(chest != null && Mathf.Abs(chest.transform.position.y) < .6f, "Hoarder spawns a normal chest on map ground");
        Require(inventory.tomeInventory.GetTomeLevel(hoarder.eTome) == 1, "Hoarder tome level advances");
        UnityEngine.Object.DestroyImmediate(chest); UnityEngine.Object.DestroyImmediate(chestTemplate); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("SpecialTomesPortCheck passed: Chaos, both Gambler outcomes and Hoarder chest");
    }
}
