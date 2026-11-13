using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class GameTimerPortCheck {
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("GameTimerPortCheck: " + msg); }
    static void SetProperty(object o, string n, object value) => o.GetType().GetProperty(n).GetSetMethod(true).Invoke(o, new[] { value });
    public static void Run() {
        var root = new GameObject("timer test"); root.SetActive(false);
        try {
            var game = root.AddComponent<GameManager>(); GameManager.Instance = game; typeof(GameManager).GetField("isPlaying", P).SetValue(game, true);
            var enemy = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemy; enemy.enabledWaves = true;
            var timeline = new StageTimeline { stageTime = 600f }; var summoner = (SummonerController)FormatterServices.GetUninitializedObject(typeof(SummonerController)); typeof(SummonerController).GetField("timeline", P).SetValue(summoner, timeline); enemy.summonerController = summoner;
            var timer = root.AddComponent<GameTimer>(); timer.t_timerRun = new GameObject("run").AddComponent<TextMeshProUGUI>(); timer.t_timerStage = new GameObject("stage").AddComponent<TextMeshProUGUI>(); timer.t_timerSpeedrun = new GameObject("extra").AddComponent<TextMeshProUGUI>(); timer.t_timerStage.fontSize = 36f; timer.t_timerSpeedrun.text = "unchanged";
            MyTime.time = 1f; MyTime.runTimer = 125f; MyTime.stageTimer = 12f;
            typeof(GameTimer).GetMethod("Start", P).Invoke(timer, null);
            Check(timer.t_timerRun.text == "<size=110%><sprite name=clock></size> 2:05" && timer.t_timerStage.text == "9:48", "run clock and stage countdown"); Check(timer.t_timerSpeedrun.text == "unchanged", "unused serialized text preserved");
            var speed = ScriptableObject.CreateInstance<ChallengeModifierSpeedrun>(); speed.internalName = "speedrun"; speed.timeLimitMinutes = 5f; SetProperty(speed, "timeLeftOnStage", 187f);
            var names = (HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); names.Add("speedrun"); ChallengesTracker.challengeModifiers = new ChallengeModifier[] { speed };
            MyTime.time = 2f; typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null); Check(timer.t_timerStage.text == "2:55", "speedrun stage limit");
            typeof(SummonerController).GetField("isFinalSwarmStarted", P).SetValue(summoner, true); MyTime.finalSwarmTimer = 33f; typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null);
            Check(timer.t_timerStage.text == "0:33" && timer.t_timerStage.color == Color.red, "final swarm elapsed time and red color");
            typeof(SummonerController).GetField("isFinalSwarmStarted", P).SetValue(summoner, false); typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null);
            SetProperty(game, "isCrypt", true); SetProperty(game, "isDungeonTimerStarted", false); typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null); Check(timer.t_timerStage.text == "", "crypt waits for timer start");
            SetProperty(game, "isDungeonTimerStarted", true); SetProperty(game, "dungeonTimeToComplete", 60f); MyTime.cryptTimer = 20f; typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null);
            Check(timer.t_timerStage.text == "<size=110%><sprite name=clock></size> 0:40" && timer.t_timerStage.color == Color.red && Mathf.Approximately(timer.t_timerStage.fontSize,50f), "crypt clock, color and size");
            SetProperty(game, "isCrypt", false); typeof(SummonerController).GetField("isFinalSwarmStarted", P).SetValue(summoner, false); MyTime.time = 3f; typeof(GameTimer).GetMethod("FixedUpdate", P).Invoke(timer, null);
            Check(timer.t_timerStage.text == "2:55" && timer.t_timerStage.color == Color.white && Mathf.Approximately(timer.t_timerStage.fontSize,36f), "normal timer style restored");
            names.Clear(); ChallengesTracker.challengeModifiers = null; UnityEngine.Object.DestroyImmediate(speed); UnityEngine.Object.DestroyImmediate(timer.t_timerRun.gameObject); UnityEngine.Object.DestroyImmediate(timer.t_timerStage.gameObject); UnityEngine.Object.DestroyImmediate(timer.t_timerSpeedrun.gameObject);
            Debug.Log("GameTimerPortCheck passed: run clock, speedrun cap, final swarm, crypt and style restoration");
        } finally { GameManager.Instance = null; EnemyManager.Instance = null; ChallengesTracker.challengeModifiers = null; ((HashSet<string>)typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Clear(); UnityEngine.Object.DestroyImmediate(root); }
    }
}
