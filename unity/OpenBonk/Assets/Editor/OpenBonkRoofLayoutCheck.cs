#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OpenBonkRoofLayoutCheck
{
    public static void Run() => RunLayout(false);

    public static void RunDaylight() => RunLayout(true);

    private static void RunLayout(bool daylight)
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var owner = new GameObject("Roof generation check");
        var generator = owner.AddComponent<ProceduralTileGeneration>();
        var profiles = new[]
        {
            new OpenBonkRoofLayout.Profile { roofFraction = .76f, bridgeFraction = .30f, riseFraction = .045f },
            new OpenBonkRoofLayout.Profile { roofFraction = .71f, bridgeFraction = .27f, riseFraction = .065f },
            new OpenBonkRoofLayout.Profile { roofFraction = .66f, bridgeFraction = .24f, riseFraction = .09f },
        };
        int testedConnections = 0;
        Mesh previous = null;
        Material dayMaterial = null;
        try
        {
            if (daylight)
            {
                dayMaterial = new Material(Shader.Find("Unlit/Texture"));
                var style = typeof(OpenBonkRoofLayout.Profile).GetMethod("WithArchitecture",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                for (int stage = 0; stage < profiles.Length; stage++)
                    profiles[stage] = (OpenBonkRoofLayout.Profile)style.Invoke(profiles[stage],
                        new object[] { dayMaterial, dayMaterial, dayMaterial });
            }
            for (int stage = 0; stage < profiles.Length; stage++)
            {
                StageData template = forest.stages[stage];
                var parameters = Object.Instantiate(template.mapParameters);
                parameters.size = 9;
                try
                {
                    for (int seed = 0; seed < 30; seed++)
                    {
                        generator.debugSeed = seed;
                        Vector3 entry = generator.Generate(out _, template, parameters, true, profiles[stage]);
                        Require(previous == null, "previous collision mesh was retained");
                        var ground = generator.collider.GetComponent<MeshCollider>();
                        previous = ground.sharedMesh;
                        Physics.SyncTransforms();
                        if (daylight)
                        {
                            var visual = generator.tilesParent.GetComponent<MeshFilter>().sharedMesh;
                            Require(visual.colors.Distinct().Count() >= 10, "missing stylized geometry and palette");
                        }
                        Vector3 boss = generator.flatTiles.Last().transform.position;
                        Require(entry != boss, "distinct entrance and boss roofs");
                        Require(Mathf.Abs(Floor(ground, entry).point.y - entry.y) < .01f, "entrance height");
                        Require(Mathf.Abs(Floor(ground, boss).point.y - boss.y) < .01f, "boss height");
                        Require(!HasFloor(ground, Vector3.right * parameters.scaledTileWidth * 5.5f, out _), "skyline must stay outside collision");
                        var pending = new Queue<NodeTree>();
                        pending.Enqueue(Maze.Generate(parameters.size, parameters.size, seed));
                        while (pending.Count > 0)
                        {
                            var parent = pending.Dequeue();
                            Vector3 a = generator.TilePositionToWorldPosition(parent.position);
                            Floor(ground, a);
                            foreach (var child in parent.children)
                            {
                                Vector3 b = generator.TilePositionToWorldPosition(child.position);
                                Vector3 shoulder = Vector3.Cross((b - a).normalized, Vector3.up) *
                                    (parameters.scaledTileWidth * profiles[stage].bridgeFraction * .4f);
                                var advance = (b - a).normalized;
                                float interval = (b - a).magnitude / 32f;
                                for (int point = 0; point <= 32; point++)
                                {
                                    Vector3 sample = Vector3.Lerp(a, b, point / 32f);
                                    foreach (float side in new[] { -1f, 1f })
                                    {
                                        Vector3 lane = sample + shoulder * side;
                                        Require(HasFloor(ground, lane, out var floor),
                                            $"missing bridge floor at {lane} in stage {stage + 1}, seed {seed}");
                                        Require(Vector3.Angle(Vector3.up, floor.normal) <= 30f,
                                            $"steep walkable space in stage {stage + 1}, seed {seed}");
                                        if (daylight && point < 32 &&
                                            ground.Raycast(new Ray(floor.point + Vector3.up * .68f, advance), out var obstruction, interval))
                                            Require(obstruction.normal.y > .4f, $"blocked bridge in stage {stage + 1}, seed {seed}");
                                    }
                                }
                                testedConnections++;
                                pending.Enqueue(child);
                            }
                        }
                        VerifyGap(ground, generator, parameters);
                    }
                }
                finally { Object.DestroyImmediate(parameters); }
            }
        }
        finally
        {
            // Edit-mode objects do not run ProceduralTileGeneration.OnDestroy.
            if (generator.tilesParent != null)
            {
                var visual = generator.tilesParent.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(generator.tilesParent);
                Object.DestroyImmediate(visual);
            }
            if (generator.collider != null) Object.DestroyImmediate(generator.collider);
            if (previous != null) Object.DestroyImmediate(previous);
            Object.DestroyImmediate(owner);
            if (dayMaterial != null) Object.DestroyImmediate(dayMaterial);
        }
        Debug.Log($"OpenBonk rooftop layout PASS: 90 {(daylight ? "daylight" : "default")} districts, {testedConnections} measured crossings, intact gaps and guardrails, valid portal grounds");
    }

    private static void VerifyGap(MeshCollider ground, ProceduralTileGeneration generator, MapParameters parameters)
    {
        for (int x = 0; x < parameters.size - 1; x++)
            for (int z = 0; z < parameters.size; z++)
            {
                Vector3 roof = generator.TilePositionToWorldPosition(new Vector2Int(x, z));
                float distance = parameters.scaledTileWidth * .5f;
                if (HasFloor(ground, roof + Vector3.right * distance, out _)) continue;
                Vector3 inside = Floor(ground, roof).point + Vector3.up * .7f;
                Require(ground.Raycast(new Ray(inside, Vector3.right), out var rail, distance),
                    "a roof edge without a bridge needs a guard");
                Require(Mathf.Abs(rail.normal.y) < .01f, "guard is vertical");
                return;
            }
        throw new InvalidOperationException("expected at least one open gap between buildings");
    }

    private static RaycastHit Floor(MeshCollider ground, Vector3 point)
    {
        Require(HasFloor(ground, point, out var hit), $"missing walkable surface at {point}");
        return hit;
    }

    private static bool HasFloor(MeshCollider ground, Vector3 point, out RaycastHit hit) =>
        ground.Raycast(new Ray(point + Vector3.up * 100f, Vector3.down), out hit, 200f);

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
#endif
