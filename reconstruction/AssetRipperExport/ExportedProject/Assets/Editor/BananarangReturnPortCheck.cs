using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public static class BananarangReturnPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);
    static void Require(bool value, string name) { if (!value) throw new Exception("BananarangReturnPortCheck: " + name); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; float oldTime = MyTime.time, oldFixed = MyTime.fixedDeltaTime;
        var playerObject = new GameObject("banana test player"); playerObject.SetActive(false); var projectileObject = new GameObject("banana test projectile"); projectileObject.SetActive(false);
        try
        {
            MyPlayer.Instance = playerObject.AddComponent<MyPlayer>(); playerObject.transform.position = Vector3.right * 10f;
            var banana = projectileObject.AddComponent<ProjectileBanana>();
            Require(Mathf.Abs(Get<float>(banana, "hitCooldown") - .24f) < .0001f, "original enemy cooldown");
            Set(banana, "readyToCollectTime", 100f); Set(banana, "nextCheckDamageTime", 100f); Set(banana, "startDirection", Vector3.forward); Set(banana, "maxSpeed", 55f); Set(banana, "returnTime", 0f);
            MyTime.time = 0f; MyTime.fixedDeltaTime = .5f; Call(banana, "MyFixedUpdate");
            Require(Vector3.Distance(Get<Vector3>(banana, "dirToPlayer"), Vector3.right) < .001f, "direction toward player");
            Require(Mathf.Abs(Get<float>(banana, "returnTime") - .35f) < .001f, "return blend advances with game physics time");
            Require(Vector3.Distance(Get<Vector3>(banana, "movementVelocity"), new Vector3(.35f, 0, .65f) * 55f) < .001f, "outward arc blends toward player");
            MyTime.fixedDeltaTime = 2f; Call(banana, "MyFixedUpdate"); Require(Get<float>(banana, "returnTime") == 1f && Vector3.Distance(Get<Vector3>(banana, "movementVelocity"), Vector3.right * 55f) < .001f, "return blend caps at player direction");
            Debug.Log("BananarangReturnPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(projectileObject); MyPlayer.Instance = oldPlayer; MyTime.time = oldTime; MyTime.fixedDeltaTime = oldFixed; }
    }
}
