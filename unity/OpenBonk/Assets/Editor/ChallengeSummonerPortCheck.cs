using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class ChallengeSummonerPortCheck
{
    static void Require(bool v, string why) { if (!v) throw new Exception("ChallengeSummonerPortCheck: " + why); }
    public static void Run()
    {
        var root = new GameObject("challenge summoner test"); root.SetActive(false);
        var manager = root.AddComponent<GameManager>(); var player = root.AddComponent<MyPlayer>(); var data = root.AddComponent<DataManager>();
        var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest;
        var eligible = ScriptableObject.CreateInstance<EnemyData>(); eligible.enemyName = EEnemy.Goblin; eligible.maps = EMap.Forest; eligible.minStage = 0; eligible.creditCost = 1f;
        var wrongMap = ScriptableObject.CreateInstance<EnemyData>(); wrongMap.maps = EMap.Desert; wrongMap.creditCost = 1f;
        var tooLate = ScriptableObject.CreateInstance<EnemyData>(); tooLate.maps = EMap.Forest; tooLate.minStage = 9; tooLate.creditCost = 1f;
        var tooExpensive = ScriptableObject.CreateInstance<EnemyData>(); tooExpensive.maps = EMap.Forest; tooExpensive.creditCost = 1000f;
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldMap = MapController.currentMap; int oldStage = MapController.index; float oldTimer = MyTime.stageTimer;
        ChallengeSummoner summoner = null;
        try
        {
            GameManager.Instance = manager; MyPlayer.Instance = player; DataManager.Instance = data;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.EnemyAmountMultiplier] = 10f, [EStat.EliteSpawnIncrease] = 1f }; player.inventory.playerStats = stats;
            data.unsortedEnemies = new List<EnemyData> { eligible, wrongMap, tooLate, tooExpensive };
            var dict = (Dictionary<EEnemy, EnemyData>)typeof(DataManager).GetField("enemyData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data); dict[eligible.enemyName] = eligible;
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map }); MapController.index = 0; MyTime.stageTimer = 60f;
            summoner = new ChallengeSummoner(7, new List<EEnemy>());
            var pool = (List<EEnemy>)typeof(ChallengeSummoner).GetField("enemyPool", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(summoner);
            Require(pool.Count == 1 && pool[0] == EEnemy.Goblin, "select one enemy eligible for map, stage and credits");
            float initial = summoner.GetInitialCredits(); Require(initial >= 45f && initial <= 75f, "credits scale with enemy amount and stage time");
            var cards = (List<EnemyCard>)typeof(BaseSummoner).GetField("cards", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(summoner);
            Require(cards.Count == 2 && cards[0].enemy == eligible && cards[1].enemy == eligible, "base constructor uses selected pool");
            Require(!cards[0].isElite && cards[1].isElite && Mathf.Approximately(cards[0].cost, 1f) && Mathf.Approximately(cards[1].cost, 2f), "normal and elite costs");
            Require(Mathf.Approximately(cards[0].weight, 1f) && Mathf.Approximately(cards[1].weight, .04f), "normal and elite weights");
            var credits = typeof(BaseSummoner).GetField("credits", BindingFlags.NonPublic | BindingFlags.Instance);
            var choose = typeof(BaseSummoner).GetMethod("GetRandomCard", BindingFlags.NonPublic | BindingFlags.Instance);
            credits.SetValue(summoner, .5f);
            Require(choose.Invoke(summoner, new object[] { false }) == null, "no affordable card");
            credits.SetValue(summoner, 1.5f);
            Require(ReferenceEquals(choose.Invoke(summoner, new object[] { false }), cards[0]), "elite challenge falls back to normal when elite is unaffordable");
            credits.SetValue(summoner, 2f);
            Require(ReferenceEquals(choose.Invoke(summoner, new object[] { false }), cards[1]), "elite challenge picks most expensive affordable card");
            Require(summoner.GetSummonInterval() == 0 && summoner.GetBaseCreditsPerSecond() == 0 && summoner.GetNumTargetEnemies() == 999, "summoner constants");
            Require((bool)typeof(ChallengeSummoner).GetMethod("ForceSpawn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(summoner, null), "force spawn");
            Debug.Log("ChallengeSummonerPortCheck PASS");
        }
        finally
        {
            summoner?.Cleanup(); GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { oldMap }); MapController.index = oldStage; MyTime.stageTimer = oldTimer;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(eligible); UnityEngine.Object.DestroyImmediate(wrongMap); UnityEngine.Object.DestroyImmediate(tooLate); UnityEngine.Object.DestroyImmediate(tooExpensive);
        }
    }
}
