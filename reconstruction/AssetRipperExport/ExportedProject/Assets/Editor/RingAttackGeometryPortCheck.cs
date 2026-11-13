using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class RingAttackGeometryPortCheck
{
    static void Require(bool v, string s) { if (!v) throw new Exception("RingAttackGeometryPortCheck: " + s); }
    public static void Run()
    {
        var root = new GameObject("ring geometry test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerMovement = root.AddComponent<PlayerMovement>(); var rb = root.AddComponent<Rigidbody>(); rb.velocity = new Vector3(1, 0, 0); typeof(PlayerMovement).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player.playerMovement, rb);
        GameManager.Instance = root.AddComponent<GameManager>(); PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
        var warnings = new List<GameObject>(); PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => { var go = new GameObject("warning"); var w = go.AddComponent<CircleWarning>(); w.filler = new GameObject("fill").transform; w.filler.SetParent(go.transform); warnings.Add(go); return go; });
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Pharaoh1.asset"); var config = data.specialAttacks.First(a => a.attackName == "DesertPillarsCircles");
        var attackObj = UnityEngine.Object.Instantiate(config.attackPrefab); attackObj.SetActive(false); var attack = attackObj.GetComponent<EnemySpecialAttackManyRandom>();
        try
        {
            Require(attack != null && attack.circles == 4 && attack.predictive && config.attackChargeTime == 1f, "original Pharaoh configuration");
            typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(attack, new object[] { config }); attack.grounded = false;
            var routine = (IEnumerator)typeof(EnemySpecialAttackManyRandom).GetMethod("DoAttack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
            Require(routine.MoveNext() && warnings.Count == 1, "center warning"); Require(Vector3.Distance(warnings[0].transform.position, new Vector3(2, 0, 0)) < .001f, "original predictive velocity applied twice");
            Require(routine.MoveNext() && warnings.Count == 8, "first ring has seven warnings"); Require(Vector3.Distance(warnings[1].transform.position, new Vector3(19.5f, 0, 0)) < .001f, "ring radius includes margin");
            Require(routine.MoveNext() && warnings.Count == 21, "second ring has thirteen warnings");
            Require(routine.MoveNext() && warnings.Count == 40, "third ring has nineteen warnings");
            Require(routine.MoveNext() && warnings.Count == 66, "fourth ring has twenty-six warnings");
            Require(!routine.MoveNext(), "attack completes after final wait");
            Debug.Log("RingAttackGeometryPortCheck passed: original predictive center, four radii and warning counts");
        }
        finally { foreach (var w in warnings) UnityEngine.Object.DestroyImmediate(w); UnityEngine.Object.DestroyImmediate(attackObj); UnityEngine.Object.DestroyImmediate(root); MyPlayer.Instance = null; GameManager.Instance = null; PoolManager.Instance = null; EffectManager.Instance = null; }
    }
}
