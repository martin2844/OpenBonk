using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public static class TornadoPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
    static void Require(bool condition, string message) { if (!condition) throw new Exception("TornadoPortCheck: " + message); }
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPlayer = MyPlayer.Instance;
        float oldTime = MyTime.time, oldFixed = MyTime.fixedDeltaTime; bool oldPaused = MyTime.paused;
        var randomState = UnityEngine.Random.state;
        var managerObj = new GameObject("tornado-test-manager"); managerObj.SetActive(false);
        var playerObj = new GameObject("tornado-test-player"); playerObj.SetActive(false);
        var tornadoObj = new GameObject("tornado-test"); tornadoObj.SetActive(false);
        try
        {
            var manager = managerObj.AddComponent<GameManager>(); GameManager.Instance = manager;
            manager.whatIsPlayer = 1 << 0;
            var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var movement = playerObj.AddComponent<PlayerMovement>(); player.playerMovement = movement;
            var playerBody = playerObj.AddComponent<Rigidbody>(); playerBody.useGravity = false;
            playerObj.AddComponent<SphereCollider>(); Set(movement, "rb", playerBody);
            var tornado = tornadoObj.AddComponent<Tornado>(); tornado.rb = tornadoObj.AddComponent<Rigidbody>();
            tornado.rb.useGravity = false; tornado.audio = tornadoObj.AddComponent<AudioSource>();
            Require(Mathf.Approximately(tornado.force, .85f) && Mathf.Approximately(Get<float>(tornado, "fadeTime"), 2f), "original defaults");
            Set(tornado, "speed", 8f); UnityEngine.Random.InitState(12); Call(tornado, "FindNewDir");
            var direction = Get<Vector3>(tornado, "desiredVelocity");
            Require(Mathf.Abs(direction.y) < .0001f && Mathf.Abs(direction.magnitude - 8f) < .001f, "horizontal travel direction and speed");
            Set(tornado, "defaultScale", new Vector3(2, 4, 6)); Set(tornado, "defaultVolume", .8f);
            Set(tornado, "startTime", 10f); Set(tornado, "stopTime", 30f);
            MyTime.time = 11f; Call(tornado, "Update");
            Require(Vector3.Distance(tornado.transform.localScale, new Vector3(1, 2, 3)) < .001f && Mathf.Abs(tornado.audio.volume - .4f) < .001f, "fade in");
            MyTime.time = 29f; Call(tornado, "Update");
            Require(Vector3.Distance(tornado.transform.localScale, new Vector3(1, 2, 3)) < .001f && Mathf.Abs(tornado.audio.volume - .4f) < .001f, "fade out");
            playerObj.SetActive(true); tornadoObj.SetActive(true); Physics.SyncTransforms();
            Set(tornado, "scaleMultiplier", 1f); Set(tornado, "desiredVelocity", new Vector3(3, 0, 4));
            tornado.rb.velocity = new Vector3(0, 2, 0); MyTime.fixedDeltaTime = .02f; MyTime.paused = false; MyTime.time = 15f;
            Call(tornado, "FixedUpdate");
            Require(movement.isTouchingTornado && Get<float>(movement, "lastTouchedTornadoTime") == 15f, "player contact and timestamp");
            Require(Vector3.Distance(tornado.rb.velocity, new Vector3(3, 2, 4)) < .001f, "horizontal movement preserves vertical speed");
            MyTime.paused = true; MyTime.time = 16f; Call(tornado, "FixedUpdate");
            Require(Get<float>(movement, "lastTouchedTornadoTime") == 15f, "paused tornado does not affect player");
            Debug.Log("TornadoPortCheck passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tornadoObj); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(managerObj);
            GameManager.Instance = oldManager; MyPlayer.Instance = oldPlayer; MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixed; MyTime.paused = oldPaused; UnityEngine.Random.state = randomState;
        }
    }
}
