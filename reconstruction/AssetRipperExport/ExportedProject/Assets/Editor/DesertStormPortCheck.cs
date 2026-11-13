using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class DesertStormPortCheck
{
    public static void Run()
    {
        var root = new GameObject("desert check"); root.SetActive(false);
        var oldEffect = EffectManager.Instance; var oldPool = PoolManager.Instance;
        var stageProperty = typeof(MapController).GetProperty("currentStage"); var oldStage = stageProperty.GetValue(null);
        var flag = typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic); object oldFlag = flag.GetValue(null);
        float oldStageTime = MyTime.stageTimer, oldTime = MyTime.time, oldDelta = MyTime.deltaTime; var randomState = UnityEngine.Random.state; var oldMapRandom = MyRandom.random;
        float fogDensity = RenderSettings.fogDensity; Color fogColor = RenderSettings.fogColor; var oldPlayer = MyPlayer.Instance;
        var stage = ScriptableObject.CreateInstance<StageData>(); stage.stageTimeline = new StageTimeline { stageTime = 600f };
        var tornado = new GameObject("desert-test-tornado"); tornado.SetActive(false);
        var prefab = new GameObject("desert-test-storm"); prefab.SetActive(false); var renderer = prefab.AddComponent<MeshRenderer>();
        var material = new Material(Shader.Find("Sprites/Default")); renderer.sharedMaterial = material;
        var audio = prefab.AddComponent<AudioSource>(); audio.volume = 0.75f;
        var storm = prefab.AddComponent<DesertStorm>(); storm.fogOfWarRenderer = renderer; storm.audio = audio; storm.stormParticles = Array.Empty<ParticleSystem>();
        var spawned = new List<GameObject>();
        try
        {
            var effect = root.AddComponent<EffectManager>(); EffectManager.Instance = effect; effect.tornado = tornado; effect.desertStormFx = prefab;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.tumbleweed = tornado;
            typeof(PoolManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pool, null);
            Require(pool.tumbleweedPool != null, "tumbleweed pool is initialized");
            pool.tumbleweedPool = new ObjectPool<GameObject>(() => { var obj = new GameObject("desert-test-tumbleweed"); obj.SetActive(false); spawned.Add(obj); return obj; });
            stageProperty.GetSetMethod(true).Invoke(null, new object[] { stage }); flag.SetValue(null, false); UnityEngine.Random.InitState(77);
            var events = new MapEventsDesert(); events.Init();
            var times = (List<float>)typeof(MapEventsDesert).GetField("stormTimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events);
            Require(times.Count >= 1 && times.Count <= 3 && times[0] >= 30f, "storm count and initial delay");
            for (int i = 1; i < times.Count; i++) Require(times[i] - times[i - 1] >= 23f, "minimum spacing");
            // Use a predictable three-storm schedule after validating generation,
            // so the later storms and exhaustion are covered regardless of RNG.
            times.Clear(); times.AddRange(new[] { 45f, 90f, 135f });
            typeof(MapEventsDesert).GetField("nextStormTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(events, times[0]);
            int tornadoCount = 0; foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>()) if (obj.name == "desert-test-tornado(Clone)") tornadoCount++;
            Require(tornadoCount == 25, "original tornado count");
            foreach (float stageDuration in new[] { 480f, 540f, 600f })
            foreach (int seed in new[] { 11, 41, 77 })
            {
                stage.stageTimeline.stageTime = stageDuration;
                MyRandom.random = new System.Random(seed); UnityEngine.Random.InitState(seed);
                var sample = new MapEventsDesert(); sample.Init();
                var schedule = (List<float>)typeof(MapEventsDesert).GetField("stormTimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sample);
                Require(schedule.Count >= 1 && schedule.Count <= 3, "desert stage generates one to three storms");
                Require(schedule[0] >= 30f && schedule[schedule.Count - 1] <= stageDuration - 113f + .001f, "storms stay in original scheduling window");
                for (int i = 1; i < schedule.Count; i++) Require(schedule[i] - schedule[i - 1] >= 23f, "generated desert storms retain minimum spacing");
                sample.Cleanup();
            }
            stage.stageTimeline.stageTime = 600f;
            var tick = typeof(MapEventsDesert).GetMethod("TickStorms", BindingFlags.Instance | BindingFlags.NonPublic);
            MyTime.stageTimer = times[0]; tick.Invoke(events, null); Require(!MapEventsDesert.IsActiveStorm(), "no storm exactly at scheduled time");
            MyTime.stageTimer = times[0] + 0.1f; tick.Invoke(events, null); Require(MapEventsDesert.IsActiveStorm() && Mathf.Approximately(MapEventsDesert.currentStormStartedAtTime, MyTime.stageTimer), "storm starts using stage time");
            var instance = effect.GetDesertStorm(); Require(instance.gameObject.activeSelf && instance.fogOfWarRenderer.enabled && Mathf.Approximately(instance.audio.volume, 0f), "storm effect starts");
            var playerObject = new GameObject("desert-test-player"); playerObject.SetActive(false); MyPlayer.Instance = playerObject.AddComponent<MyPlayer>(); playerObject.transform.position = new Vector3(2, 3, 4);
            var update = typeof(DesertStorm).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            MyTime.deltaTime = 1.5f; update.Invoke(instance, null);
            Require(instance.transform.position == playerObject.transform.position && Mathf.Approximately(RenderSettings.fogDensity, Mathf.Lerp(fogDensity, 0.03f, 0.5f)), "storm follows player and fades fog in");
            float over = (float)typeof(MapEventsDesert).GetField("stormOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events);
            Require(over >= MyTime.stageTimer + 8f && over <= MyTime.stageTimer + 18f, "storm duration range");
            MyTime.stageTimer = over; tick.Invoke(events, null); Require(MapEventsDesert.IsActiveStorm(), "storm remains at end boundary");
            MyTime.stageTimer = over + .1f; tick.Invoke(events, null); Require(!MapEventsDesert.IsActiveStorm(), "storm stops after boundary");
            MyTime.deltaTime = 3f; update.Invoke(instance, null);
            Require(!instance.gameObject.activeSelf && Mathf.Approximately(RenderSettings.fogDensity, fogDensity) && RenderSettings.fogColor == fogColor, "storm fade out restores fog");
            MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(playerObject);
            var next = (float)typeof(MapEventsDesert).GetField("nextStormTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events);
            Require(next == times[1], "next storm advances");
            for (int i = 1; i < times.Count; i++)
            {
                MyTime.stageTimer = times[i]; tick.Invoke(events, null);
                Require(!MapEventsDesert.IsActiveStorm(), "later storm waits at scheduled boundary");
                MyTime.stageTimer = times[i] + .1f; tick.Invoke(events, null);
                Require(MapEventsDesert.IsActiveStorm() && Mathf.Approximately(MapEventsDesert.currentStormStartedAtTime, MyTime.stageTimer), "later storm starts once");
                over = (float)typeof(MapEventsDesert).GetField("stormOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events);
                MyTime.stageTimer = over + .1f; tick.Invoke(events, null);
                Require(!MapEventsDesert.IsActiveStorm(), "later storm stops");
                next = (float)typeof(MapEventsDesert).GetField("nextStormTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events);
                Require(next == (i + 1 < times.Count ? times[i + 1] : float.MaxValue), "later storm advances or exhausts schedule");
            }
            MyTime.stageTimer = 599f; tick.Invoke(events, null);
            Require(!MapEventsDesert.IsActiveStorm(), "no storm restarts after schedule exhausted");
            var spawn = typeof(MapEventsDesert).GetMethod("SpawnTumbleWeeds", BindingFlags.Instance | BindingFlags.NonPublic);
            MyTime.time = 2.19f; spawn.Invoke(events, null); Require(spawned.Count == 0, "tumbleweed interval not reached");
            MyTime.time = 2.2f; spawn.Invoke(events, null); Require(spawned.Count == 1 && spawned[0].activeSelf, "tumbleweed spawned at interval");
            events.Cleanup(); Require(!MapEventsDesert.IsActiveStorm(), "cleanup resets storm state");
            Debug.Log("DesertStormPortCheck passed");
        }
        finally
        {
            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>()) if (obj.name == "desert-test-tornado(Clone)" || obj.name == "desert-test-storm(Clone)") UnityEngine.Object.DestroyImmediate(obj);
            foreach (var obj in spawned) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tornado); UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(stage);
            EffectManager.Instance = oldEffect; PoolManager.Instance = oldPool; MyPlayer.Instance = oldPlayer; stageProperty.GetSetMethod(true).Invoke(null, new[] { oldStage }); flag.SetValue(null, oldFlag); MyTime.stageTimer = oldStageTime; MyTime.time = oldTime; MyTime.deltaTime = oldDelta; RenderSettings.fogDensity = fogDensity; RenderSettings.fogColor = fogColor; UnityEngine.Random.state = randomState; MyRandom.random = oldMapRandom;
        }
    }
    static void Require(bool value, string name) { if (!value) throw new Exception("DesertStormPortCheck failed: " + name); }
}
