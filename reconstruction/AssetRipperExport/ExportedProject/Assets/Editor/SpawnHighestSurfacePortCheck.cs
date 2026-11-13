using System;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public static class SpawnHighestSurfacePortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static RaycastHit Hit(GameObject obj)
    {
        Check(obj.GetComponent<Collider>().Raycast(new Ray(obj.transform.position + Vector3.up * 30, Vector3.down), out RaycastHit hit, 60), "fixture raycast");
        return hit;
    }
    public static void Run()
    {
        var setter = typeof(MapController).GetProperty("currentMap").GetSetMethod(true);
        var old = MapController.currentMap;
        var map = ScriptableObject.CreateInstance<MapData>(); map.mapType = EMapType.ProceduralMesh;
        var low = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var high = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var equal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var ignore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var playerObject = new GameObject("surface selection player"); playerObject.SetActive(false);
        var oldPlayer = MyPlayer.Instance;
        try
        {
            setter.Invoke(null, new object[] { map });
            low.transform.position = new Vector3(0, 0, 0); high.transform.position = new Vector3(5, 10, 0);
            equal.transform.position = new Vector3(10, 10, 0); ignore.transform.position = new Vector3(15, 20, 0); ignore.tag = "Ignore";
            Physics.SyncTransforms();
            RaycastHit l = Hit(low), h = Hit(high), e = Hit(equal), x = Hit(ignore);
            RaycastHit result = SpawnPositions.FindHitClosestToPlayerY(new[] { h, l }, out bool found);
            Check(found && result.collider == high.GetComponent<Collider>(), "highest must win when lower surface follows");
            result = SpawnPositions.FindHitClosestToPlayerY(new[] { l, x, h }, out found);
            Check(found && result.collider == high.GetComponent<Collider>(), "ignored higher surface and unordered hits");
            result = SpawnPositions.FindHitClosestToPlayerY(new[] { h, e }, out found);
            Check(found && result.collider == high.GetComponent<Collider>(), "first surface wins equal heights");
            result = SpawnPositions.FindHitClosestToPlayerY(new[] { x }, out found);
            Check(!found && result.collider == null, "no valid surface");
            MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
            map.mapType = EMapType.Tiles;
            result = SpawnPositions.FindHitClosestToPlayerY(new[] { x }, out found);
            Check(!found && result.collider == ignore.GetComponent<Collider>(), "tiled-map ignored hit remains fallback with found false");
            result = SpawnPositions.FindHitClosestToPlayerY(Array.Empty<RaycastHit>(), out found);
            Check(!found && result.collider == null, "tiled-map empty hits");
            result = SpawnPositions.FindHitClosestToPlayerY(new[] { x, h, l }, out found);
            Check(found && result.collider == low.GetComponent<Collider>(), "tiled-map selects closest valid height");
            Debug.Log("SpawnHighestSurfacePortCheck passed: procedural highest and tiled-map fallback and closest surface");
        }
        finally
        {
            setter.Invoke(null, new object[] { old });
            UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(low); UnityEngine.Object.DestroyImmediate(high);
            UnityEngine.Object.DestroyImmediate(equal); UnityEngine.Object.DestroyImmediate(ignore);
            MyPlayer.Instance = oldPlayer; UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }
}
