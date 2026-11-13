using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemySpawnBoundsPortCheck
{
    const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    static void Near(float value, float expected, string name) { if (Mathf.Abs(value - expected) > .001f) throw new Exception(name + ": " + value + " expected " + expected); }
    static float Field(string name) => (float)typeof(SpawnPositions).GetField(name, StaticPrivate).GetValue(null);
    static void Bounds(float minX, float maxX, float minZ, float maxZ)
    {
        typeof(SpawnPositions).GetMethod("FindBounds", StaticPrivate).Invoke(null, null);
        Near(Field("minX"), minX, "min X"); Near(Field("maxX"), maxX, "max X");
        Near(Field("minZ"), minZ, "min Z"); Near(Field("maxZ"), maxZ, "max Z");
    }
    static void Distances(float time, float min, float max)
    {
        MyTime.runTimer = time;
        object[] args = { 0f, 0f };
        typeof(SpawnPositions).GetMethod("GetSpawnDistances", StaticPrivate).Invoke(null, args);
        Near((float)args[0], min, "minimum spawn distance"); Near((float)args[1], max, "maximum spawn distance");
    }
    public static void Run()
    {
        var root = new GameObject("enemy spawn bounds test"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldManager = GameManager.Instance;
        var oldLower = MapInfo.mapBoundsLower; var oldUpper = MapInfo.mapBoundsUpper; float oldTime = MyTime.runTimer;
        var player = root.AddComponent<MyPlayer>(); var manager = root.AddComponent<GameManager>();
        try
        {
            MyPlayer.Instance = player; GameManager.Instance = manager;
            MapInfo.mapBoundsLower = new Vector3(-100, -20, -100); MapInfo.mapBoundsUpper = new Vector3(100, 20, 100);
            player.transform.position = Vector3.zero; Bounds(-98, 98, -98, 98);
            player.transform.position = new Vector3(200, 0, -200); Bounds(-98, 175, -175, 98);
            player.transform.position = new Vector3(-200, 0, 200); Bounds(-175, 98, -98, 175);
            typeof(GameManager).GetProperty("isCrypt").GetSetMethod(true).Invoke(manager, new object[] { true });
            Bounds(-9999, 9999, -9999, 9999);
            Distances(-60, 20, 40); Distances(0, 20, 40); Distances(750, 30, 60); Distances(1500, 40, 80); Distances(3000, 40, 80);
            Debug.Log("EnemySpawnBoundsPortCheck passed: map bounds, outside positions, crypt and time-scaled distance caps");
        }
        finally
        {
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldManager;
            MapInfo.mapBoundsLower = oldLower; MapInfo.mapBoundsUpper = oldUpper; MyTime.runTimer = oldTime;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
