using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class FollowingAttackMotionPortCheck
{
    static void Require(bool v, string m) { if (!v) throw new Exception("FollowingAttackMotionPortCheck: " + m); }
    public static void Run()
    {
        var root = new GameObject("following test"); root.SetActive(false); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerMovement = root.AddComponent<PlayerMovement>(); var rb = root.AddComponent<Rigidbody>(); rb.velocity = Vector3.right * 2; typeof(PlayerMovement).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player.playerMovement, rb);
        GameManager.Instance = root.AddComponent<GameManager>(); PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
        var warnings = new List<GameObject>(); PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => { var go = new GameObject("warning"); var w = go.AddComponent<CircleWarning>(); w.filler = new GameObject("fill").transform; w.filler.SetParent(go.transform); warnings.Add(go); return go; });
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage1.asset"); var config = data.specialAttacks.First(a => a.attackName == "RootAttackContinous");
        var attackObj = UnityEngine.Object.Instantiate(config.attackPrefab); attackObj.SetActive(false); var attack = attackObj.GetComponent<EnemySpecialAttackFollowing>(); float previousDelta = MyTime.deltaTime;
        var previousMap = MapController.currentMap;
        var mapSetter = typeof(MapController).GetProperty("currentMap").GetSetMethod(true);
        var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.Tiles;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            Require(attack != null && attack.numHits == 15 && Mathf.Abs(attack.delayBetweenHits - .6f) < .001f, "original Ent prefab");
            typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(attack, new object[] { config }); attack.grounded = false; attack.predictive = true; MyTime.deltaTime = .3f;
            var routine = (IEnumerator)typeof(EnemySpecialAttackFollowing).GetMethod("DoAttack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
            Require(routine.MoveNext() && warnings.Count == 1, "immediate first warning"); Require(Vector3.Distance(warnings[0].transform.position, Vector3.right * 1.8f) < .001f, "velocity-based prediction");
            player.transform.position = Vector3.forward * 5; Require(routine.MoveNext() && warnings.Count == 1, "delay before next warning");
            Require(routine.MoveNext() && warnings.Count == 2, "second warning after delay"); Require(Vector3.Distance(warnings[1].transform.position, Vector3.forward * 5 + Vector3.right * 1.8f) < .001f, "new warning tracks moved player");
            mapSetter.Invoke(null, new object[] { map });
            attack.grounded = true; attack.predictive = false;
            player.transform.position = new Vector3(0, 3, 5);
            floor.transform.position = new Vector3(0, 0, 5); floor.tag = "Ignore"; Physics.SyncTransforms();
            GameManager.Instance.whatIsGround = 0; GameManager.Instance.whatIsObjects = 1;
            routine = (IEnumerator)typeof(EnemySpecialAttackFollowing).GetMethod("DoAttack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
            Require(routine.MoveNext() && warnings.Count == 3 && warnings[2].transform.position == Vector3.zero, "no ray hits use original default hit point");
            GameManager.Instance.whatIsGround = 1; GameManager.Instance.whatIsObjects = 0;
            routine = (IEnumerator)typeof(EnemySpecialAttackFollowing).GetMethod("DoAttack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
            Require(routine.MoveNext() && warnings.Count == 4 && Vector3.Distance(warnings[3].transform.position, new Vector3(0, .5f, 5)) < .001f, "filtered tiled hit uses original first-hit fallback");
            floor.tag = "Untagged";
            routine = (IEnumerator)typeof(EnemySpecialAttackFollowing).GetMethod("DoAttack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
            Require(routine.MoveNext() && warnings.Count == 5 && Vector3.Distance(warnings[4].transform.position, new Vector3(0, .5f, 5)) < .001f, "valid tiled ground hit");
            Debug.Log("FollowingAttackMotionPortCheck passed: prediction, cadence, moving-player targeting and grounded hit fallbacks");
        }
        finally { mapSetter.Invoke(null, new object[] { previousMap }); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(floor); MyTime.deltaTime = previousDelta; foreach (var warning in warnings) UnityEngine.Object.DestroyImmediate(warning); UnityEngine.Object.DestroyImmediate(attackObj); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; PoolManager.Instance = null; EffectManager.Instance = null; }
    }
}
