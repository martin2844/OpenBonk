using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class StageBossCompletionPortCheck {
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("StageBossCompletionPortCheck: " + msg); }
    static void FinalStage(bool value) => typeof(MapController).GetField("isFinalBossStage", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
    static Enemy MakeEnemy(GameObject root, EnemyData data, string name) {
        var obj = new GameObject(name); obj.transform.SetParent(root.transform); obj.SetActive(false);
        var enemy = obj.AddComponent<Enemy>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); enemy.hp = 100f;
        var movement = obj.AddComponent<EnemyMovementRb>(); movement.rb = obj.AddComponent<Rigidbody>(); enemy.enemyMovement = movement;
        var dissolve = obj.AddComponent<EnemyDissolve>(); dissolve.enemyRenderer = obj.AddComponent<MeshRenderer>(); dissolve.Reset(); enemy.dissolve = dissolve;
        return enemy;
    }
    public static void Run() {
        var root = new GameObject("stage completion fixture"); root.SetActive(false);
        try {
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game;
            var manager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var stage = ScriptableObject.CreateInstance<StageData>(); stage.stageTimeline = new StageTimeline { stageTime = 450f };
            typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
            MyTime.time = 1f; Check(Mathf.Approximately(game.GetStageTimeMax(), 450f), "timeline fallback without summoner");
            var summoner = (SummonerController)FormatterServices.GetUninitializedObject(typeof(SummonerController)); typeof(SummonerController).GetField("timeline", P).SetValue(summoner, stage.stageTimeline); manager.summonerController = summoner;
            MyTime.time = 2f; Check(Mathf.Approximately(game.GetStageTimeMax(), 450f), "summoner time"); stage.stageTimeline.stageTime = 400f;
            MyTime.time = 2.05f; Check(Mathf.Approximately(game.GetStageTimeMax(), 450f), "cached time before refresh"); MyTime.time = 2.2f; Check(Mathf.Approximately(game.GetStageTimeMax(), 400f), "time refresh");
            var speed = ScriptableObject.CreateInstance<ChallengeModifierSpeedrun>(); speed.internalName = "speedrun"; typeof(ChallengeModifierSpeedrun).GetProperty("timeLeftOnStage").GetSetMethod(true).Invoke(speed, new object[] { 120f });
            var names = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); names.Add("speedrun"); ChallengesTracker.challengeModifiers = new ChallengeModifier[] { speed };
            MyTime.time = 3f; Check(Mathf.Approximately(game.GetStageTimeMax(), 120f), "speedrun caps stage time"); names.Clear(); ChallengesTracker.challengeModifiers = null;
            var rsg = root.AddComponent<RsgController>(); RsgController.Instance = rsg; var room = root.AddComponent<GraveyardBossRoom>(); rsg.roomBoss = room;
            Check(!game.HasEnteredBossRoom(), "room not entered before spawn"); typeof(GraveyardBossRoom).GetProperty("hasSpawnedBoss").GetSetMethod(true).Invoke(room, new object[] { true }); Check(game.HasEnteredBossRoom(), "boss room detected");
            MyTime.time = 4f; Check(Mathf.Approximately(game.GetStageTimeMax(), 600f), "boss room overrides stage duration");
            RsgController.Instance = null; var data = ScriptableObject.CreateInstance<EnemyData>();
            var first = MakeEnemy(root, data, "first enemy"); var second = MakeEnemy(root, data, "second enemy"); manager.enemies.Add(1, first); manager.enemies.Add(2, second);
            int deaths = 0; bool nullSources = true; Action<Enemy, DamageContainer> listener = (enemy, source) => { deaths++; nullSources &= source == null; if (enemy == first) manager.enemies.Remove(2); }; Enemy.A_EnemyDied += listener;
            var stageBossDied = typeof(GameManager).GetMethod("OnStageBossDied", P); FinalStage(false); stageBossDied.Invoke(game, null); Check(deaths == 0 && first.hp == 100f && second.hp == 100f, "normal stages retain enemies");
            FinalStage(true); stageBossDied.Invoke(game, null); Check(deaths == 2 && first.hp == 0f && second.hp == 0f && nullSources, "final stage clears snapshot via real death path");
            Enemy.A_EnemyDied -= listener; UnityEngine.Object.DestroyImmediate(speed); UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(stage);
            Debug.Log("StageBossCompletionPortCheck passed: timer cache, speedrun, boss room and final-stage enemy clear");
        } finally { FinalStage(false); RsgController.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; ChallengesTracker.challengeModifiers = null; ((HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Clear(); typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { null }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
