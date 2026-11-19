#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Runs against the shipped Forest tile meshes and the actual welded collision
// mesh. This catches geometry-scale seams that a logical maze-only test misses.
public static class OpenBonkStreetLayoutCheck
{
    public static void Run()
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        Require(forest != null && forest.stages.Length == 3, "Forest fixture");
        var owner = new GameObject("Street generation check");
        var generator = owner.AddComponent<ProceduralTileGeneration>();
        generator.flatTile = Tile("TileGround"); generator.slopeTile = Tile("TileSlope");
        generator.ceilingTile = Tile("TileCeiling"); generator.stairsMesh = Tile("StairMesh");
        generator.wallFlat = Tile("TileWall"); generator.wallLeftUp = Tile("TileWallSlopeLeftUp");
        generator.wallLeftDown = Tile("TileWallSlopeLeftDown"); generator.wallLeftCross = Tile("TileWallSlopeLeftDownCross");
        var material = new Material(Shader.Find("Unlit/Texture"));
        var texture = new Texture2D(2, 2);
        var frontage = new GameObject("Street check frontage");
        var mesh = Box(new Vector3(6.27f, 4.53f, 3.78f));
        frontage.AddComponent<MeshFilter>().sharedMesh = mesh;
        frontage.AddComponent<MeshRenderer>().sharedMaterial = material;
        var box = frontage.AddComponent<BoxCollider>(); box.center = mesh.bounds.center; box.size = mesh.bounds.size;
        int connections = 0, placed = 0;
        var savedRandom = UnityEngine.Random.state;
        try
        {
            Require(new OpenBonkStreetLayout.Spec { tileWidth = 26, stepHeight = 6, coverage = .85f, frontages = new[] { "NimboRamenFront" } }.IsValid(), "valid profile");
            Require(!new OpenBonkStreetLayout.Spec { tileWidth = 26, stepHeight = 6, frontages = new[] { "NimboRamenFront" } }.IsValid(), "missing coverage rejected");
            Require(!OpenBonkStreetLayout.DimensionsValid(26.5f, 6) && !OpenBonkStreetLayout.DimensionsValid(20, 12), "fractional dimensions and excessive slope rejected");
            var originalWidth = new int[3]; var originalHeight = new int[3];
            var environments = new OpenBonkProceduralMapRecipe.StageEnvironment[3];
            for (int stage = 0; stage < 3; stage++)
            {
                originalWidth[stage] = forest.stages[stage].mapParameters.scaledTileWidth;
                originalHeight[stage] = forest.stages[stage].mapParameters.scaledTileHeight;
                environments[stage] = new OpenBonkProceduralMapRecipe.StageEnvironment
                {
                    stage = stage + 1, architectureMaterial = material, skyMaterial = material,
                    groundTexture = texture, wallTexture = texture, replaceDecorations = true,
                    streetscape = new OpenBonkStreetLayout.Profile(26 + stage * 2, 6 + stage, .85f, new[] { frontage }),
                };
            }
            var recipe = new OpenBonkProceduralMapRecipe
            {
                name = "Street test", description = "Street test", tileGridSize = 15,
                volatility = .8f, slopeStrength = .25f, flatMapBias = .1f, stageDuration = 420,
                stageEnvironments = environments,
            };
            using var build = OpenBonkProceduralMapBuild.Create(forest, recipe, texture);
            for (int stage = 0; stage < 3; stage++)
            {
                var data = build.Map.stages[stage]; var profile = environments[stage].streetscape;
                Require(data.mapParameters.scaledTileWidth == profile.tileWidth && data.mapParameters.scaledTileHeight == profile.stepHeight, "cloned dimensions");
                Require(forest.stages[stage].mapParameters.scaledTileWidth == originalWidth[stage] && forest.stages[stage].mapParameters.scaledTileHeight == originalHeight[stage], "Forest dimensions preserved");
                foreach (int seed in new[] { 0, 42, 731, 20261119 })
                {
                    generator.debugSeed = seed;
                    var spawn = generator.Generate(out _, data, data.mapParameters, true, streetscape: profile);
                    var terrain = generator.collider.GetComponent<MeshCollider>();
                    Physics.SyncTransforms();
                    Require(generator.tiles.Count == 225 && generator.flatTiles.Count > 2, "complete terrain grid");
                    Require(generator.tiles.Count > generator.flatTiles.Count, "elevation uses real ramps");
                    foreach (var fill in generator.fillTiles)
                    {
                        var scale = fill.transform.localScale;
                        Require(Mathf.Abs(Mathf.Abs(scale.x) - profile.tileWidth * .5f) < .001f &&
                            Mathf.Abs(scale.y - profile.stepHeight) < .001f && Mathf.Abs(scale.z - profile.tileWidth * .5f) < .001f, "fill geometry uses matching nonuniform dimensions");
                    }
                    Require((generator.flatTiles[0].transform.position - spawn).sqrMagnitude < .001f, "spawn anchor");
                    connections += CheckConnections(generator, terrain, profile, seed, 15);
                    UnityEngine.Random.InitState(413);
                    var before = UnityEngine.Random.state;
                    var first = OpenBonkStreetLayout.Plan(generator.flatTiles, profile, seed, terrain);
                    float after = UnityEngine.Random.value;
                    UnityEngine.Random.state = before;
                    Require(UnityEngine.Random.value == after, "street planner must not consume gameplay RNG");
                    var second = OpenBonkStreetLayout.Plan(generator.flatTiles, profile, seed, terrain);
                    Require(first.Count > generator.flatTiles.Count && first.Count == second.Count, "dense deterministic frontage count");
                    for (int i = 0; i < first.Count; i++)
                    {
                        var p = first[i]; var q = second[i];
                        Require(p.position == q.position && p.rotation == q.rotation && p.scale == q.scale && p.prefab == q.prefab, "deterministic placement");
                        Require(p.tileIndex > 0 && p.tileIndex < generator.flatTiles.Count - 1, "start and boss tiles stay clear");
                        var center = generator.flatTiles[p.tileIndex].transform.position;
                        var min = p.worldBounds.min - center; var max = p.worldBounds.max - center;
                        Require((min.x >= 6f || max.x <= -6f) && (min.z >= 6f || max.z <= -6f), "twelve metre cross and ramp mouths stay clear");
                        Require(Mathf.Max(Mathf.Abs(min.x), Mathf.Abs(max.x), Mathf.Abs(min.z), Mathf.Abs(max.z)) < profile.tileWidth * .5f, "frontage stays on its own flat tile");
                        var inward = center - p.worldBounds.center; inward.y = 0;
                        Require(Vector3.Dot(p.rotation * Vector3.forward, inward) > 0 && p.scale == 1f, "full-size frontage faces street");
                    }
                    placed += first.Count;
                    ReleaseCollisionMesh(generator);
                }
            }
            // Odd metre widths on even grids require a half-metre world origin;
            // retaining the old integer offset would misalign the road shader.
            recipe.tileGridSize = 14;
            environments[0].streetscape = new OpenBonkStreetLayout.Profile(25, 6, .85f, new[] { frontage });
            using (var odd = OpenBonkProceduralMapBuild.Create(forest, recipe, texture))
            {
                var data = odd.Map.stages[0];
                generator.debugSeed = 42;
                generator.Generate(out _, data, data.mapParameters, true, streetscape: environments[0].streetscape);
                Physics.SyncTransforms();
                Require(generator.TilePositionToWorldPosition(new Vector2Int(6, 6)).x == -12.5f, "odd-width/even-grid shader alignment");
                connections += CheckConnections(generator, generator.collider.GetComponent<MeshCollider>(), environments[0].streetscape, 42, 14);
                ReleaseCollisionMesh(generator);
            }
            // A profile-free recipe still uses exactly the native Forest dimensions.
            recipe.tileGridSize = 15;
            recipe.stageEnvironments = null;
            using var legacy = OpenBonkProceduralMapBuild.Create(forest, recipe, texture);
            var native = legacy.Map.stages[0];
            generator.debugSeed = 42;
            generator.Generate(out _, native, native.mapParameters, true);
            Require(generator.flatTiles[0].transform.localScale == generator.flatTile.transform.localScale * native.mapParameters.scale &&
                native.mapParameters.scaledTileWidth == originalWidth[0] && native.mapParameters.scaledTileHeight == originalHeight[0], "legacy transform remains uniform");
            Debug.Log($"OpenBonk streetscape PASS: {connections} actual-collider tree connections across 13 generated maps, {placed} deterministic frontages, clear spawn/boss and 12m lanes, unchanged Forest");
        }
        finally
        {
            UnityEngine.Random.state = savedRandom;
            ReleaseCollisionMesh(generator);
            if (generator.tilesParent != null) Object.DestroyImmediate(generator.tilesParent);
            Object.DestroyImmediate(owner); Object.DestroyImmediate(frontage); Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(texture); Object.DestroyImmediate(material);
        }
    }

    private static int CheckConnections(ProceduralTileGeneration generator, MeshCollider terrain, OpenBonkStreetLayout.Profile profile, int seed, int size)
    {
        int count = 0;
        var pending = new Queue<NodeTree>(); pending.Enqueue(Maze.Generate(size, size, seed));
        while (pending.Count > 0)
        {
            var parent = pending.Dequeue();
            foreach (var child in parent.children)
            {
                pending.Enqueue(child); count++;
                var a = generator.proceduralTiles[parent.position.x][parent.position.y];
                var b = generator.proceduralTiles[child.position.x][child.position.y];
                Vector3 from = a.transform.position, to = b.transform.position;
                Vector3 direction = to - from; direction.y = 0; direction.Normalize();
                Vector3 shoulder = Vector3.Cross(direction, Vector3.up);
                foreach (float side in new[] { -5.5f, 0f, 5.5f })
                {
                    Vector3 previous = default;
                    const int samples = 64;
                    for (int sample = 0; sample <= samples; sample++)
                    {
                        float fraction = sample / (float)samples;
                        Vector3 point = Vector3.Lerp(from, to, fraction) + shoulder * side;
                        var tile = fraction < .5f ? a : b;
                        float expected = tile.transform.position.y;
                        if (!tile.isFlat) expected += profile.stepHeight * (.5f + Vector3.Dot(point - tile.transform.position, tile.transform.forward) / profile.tileWidth);
                        Require(terrain.Raycast(new Ray(new Vector3(point.x, expected + 2f, point.z), Vector3.down), out var hit, 4f), $"missing floor seed={seed} point={point}");
                        Require(Mathf.Abs(hit.point.y - expected) < .06f && hit.normal.y > .92f, $"noncontinuous terrain seed={seed} expected={expected} actual={hit.point}");
                        var body = hit.point + Vector3.up * .7f;
                        if (sample > 0)
                        {
                            var travel = body - previous;
                            Require(!terrain.Raycast(new Ray(previous, travel.normalized), out _, travel.magnitude), $"wall blocks connected street seed={seed} point={point}");
                        }
                        previous = body;
                    }
                }
            }
        }
        return count;
    }
    private static void ReleaseCollisionMesh(ProceduralTileGeneration generator)
    {
        if (generator.collider == null) return;
        var mesh = generator.collider.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(generator.collider); Object.DestroyImmediate(mesh);
    }
    private static GameObject Tile(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
        Require(prefab != null, name); return prefab;
    }
    private static Mesh Box(Vector3 size)
    {
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i] + Vector3.up * .5f, size);
        mesh.vertices = vertices; mesh.RecalculateBounds(); Object.DestroyImmediate(primitive); return mesh;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
#endif
