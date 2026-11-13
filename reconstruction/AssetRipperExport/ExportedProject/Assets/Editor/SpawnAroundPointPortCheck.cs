using System;
using System.Reflection;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

public static class SpawnAroundPointPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var old = GameManager.Instance;
        var managerObject = new GameObject("spawn test manager"); managerObject.SetActive(false);
        var manager = managerObject.AddComponent<GameManager>(); GameManager.Instance = manager;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            int groundLayer = LayerMask.NameToLayer("Ground"), waterLayer = LayerMask.NameToLayer("Water");
            Require(groundLayer >= 0 && waterLayer >= 0, "ground and water layers exist");
            floor.layer = groundLayer; floor.transform.position = new Vector3(12, -1, -7); floor.transform.localScale = new Vector3(100, 2, 100);
            water.layer = waterLayer; water.transform.position = new Vector3(12, 1, -7); water.transform.localScale = new Vector3(100, .2f, 100); water.SetActive(false);
            manager.whatIsGroundAndObjects = 1 << groundLayer;
            Physics.SyncTransforms();
            Vector3 center = new Vector3(12, 12, -7);
            Require(SpawnPositions.GetPositionAroundPoint(center, 2, 4, .2f, 0) == center, "zero attempts returns original center");
            Vector3 objectPos = SpawnPositions.GetObjectSpawnPosition(new Vector3(12, 0, -7), Vector3.zero, .2f, 1 << groundLayer, out Vector3 objectNormal, 1);
            Require(Mathf.Abs(objectPos.y) < .01f && objectNormal.y > .99f, "object spawn on ground");
            objectPos = SpawnPositions.GetObjectSpawnPosition(center, Vector3.zero, .2f, 1 << groundLayer, out objectNormal, 0);
            Require(objectPos == SpawnPositions.INVALID_POS && objectNormal == Vector3.zero, "object spawn zero attempts");
            UnityEngine.Random.InitState(90017);
            for (int i = 0; i < 50; i++)
            {
                Vector3 result = SpawnPositions.GetPositionAroundPoint(center, 2, 4, .2f, 1);
                float radius = new Vector2(result.x - center.x, result.z - center.z).magnitude;
                Require(Mathf.Abs(result.y) < .01f && radius >= 1.999f && radius <= 4.001f, "sampled floor position and distance");
            }
            floor.tag = "Ignore"; Physics.SyncTransforms();
            Require(SpawnPositions.GetPositionAroundPoint(center, 2, 4, .2f, 3) == center, "ignored collider returns center");
            objectPos = SpawnPositions.GetObjectSpawnPosition(center, Vector3.zero, .2f, 1 << groundLayer, out objectNormal, 1);
            Require(objectPos == SpawnPositions.INVALID_POS && objectNormal == Vector3.zero, "object spawn ignores tagged ground");
            floor.tag = "Untagged";
            water.SetActive(true); Physics.SyncTransforms();
            Require(SpawnPositions.GetPositionAroundPoint(center, 2, 4, .2f, 3) == center, "water blocks sample");
            objectPos = SpawnPositions.GetObjectSpawnPosition(center, Vector3.zero, .2f, 1 << groundLayer, out objectNormal, 1);
            Require(objectPos == SpawnPositions.INVALID_POS && objectNormal == Vector3.zero, "object spawn blocked by water");
            objectPos = SpawnPositions.GetObjectSpawnPosition(center, Vector3.zero, .2f, 1 << groundLayer, out objectNormal, 1, true, false, true);
            Require(Mathf.Abs(objectPos.y) < .01f && objectNormal.y > .99f, "object spawn permitted under water");
            var method = typeof(SpawnPositions).GetMethod("TryGetPosition", BindingFlags.Static | BindingFlags.NonPublic);
            object[] args = { new Vector3(12, 20, -7), .2f, 1 << groundLayer, Vector3.zero, Vector3.zero, true, true, 90f };
            Require((bool)method.Invoke(null, args), "water allowed");
            Require(Mathf.Abs(((Vector3)args[3]).y) < .01f && ((Vector3)args[4]).y > .99f, "hit position and normal");
            water.SetActive(false); floor.layer = 0; Physics.SyncTransforms();
            args = new object[] { new Vector3(12, 20, -7), .2f, 1, Vector3.zero, Vector3.zero, true, true, 90f };
            Require(!(bool)method.Invoke(null, args), "ground-only rejects other layers");
            args[5] = false; Require((bool)method.Invoke(null, args), "non-ground layer allowed");
            floor.layer = groundLayer; floor.transform.rotation = Quaternion.Euler(0, 0, 45); Physics.SyncTransforms();
            args = new object[] { new Vector3(12, 30, -7), .2f, 1 << groundLayer, Vector3.zero, Vector3.zero, true, true, 20f };
            Require(!(bool)method.Invoke(null, args), "steep slope rejected");
            args[7] = 60f; Require((bool)method.Invoke(null, args), "slope within limit accepted");
            Debug.Log("SpawnAroundPointPortCheck passed: 50 ground samples, fallback, ignored tag, water, layer and slope gates; real object spawn ground, failure, ignore and water");
        }
        finally { GameManager.Instance = old; UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(water); UnityEngine.Object.DestroyImmediate(managerObject); }
    }
}
