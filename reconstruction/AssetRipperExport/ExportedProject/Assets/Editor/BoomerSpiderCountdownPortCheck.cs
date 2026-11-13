using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class BoomerSpiderCountdownPortCheck
{
    public static void Run()
    {
        var oldRandom = UnityEngine.Random.state;
        float oldDelta = MyTime.deltaTime;
        var oldPlayer = MyPlayer.Instance;
        var oldPool = PoolManager.Instance;
        var managerObject = new GameObject("countdown manager"); managerObject.SetActive(false);
        var effectManager = managerObject.AddComponent<EffectManager>();
        effectManager.americanExplosionEffectStart = new GameObject("start effect");
        var poolObject = new GameObject("countdown pool"); poolObject.SetActive(false);
        PoolManager.Instance = poolObject.AddComponent<PoolManager>();
        PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => {
            var obj = new GameObject("warning");
            obj.AddComponent<CircleWarning>().filler = new GameObject("filler").transform;
            return obj;
        });
        var playerObject = new GameObject("countdown player"); playerObject.SetActive(false);
        var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.statusEffects = new PlayerStatusEffects();
        var actor = new GameObject("spider");
        var enemy = actor.AddComponent<Enemy>();
        enemy.renderer = actor.AddComponent<MeshRenderer>();
        try
        {
            bool sawPositive = false, sawNegative = false;
            for (int seed = 1; seed <= 8; seed++)
            {
                actor.transform.localScale = Vector3.one; actor.transform.rotation = Quaternion.identity;
                UnityEngine.Random.InitState(seed);
                int selection = UnityEngine.Random.Range(0, 2);
                if (selection == 0) sawPositive = true; else sawNegative = true;
                UnityEngine.Random.InitState(seed);
                MyTime.deltaTime = .1f;
                var routine = (IEnumerator)typeof(EffectManager).GetMethod("ExploderDeath", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(effectManager, new object[] { enemy });
                if (!routine.MoveNext()) throw new Exception("Countdown ended before its first frame");
                if (Quaternion.Angle(actor.transform.rotation, Quaternion.identity) > .001f) throw new Exception("Spider rotated at exactly 0.1 seconds");
                if (Mathf.Abs(actor.transform.localScale.x - (1f + .5f * (.1f / .75f))) > .0001f) throw new Exception("Incorrect countdown scale");
                MyTime.deltaTime = .01f;
                if (!routine.MoveNext()) throw new Exception("Countdown ended before its second frame");
                float expectedYaw = selection == 0 ? 44f : -44f;
                if (Quaternion.Angle(actor.transform.rotation, Quaternion.Euler(0, expectedYaw, 0)) > .01f) throw new Exception("Incorrect spin for random selection " + selection + ": " + actor.transform.rotation.eulerAngles.y);
            }
            if (!sawPositive || !sawNegative) throw new Exception("Random seeds did not cover both spin directions");
            Debug.Log("BoomerSpiderCountdownPortCheck passed");
        }
        finally
        {
            MyTime.deltaTime = oldDelta; UnityEngine.Random.state = oldRandom;
            MyPlayer.Instance = oldPlayer; PoolManager.Instance = oldPool;
            UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(poolObject);
            UnityEngine.Object.DestroyImmediate(effectManager.americanExplosionEffectStart); UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
