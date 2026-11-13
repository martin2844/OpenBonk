using System;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Objects.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class EnemyStatusSymbolsPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("EnemyStatusSymbolsPortCheck: " + message); }
    public static void Run()
    {
        var previous = PoolManager.Instance;
        var managerObject = new GameObject("symbol pool"); managerObject.SetActive(false);
        var enemyObject = new GameObject("symbol enemy"); enemyObject.SetActive(false);
        GameObject instance = null;
        try
        {
            var poolManager = managerObject.AddComponent<PoolManager>(); PoolManager.Instance = poolManager;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/EnemyStatusSymbols.prefab");
            Require(prefab != null, "original prefab exists");
            poolManager.enemyStatusSymbolsPool = new ObjectPool<GameObject>(() => instance = UnityEngine.Object.Instantiate(prefab), null, x => x.SetActive(false), null, true, 10, 100);
            var enemy = enemyObject.AddComponent<Enemy>();
            var mesh = new GameObject("mesh"); mesh.transform.SetParent(enemyObject.transform); mesh.transform.position = new Vector3(2, 3, 4);
            enemy.renderer = mesh.AddComponent<MeshRenderer>();
            typeof(Enemy).GetField("<meshHeight>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 4f);
            var flag = typeof(Enemy).GetField("enemyFlag", BindingFlags.NonPublic | BindingFlags.Instance);
            var check = typeof(Enemy).GetMethod("CheckStatusSymbols", BindingFlags.NonPublic | BindingFlags.Instance);
            flag.SetValue(enemy, EEnemyFlag.Elite | EEnemyFlag.Boss | EEnemyFlag.Challenge);
            check.Invoke(enemy, null);
            var symbols = instance.GetComponent<EnemyStatusSymbols>();
            Require(symbols != null && instance.activeSelf && instance.transform.parent == mesh.transform, "marker attached");
            Require(Vector3.Distance(instance.transform.position, new Vector3(2, 7.5f, 4)) < 0.001f, "marker height");
            Require(symbols.elite.activeSelf && symbols.boss.activeSelf && symbols.challenge.activeSelf && symbols.bossMinimapIcon.activeSelf, "all flags enabled");
            float spacing = symbols.size + symbols.padding;
            Require(Mathf.Abs(symbols.elite.transform.localPosition.x + spacing) < .001f && Mathf.Abs(symbols.boss.transform.localPosition.x) < .001f && Mathf.Abs(symbols.challenge.transform.localPosition.x - spacing) < .001f, "three icons centered");
            flag.SetValue(enemy, EEnemyFlag.Elite | EEnemyFlag.Challenge); check.Invoke(enemy, null);
            Require(!symbols.boss.activeSelf && !symbols.bossMinimapIcon.activeSelf && Mathf.Abs(symbols.elite.transform.localPosition.x + spacing * .5f) < .001f && Mathf.Abs(symbols.challenge.transform.localPosition.x - spacing * .5f) < .001f, "two icons centered and boss hidden");
            flag.SetValue(enemy, EEnemyFlag.None); check.Invoke(enemy, null);
            Require(!instance.activeSelf && instance.transform.parent == null && poolManager.enemyStatusSymbolsPool.CountInactive == 1, "ordinary enemy returns marker");
            flag.SetValue(enemy, EEnemyFlag.StageBoss); enemy.MakeChallenge();
            Require(instance.activeSelf && symbols.boss.activeSelf && !symbols.elite.activeSelf && !symbols.challenge.activeSelf && symbols.bossMinimapIcon.activeSelf, "pooled marker reused for stage boss");
            Debug.Log("EnemyStatusSymbolsPortCheck passed: marker placement, flags, centering and reuse");
        }
        finally
        {
            PoolManager.Instance = previous;
            UnityEngine.Object.DestroyImmediate(enemyObject);
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
