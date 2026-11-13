using System;
using System.Reflection;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyParkourMultiplierPortCheck
{
    public static void Run()
    {
        var root = new GameObject("enemy parkour multiplier fixture"); root.SetActive(false);
        var previousManager = GameManager.Instance; float previousTimer = MyTime.cryptTimer;
        try
        {
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            typeof(GameManager).GetProperty("dungeonTimeToComplete").GetSetMethod(true).Invoke(manager, new object[] { 60f });
            var method = typeof(EnemyStats).GetMethod("GetParkourMultiplier", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var test in new[] { (0f, 0f), (59f, 0f), (60f, 0f), (61f, .2f), (90f, 6f), (180f, 24f) })
            {
                MyTime.cryptTimer = test.Item1;
                float actual = (float)method.Invoke(null, null);
                if (Mathf.Abs(actual - test.Item2) > .0001f) throw new Exception($"time {test.Item1}: got {actual}, expected {test.Item2}");
            }
            Debug.Log("EnemyParkourMultiplierPortCheck passed: crypt deadline and overtime scaling boundaries");
        }
        finally { MyTime.cryptTimer = previousTimer; GameManager.Instance = previousManager; UnityEngine.Object.DestroyImmediate(root); }
    }
}
