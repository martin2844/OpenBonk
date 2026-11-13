using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.MapsAndStages;
using Utility;
using UnityEditor;
using UnityEngine;

public static class StageEnemyPoolPortCheck
{
    private sealed class FixedRandom : System.Random
    {
        readonly double result;
        public FixedRandom(double result) { this.result = result; }
        public override double NextDouble() => result;
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string name) { if (!condition) throw new Exception("StageEnemyPoolPortCheck: " + name); }
    public static void Run()
    {
        var oldMap = MapController.currentMap; int oldIndex = MapController.index; float oldTime = MyTime.stageTimer; float oldSwarmTime = MyTime.finalSwarmTimer; float oldGameTime = MyTime.time;
        var oldGame = GameManager.Instance; var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldEnemies = EnemyManager.Instance; var oldRandom = MyRandom.random;
        var root = new GameObject("stage pool fixture"); root.SetActive(false);
        var map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Forest; map.stageDuration = 600f;
        var a = ScriptableObject.CreateInstance<EnemyData>(); var b = ScriptableObject.CreateInstance<EnemyData>();
        var ghost = ScriptableObject.CreateInstance<EnemyData>(); ghost.enemyName = EEnemy.Ghost; ghost.maps = EMap.Forest; ghost.creditCost = 3f;
        var greaterGhost = ScriptableObject.CreateInstance<EnemyData>(); greaterGhost.enemyName = EEnemy.GreaterGhost; greaterGhost.creditCost = 3f;
        var purpleGhost = ScriptableObject.CreateInstance<EnemyData>(); purpleGhost.enemyName = EEnemy.GhostPurple; purpleGhost.creditCost = 3f;
        var redGhost = ScriptableObject.CreateInstance<EnemyData>(); redGhost.enemyName = EEnemy.GhostRed; redGhost.creditCost = 3f;
        a.enemyName = EEnemy.Goblin; a.maps = EMap.Forest; a.creditCost = 2f; a.canSpawnAfterTime = 30f;
        b.enemyName = EEnemy.GoblinStrong; b.maps = EMap.Forest; b.creditCost = 100f; b.canSpawnAfterTime = 0f;
        StageSummoner summoner = null;
        try
        {
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map }); MapController.index = 0;
            GameManager.Instance = root.AddComponent<GameManager>(); EnemyManager.Instance = root.AddComponent<EnemyManager>();
            DataManager.Instance = root.AddComponent<DataManager>(); DataManager.Instance.unsortedEnemies = new List<EnemyData> { a, b };
            typeof(DataManager).GetField("enemyData", Private).SetValue(DataManager.Instance, new Dictionary<EEnemy, EnemyData> { [a.enemyName] = a, [b.enemyName] = b });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            player.inventory.statInventory = new StatInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 0f;
            stats.stats[EStat.EnemyAmountMultiplier] = 1f; player.inventory.playerStats = stats;
            summoner = new StageSummoner(42, new List<EEnemy>());
            MyTime.stageTimer = 30f;
            Check(!summoner.AddEnemyToPoolAndPickNewCard(null, out _), "spawn-time boundary and expensive enemy excluded");
            MyTime.stageTimer = 30.01f;
            Check(summoner.AddEnemyToPoolAndPickNewCard(null, out var selected) && selected == EEnemy.Goblin, "affordable enemy becomes eligible after boundary");
            var current = (List<EEnemy>)typeof(BaseSummoner).GetField("currentEnemies", Private).GetValue(summoner);
            var pool = (List<EEnemy>)typeof(StageSummoner).GetField("enemyPool", Private).GetValue(summoner);
            Check(current.Count == 1 && current[0] == EEnemy.Goblin && !pool.Contains(EEnemy.Goblin), "selected enemy moved from pool into active cards");
            Check(!summoner.AddEnemyToPoolAndPickNewCard(null, out _), "unaffordable remaining enemy excluded");
            b.creditCost = 1f;
            Check(summoner.AddEnemyToPoolAndPickNewCard(null, out selected) && selected == EEnemy.GoblinStrong, "remaining affordable enemy added");
            Check(!summoner.AddEnemyToPoolAndPickNewCard(null, out _), "empty pool cannot add another card");
            stats.stats[EStat.EliteSpawnIncrease] = 2f;
            var normal = new EnemyCard(a, false); var elite = new EnemyCard(a, true);
            Check(normal.cost == 2f && normal.weight == 1f && elite.cost == 4f && Mathf.Approximately(elite.weight, .08f), "normal and elite costs and weights");
            var cards = typeof(BaseSummoner).GetField("cards", Private); var credits = typeof(BaseSummoner).GetField("credits", Private);
            var choose = typeof(BaseSummoner).GetMethod("GetRandomCard", Private);
            cards.SetValue(summoner, new List<EnemyCard> { normal, elite }); credits.SetValue(summoner, 4f);
            MyRandom.random = new FixedRandom(.9); Check(ReferenceEquals(choose.Invoke(summoner, new object[] { true }), normal), "weighted draw selects normal below threshold");
            MyRandom.random = new FixedRandom(.99); Check(ReferenceEquals(choose.Invoke(summoner, new object[] { true }), elite), "weighted draw selects elite above threshold");
            Check(ReferenceEquals(choose.Invoke(summoner, new object[] { false }), elite), "unweighted selection chooses most expensive affordable card");
            credits.SetValue(summoner, 3f); Check(ReferenceEquals(choose.Invoke(summoner, new object[] { true }), normal), "elite excluded when unaffordable");
            credits.SetValue(summoner, 1f); Check(choose.Invoke(summoner, new object[] { true }) == null, "no affordable cards returns null");
            var waveCard = new EnemyCard(b, false);
            typeof(BaseSummoner).GetField("waveModeCards", Private).SetValue(summoner, new List<EnemyCard> { waveCard });
            typeof(BaseSummoner).GetField("isWaveMode", Private).SetValue(summoner, true);
            Check(ReferenceEquals(choose.Invoke(summoner, new object[] { true }), waveCard), "wave mode uses its own affordable cards");
            var manager = EnemyManager.Instance;
            var controller = (SummonerController)FormatterServices.GetUninitializedObject(typeof(SummonerController));
            manager.summonerController = controller;
            var swarm = typeof(SummonerController).GetField("isFinalSwarmStarted", Private);
            Check(manager.GetNumMaxEnemies() == 550, "normal enemy cap");
            manager.numEnemies = 549; Check(manager.CanSpawnEnemy() && !manager.HasMaxEnemies(), "normal count below cap");
            manager.numEnemies = 550; Check(!manager.CanSpawnEnemy() && manager.HasMaxEnemies(), "normal cap enforced");
            swarm.SetValue(controller, true); MyTime.finalSwarmTimer = 119.99f;
            Check(manager.GetNumMaxEnemies() == 400, "first final-swarm cap");
            manager.numEnemies = 400; Check(!manager.CanSpawnEnemy() && manager.HasMaxEnemies(), "first final-swarm cap enforced");
            MyTime.finalSwarmTimer = 120f; Check(manager.GetNumMaxEnemies() == 300, "late final-swarm cap at boundary");
            manager.numEnemies = 299; Check(manager.CanSpawnEnemy() && !manager.HasMaxEnemies(), "late swarm below cap");
            manager.numEnemies = 300; Check(!manager.CanSpawnEnemy() && manager.HasMaxEnemies(), "late swarm cap enforced");
            DataManager.Instance.unsortedEnemies.Add(ghost);
            var enemyData = (Dictionary<EEnemy, EnemyData>)typeof(DataManager).GetField("enemyData", Private).GetValue(DataManager.Instance);
            foreach (var entry in new[] { ghost, greaterGhost, purpleGhost, redGhost }) enemyData[entry.enemyName] = entry;
            typeof(SummonerController).GetField("timeline", Private).SetValue(controller, new StageTimeline { startEnemies = new List<EEnemy> { EEnemy.Goblin } });
            var controllerList = typeof(SummonerController).GetField("summoners", Private);
            controllerList.SetValue(controller, new List<BaseSummoner> { summoner }); swarm.SetValue(controller, false);
            var ui = root.AddComponent<FinalSwarmSilverUi>(); ui.contentParent = new GameObject("swarm content"); ui.contentParent.SetActive(false);
            typeof(FinalSwarmSilverUi).GetMethod("Start", Private).Invoke(ui, null);
            int started = 0; Action onStart = () => started++; SummonerController.A_FinalSwarmStarted += onStart;
            try
            {
                var startSwarm = typeof(SummonerController).GetMethod("StartFinalSwarm", Private);
                startSwarm.Invoke(controller, null); startSwarm.Invoke(controller, null);
                var active = (List<BaseSummoner>)controllerList.GetValue(controller);
                Check(controller.IsFinalSwarm() && started == 1 && active.Count == 1 && active[0] is SwarmSummoner, "final swarm replaces stage summoner once");
                var finalSwarm = (SwarmSummoner)active[0]; var tickExtra = typeof(SwarmSummoner).GetMethod("TickExtra", Private);
                var tier = typeof(SwarmSummoner).GetField("ghostTier", Private);
                var cardField = typeof(BaseSummoner).GetField("cards", Private);
                foreach (var t in new[] { (seconds: 60f, expected: 0, enemy: EEnemy.Ghost), (60.01f, 1, EEnemy.GreaterGhost),
                                          (360f, 1, EEnemy.GreaterGhost), (360.01f, 2, EEnemy.GhostPurple),
                                          (720f, 2, EEnemy.GhostPurple), (720.01f, 3, EEnemy.GhostRed) })
                {
                    MyTime.finalSwarmTimer = t.Item1; tickExtra.Invoke(finalSwarm, null);
                    var swarmCards = (List<EnemyCard>)cardField.GetValue(finalSwarm);
                    Check((int)tier.GetValue(finalSwarm) == t.Item2 && swarmCards.Count == 2 && swarmCards.TrueForAll(c => c.enemy.enemyName == t.Item3), "final swarm ghost tier at " + t.Item1);
                }
                MyTime.time = 10f; MyTime.finalSwarmTimer = 120f;
                typeof(FinalSwarmSilverUi).GetMethod("UpdateMultiplier", Private).Invoke(ui, null);
                Check(ui.contentParent.activeSelf && player.inventory.statInventory.movingStats.TryGetValue("Swarm Silver Multiplier", out var multiplier) && Mathf.Approximately(multiplier.modification, 4.5f), "final swarm activates silver multiplier");
                controller.StopFinalSwarm(); active = (List<BaseSummoner>)controllerList.GetValue(controller);
                Check(!controller.IsFinalSwarm() && active.Count == 1 && active[0] is StageSummoner && ReferenceEquals(active[0], controller.stageSummoner), "stopping swarm restores stage summoner");
                Check(ui.contentParent.activeSelf && player.inventory.statInventory.movingStats.ContainsKey("Swarm Silver Multiplier"), "actual stop retains HUD and multiplier without stop event");
                MyTime.finalSwarmTimer = 0f; MyTime.time = 12f;
                typeof(FinalSwarmSilverUi).GetMethod("UpdateMultiplier", Private).Invoke(ui, null);
                Check(player.inventory.statInventory.movingStats.TryGetValue("Swarm Silver Multiplier", out multiplier) && Mathf.Approximately(multiplier.modification, 4.5f), "reset timer does not immediately clear prior silver multiplier");
                startSwarm.Invoke(controller, null); active = (List<BaseSummoner>)controllerList.GetValue(controller);
                Check(controller.IsFinalSwarm() && started == 2 && active.Count == 1 && active[0] is SwarmSummoner, "final swarm can start again after stopping");
                controller.Cleanup();
            }
            finally { SummonerController.A_FinalSwarmStarted -= onStart; typeof(FinalSwarmSilverUi).GetMethod("OnDestroy", Private).Invoke(ui, null); UnityEngine.Object.DestroyImmediate(ui.contentParent); }
            Debug.Log("StageEnemyPoolPortCheck passed: selection, caps, final-swarm lifecycle and observed HUD retention");
        }
        finally
        {
            summoner?.Cleanup(); GameManager.Instance = oldGame; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; EnemyManager.Instance = oldEnemies; MyRandom.random = oldRandom;
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { oldMap }); MapController.index = oldIndex; MyTime.stageTimer = oldTime; MyTime.finalSwarmTimer = oldSwarmTime; MyTime.time = oldGameTime;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(ghost);
            UnityEngine.Object.DestroyImmediate(greaterGhost); UnityEngine.Object.DestroyImmediate(purpleGhost); UnityEngine.Object.DestroyImmediate(redGhost);
        }
    }
}
