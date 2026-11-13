using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEditor;
using UnityEngine;

public static class ProceduralEdgeFillPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ProceduralEdgeFillPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("edge fixture"); root.SetActive(false);
        var generator = root.AddComponent<ProceduralTileGeneration>();
        generator.wallFlat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/TileWall.prefab");
        var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/StageDungeon.asset");
        var map = ScriptableObject.CreateInstance<MapParameters>(); map.size = 2; map.scale = 1; map.scaledTileWidth = 10;
        Check(generator.wallFlat != null && stage != null && stage.mapEdgeFillType == MapEdgeFillType.Walls, "original wall and dungeon stage");
        typeof(ProceduralTileGeneration).GetField("currentStage", Private).SetValue(generator, stage);
        typeof(ProceduralTileGeneration).GetField("currentMapParameters", Private).SetValue(generator, map);
        generator.proceduralTiles = new ProceduralTile[2][];
        generator.fillTiles = new List<GameObject>();
        try
        {
            for (int x = 0; x < 2; x++)
            {
                generator.proceduralTiles[x] = new ProceduralTile[2];
                for (int y = 0; y < 2; y++)
                {
                    var obj = new GameObject($"tile-{x}-{y}"); obj.transform.SetParent(root.transform);
                    var tile = obj.AddComponent<ProceduralTile>(); tile.posY = x * 2 + y; tile.edges = new List<TileEdge>();
                    foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }) tile.edges.Add(new TileEdge { direction = direction, type = ETileEdgeType.Flat });
                    generator.proceduralTiles[x][y] = tile;
                }
            }
            typeof(ProceduralTileGeneration).GetMethod("FillEdgesWalls", Private).Invoke(generator, null);
            Check(generator.fillTiles.Count == 28, "walls extend all sides to two above maximum height");
            foreach (var obj in generator.fillTiles) Check(obj.transform.position.y >= 0f && obj.transform.position.y <= 4f && obj.GetComponent<FillTile>().useEdge, "wall height and edge material");
            foreach (var obj in generator.fillTiles) UnityEngine.Object.DestroyImmediate(obj);
            generator.fillTiles.Clear();
            typeof(ProceduralTileGeneration).GetMethod("FillEdgesIsland", Private).Invoke(generator, null);
            Check(generator.fillTiles.Count == 20, "island extends all sides to one below minimum height");
            foreach (var obj in generator.fillTiles) Check(obj.transform.position.y >= -1f && obj.transform.position.y <= 2f && !obj.GetComponent<FillTile>().useEdge, "island height and material");
            Debug.Log("ProceduralEdgeFillPortCheck passed: original dungeon wall prefab, varied tile heights and both edge modes");
        }
        finally { foreach (var obj in generator.fillTiles) if (obj != null) UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(map); }
    }
}
