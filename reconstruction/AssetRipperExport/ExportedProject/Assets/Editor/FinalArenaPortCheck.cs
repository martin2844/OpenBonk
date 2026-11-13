using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using UnityEditor;
using UnityEngine;

public static class FinalArenaPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("FinalArenaPortCheck: " + message); }
    static void Field(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static void Near(Vector3 a, Vector3 b, string message) => Check(Vector3.Distance(a, b) < .001f, message + ": " + a);
    public static void Run()
    {
        var root = new GameObject("final arena fixture"); root.SetActive(false);
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageForest3.asset");
        Check(map != null && stage != null, "original forest data available");
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
        typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
        var manager = root.AddComponent<GameManager>(); manager.player = root.AddComponent<MyPlayer>(); manager.playerCamera = root.AddComponent<PlayerCamera>(); manager.uiManager = root.AddComponent<UiManager>();
        Field(manager, "inited", true); Field(manager.player, "inited", true); Field(manager.playerCamera, "inited", true); Field(manager.uiManager, "inited", true);
        var arena = root.AddComponent<MapGenerationFinalBoss>(); arena.gameManager = manager;
        arena.spawnTransform = new GameObject("spawn").transform; arena.spawnTransform.SetParent(root.transform); arena.spawnTransform.position = new Vector3(9, 2, -4); arena.spawnTransform.rotation = Quaternion.Euler(0, 40, 0);
        arena.colliderBox = new GameObject("bounds"); arena.colliderBox.transform.SetParent(root.transform);
        arena.grassRenderer = new GameObject("grass").AddComponent<GrassChunkManager>(); arena.grassRenderer.transform.SetParent(root.transform);
        arena.worldMeshRenderer = new GameObject("world").AddComponent<MeshRenderer>(); arena.worldMeshRenderer.transform.SetParent(root.transform);
        arena.worldMeshFilter = arena.worldMeshRenderer.gameObject.AddComponent<MeshFilter>(); arena.worldMeshFilter.sharedMesh = new Mesh();
        var extra = new GameObject("extra").AddComponent<MeshRenderer>(); extra.transform.SetParent(root.transform); arena.renderersForMaterial = new[] { extra };
        arena.minimapMesh = new GameObject("minimap").AddComponent<MinimapMesh>(); arena.minimapMesh.transform.SetParent(root.transform); arena.minimapMesh.meshFilter = arena.minimapMesh.gameObject.AddComponent<MeshFilter>(); arena.minimapMesh.meshRenderer = arena.minimapMesh.gameObject.AddComponent<MeshRenderer>();
        var material = new Material(Shader.Find("Standard")); arena.minimapMesh.meshRenderer.sharedMaterial = material;
        arena.sunLight = new GameObject("sun").AddComponent<Light>(); arena.sunLight.transform.SetParent(root.transform);
        try {
            var routine = (IEnumerator)typeof(MapGenerationFinalBoss).GetMethod("GenerateMap", Private).Invoke(arena, null);
            Check(!routine.MoveNext(), "setup completes without yielding");
            Near((Vector3)typeof(MapGenerationFinalBoss).GetField("spawnPosition", Private).GetValue(arena), arena.spawnTransform.position, "spawn position");
            Near((Vector3)typeof(MapGenerationFinalBoss).GetField("spawnDirection", Private).GetValue(arena), arena.spawnTransform.forward, "spawn direction");
            Near(arena.colliderBox.transform.position, new Vector3(0, -155, 0), "collider center");
            Near(arena.colliderBox.transform.localScale, new Vector3(300, 500, 300), "collider scale");
            Near(MapInfo.mapBoundsLower, Vector3.one * -150, "lower bounds"); Near(MapInfo.mapBoundsUpper, Vector3.one * 150, "upper bounds");
            Check(arena.worldMeshRenderer.sharedMaterial == map.finalStageMaterial && extra.sharedMaterial == map.finalStageMaterial, "original map material assigned");
            Check(arena.minimapMesh.meshFilter.sharedMesh == arena.worldMeshFilter.sharedMesh, "world mesh passed to minimap");
            Check(Mathf.Abs(RenderSettings.fogDensity - .005f) < .000001f, "fog density");
            Check(arena.sunLight.color == stage.lightColor && Mathf.Abs(arena.sunLight.intensity - stage.lightIntensity) < .001f, "stage lighting");
            Debug.Log("FinalArenaPortCheck passed: original forest data, arena bounds, spawn, materials, lighting and minimap");
        } finally { UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(root); }
    }
}
