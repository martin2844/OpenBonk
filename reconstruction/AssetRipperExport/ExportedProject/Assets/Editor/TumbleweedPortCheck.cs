using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class TumbleweedPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Require(bool condition, string message) { if (!condition) throw new Exception("TumbleweedPortCheck: " + message); }
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPool = PoolManager.Instance; var oldPlayer = MyPlayer.Instance;
        var oldCenter = MapInfo.mapCenter; var oldSize = MapInfo.mapSize; var oldRandom = UnityEngine.Random.state;
        float oldTime = MyTime.time, oldFixed = MyTime.fixedDeltaTime; bool oldPaused = MyTime.paused;
        var root = new GameObject("tumbleweed test root"); root.SetActive(false);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "test ground"; ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = new Vector3(0, -1, 0); ground.transform.localScale = new Vector3(200, 2, 200);
        var weedObj = new GameObject("test tumbleweed"); weedObj.SetActive(false);
        var effect = new GameObject("test break effect"); effect.SetActive(false);
        try
        {
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            manager.whatIsGroundAndObjects = 1 << ground.layer; manager.whatIsGround = 1 << ground.layer;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.tumbleweedPool = new ObjectPool<GameObject>(() => weedObj, x => x.SetActive(true), x => x.SetActive(false));
            pool.tumbleweedBreakPool = new ObjectPool<GameObject>(() => effect, x => x.SetActive(true), x => x.SetActive(false));
            var weed = weedObj.AddComponent<InteractableTumbleWeed>(); weed.rb = weedObj.AddComponent<Rigidbody>(); weed.rb.useGravity = false;
            MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100, 10, 100); MyTime.time = 100f; MyTime.fixedDeltaTime = .02f; MyTime.paused = false;
            Physics.SyncTransforms(); pool.tumbleweedPool.Get(); Call(weed, "OnEnable");
            Require(Mathf.Abs(weed.transform.position.y - 1f) < .01f, "spawn above map ground");
            Require(Get<float>(weed, "speed") >= 4f && Get<float>(weed, "speed") <= 10f, "speed range");
            var velocity = Get<Vector3>(weed, "desiredVelocity");
            Require(velocity.y == 0f && Mathf.Abs(velocity.magnitude - Get<float>(weed, "speed")) < .001f, "horizontal direction");
            Require(Get<float>(weed, "stopTime") >= 145f && Get<float>(weed, "stopTime") <= 160f, "lifetime range");
            Set(weed, "desiredVelocity", new Vector3(3, 0, 4)); weed.rb.velocity = new Vector3(0, 2, 0); Set(weed, "lastPos", new Vector3(weed.rb.position.x, 0, weed.rb.position.z));
            Call(weed, "FixedUpdate"); Require(Vector3.Distance(weed.rb.velocity, new Vector3(3, 2, 4)) < .001f, "movement preserves vertical speed");
            MyTime.paused = true; weed.rb.velocity = Vector3.zero; Call(weed, "FixedUpdate"); Require(weed.rb.velocity == Vector3.zero, "pause stops updates");
            MyTime.paused = false; player.transform.position = weed.transform.position; MyTime.time = Get<float>(weed, "stopTime") + .1f; Call(weed, "FixedUpdate");
            Require(!weedObj.activeSelf && pool.tumbleweedPool.CountInactive == 1, "expired weed returns to pool");
            Require(effect.activeSelf && effect.transform.position == weed.transform.position, "nearby break effect");
            Set(weed, "broken", true); Require(!weed.Interact(), "broken weed rejects repeated interaction");
            Debug.Log("TumbleweedPortCheck passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(weedObj); UnityEngine.Object.DestroyImmediate(effect);
            GameManager.Instance = oldManager; PoolManager.Instance = oldPool; MyPlayer.Instance = oldPlayer; MapInfo.mapCenter = oldCenter; MapInfo.mapSize = oldSize; UnityEngine.Random.state = oldRandom; MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixed; MyTime.paused = oldPaused;
        }
    }
}
