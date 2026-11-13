using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class RootAttackPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("RootAttackPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("root attack test"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(0, 0, 20);
        GameManager.Instance = root.AddComponent<GameManager>(); GameManager.Instance.whatIsPlayer = 0;
        PoolManager.Instance = root.AddComponent<PoolManager>(); EffectManager.Instance = root.AddComponent<EffectManager>();
        var warningObjects = new List<GameObject>();
        PoolManager.Instance.warningSpherePool = new ObjectPool<GameObject>(() => { var obj = new GameObject("test warning"); var warning = obj.AddComponent<CircleWarning>(); warning.filler = new GameObject("filler").transform; warning.filler.SetParent(obj.transform); warningObjects.Add(obj); return obj; });
        var enemyObj = new GameObject("test enemy"); enemyObj.SetActive(false); var enemy = enemyObj.AddComponent<Enemy>(); enemy.renderer = enemyObj.AddComponent<MeshRenderer>();
        var ent = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage1.asset"); Check(ent != null, "Ent asset");
        var config = ent.specialAttacks.First(a => a.attackName == "RootAttackLines3");
        var attackObj = UnityEngine.Object.Instantiate(config.attackPrefab); attackObj.SetActive(false); var attack = attackObj.GetComponent<EnemySpecialAttackPrefabMulti>();
        try
        {
            Check(attack != null && attack.numLines == 3 && attack.numHits == 6 && config.attackRadius == 4f, "original prefab and settings");
            typeof(EnemySpecialAttackPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attack, enemy);
            typeof(EnemySpecialAttackPrefab).GetProperty("specialAttack").GetSetMethod(true).Invoke(attack, new object[] { config });
            attack.grounded = false;
            var routine = (IEnumerator)typeof(EnemySpecialAttackPrefabMulti).GetMethod("DoAttack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
            Check(routine.MoveNext() && warningObjects.Count == 3, "first group creates three warnings");
            Check(Mathf.Abs(warningObjects[0].transform.position.x + 2f) < .001f && Mathf.Abs(warningObjects[1].transform.position.z - 4f) < .001f && Mathf.Abs(warningObjects[2].transform.position.x - 2f) < .001f, "three lines follow original 30-degree spread");
            Check(routine.Current is IEnumerator, "each group waits game time");
            Check(routine.MoveNext() && warningObjects.Count == 6, "second group created");
            Check(Mathf.Abs(warningObjects[4].transform.position.z - 11.2f) < .001f, "radius and spacing");
            var callback = (Action)typeof(CircleWarning).GetField("finishAction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(warningObjects[1].GetComponent<CircleWarning>()); callback();
            Check((int)typeof(EnemySpecialAttackPrefabMulti).GetField("numSpawned", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(attack) == 1, "warning triggers pooled impact");
            Debug.Log("RootAttackPortCheck passed: Ent prefab, multi-line warning geometry, timed groups and impact callback");
        }
        finally
        {
            foreach (var warning in warningObjects) UnityEngine.Object.DestroyImmediate(warning);
            foreach (var effect in Resources.FindObjectsOfTypeAll<EnemyAttackEffectPrefab>()) if (effect.gameObject.name.EndsWith("(Clone)")) UnityEngine.Object.DestroyImmediate(effect.gameObject);
            UnityEngine.Object.DestroyImmediate(attackObj); UnityEngine.Object.DestroyImmediate(enemyObj); UnityEngine.Object.DestroyImmediate(root);
            MyPlayer.Instance = null; GameManager.Instance = null; PoolManager.Instance = null; EffectManager.Instance = null;
        }
    }
}
