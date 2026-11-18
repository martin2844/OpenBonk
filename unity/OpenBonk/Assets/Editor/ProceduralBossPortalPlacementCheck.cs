using System;
using Assets.Scripts.Game.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class ProceduralBossPortalPlacementCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity");
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset");
        var controller = Object.FindAnyObjectByType<MapGenerationController>(FindObjectsInactive.Include);
        var generator = controller.proceduralMapMeshGenerator;
        var edgeObject = controller.proceduralMapWorldEdges;
        var edge = edgeObject.GetComponent<Collider>();
        var originalScale = edgeObject.transform.localScale;
        generator.gameObject.SetActive(true);

        int oldBlocked = 0;
        int tested = 0;
        foreach (StageData stage in map.stages)
        {
            for (int seed = 101; seed <= 104; seed++)
            {
                generator.GenerateMap(map, stage, seed);
                MeshCollider terrain = generator.display.meshCollider;
                edgeObject.transform.localScale = originalScale * stage.proceduralMapScale;
                edgeObject.SetActive(true);
                Physics.SyncTransforms();
                Bounds perimeter = edge.bounds;
                Bounds bounds = terrain.bounds;
                float waterHeight = controller.water.transform.position.y;
                int mask = controller.gameManager.whatIsGroundAndObjects;

                // The 0.1.11 path samples the whole mesh before the border and water exist.
                edgeObject.SetActive(false);
                Physics.SyncTransforms();
                var oldPositions = new Vector3[32];
                for (int attempt = 0; attempt < oldPositions.Length; attempt++)
                {
                    Random.InitState(seed * 1000 + attempt);
                    Vector3 old = SpawnPositions.GetObjectSpawnPosition(bounds.center + Vector3.up * 999f, bounds.size,
                        5f, mask, out _, 9999, true, false, false, 10f);
                    Require(old != SpawnPositions.INVALID_POS, "baseline has ground on " + stage.name);
                    oldPositions[attempt] = old;
                }

                edgeObject.SetActive(true);
                Physics.SyncTransforms();
                foreach (Vector3 position in oldPositions)
                {
                    if (TerrainCoveredByBorder(position, bounds, edge, mask)) oldBlocked++;
                }
                for (int attempt = 0; attempt < 32; attempt++)
                {
                    Random.InitState(seed * 1000 + attempt);
                    Vector3 position = SpawnPositions.GetProceduralObjectivePosition(terrain, edge, waterHeight + .25f, mask);
                    Require(position != SpawnPositions.INVALID_POS && WithinPerimeter(position, perimeter, 5f) &&
                        position.y >= waterHeight + .25f && !TerrainCoveredByBorder(position, bounds, edge, mask),
                        $"no safe spawn for {stage.name}, seed {seed}, sample {attempt}");
                    tested++;
                }
                Object.DestroyImmediate(terrain.sharedMesh);
            }
        }
        Require(oldBlocked > 0, "the old placement must reproduce the border overlap");
        Debug.Log($"ProceduralBossPortalPlacementCheck passed: {tested} Desert samples across all tiers; baseline covered by the border: {oldBlocked}");
    }

    private static bool TerrainCoveredByBorder(Vector3 position, Bounds terrain, Collider edge, int mask)
    {
        float top = Mathf.Max(terrain.max.y, edge.bounds.max.y) + 6f;
        var origin = new Vector3(position.x, top, position.z);
        return Physics.SphereCast(origin, 5f, Vector3.down, out RaycastHit hit, top - terrain.min.y,
            mask, QueryTriggerInteraction.Ignore) && hit.collider == edge;
    }

    private static bool WithinPerimeter(Vector3 position, Bounds bounds, float margin)
    {
        return position.x >= bounds.min.x + margin && position.x <= bounds.max.x - margin &&
            position.z >= bounds.min.z + margin && position.z <= bounds.max.z - margin;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("ProceduralBossPortalPlacementCheck: " + message);
    }
}
