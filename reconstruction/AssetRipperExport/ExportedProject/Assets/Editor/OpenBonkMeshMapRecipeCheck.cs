#if UNITY_EDITOR
using System;
using Assets.Scripts._Data.MapsAndStages;
using UnityEditor;
using UnityEngine;

public static class OpenBonkMeshMapRecipeCheck
{
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var desert = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset");
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        Require(desert != null && forest != null && desert.stages.Length == 3, "template assets");
        var originalHeight = new float[3]; var originalScale = new float[3]; var originalNoise = new float[3];
        for (int i = 0; i < 3; i++) { originalHeight[i] = desert.stages[i].proceduralTerrainData.heightMultiplier; originalScale[i] = desert.stages[i].proceduralMapScale; originalNoise[i] = desert.stages[i].proceduralNoiseData.noiseScale; }
        var recipe = new OpenBonkMeshMapRecipe { name = "Dune Test", description = "A custom mesh map", noiseScale = 80, heightMultiplier = 8, mapScale = .9f, stageDuration = 300 };
        var icon = new Texture2D(2, 2);
        try
        {
            using var build = OpenBonkMeshMapBuild.Create(desert, recipe, icon);
            Require(build.Map.enemyPoolMap == EMap.Desert && build.Map.mapType == EMapType.ProceduralMesh && build.Map.stages.Length == 3, "map metadata");
            Require(MapEventsManager.HasDesertEvents(build.Map) && MapEventsManager.HasDesertEvents(desert) && !MapEventsManager.HasDesertEvents(forest), "inherited desert events only");
            for (int i = 0; i < 3; i++)
            {
                var original = desert.stages[i]; var stage = build.Map.stages[i];
                Require(stage != original && stage.proceduralTerrainData != original.proceduralTerrainData && stage.proceduralNoiseData != original.proceduralNoiseData, "cloned assets");
                Require(stage.proceduralTerrainData.heightMultiplier == 8 && stage.proceduralNoiseData.noiseScale == 80 && stage.proceduralMapScale == .9f, "recipe parameters");
                Require(original.proceduralTerrainData.heightMultiplier == originalHeight[i] && original.proceduralMapScale == originalScale[i] && original.proceduralNoiseData.noiseScale == originalNoise[i], "original terrain unchanged");
                Require(stage.stageTimeline != original.stageTimeline && stage.stageTimeline.stageTime == 300 && stage.challenges.Length == 0, "independent timeline and challenges");
                Require(stage.triplanarMaterial == original.triplanarMaterial && stage.randomMapObjects.Length == original.randomMapObjects.Length, "inherited assets");
            }
            var s = build.Map.stages[0]; var n = s.proceduralNoiseData; var t = s.proceduralTerrainData;
            var height = Noise.GenerateNoiseMap(241, 241, 42, n.noiseScale, n.octaves, n.persistance, n.lacunarity, n.blend, n.blendStrength, n.offset);
            var mesh = MeshGenerator.GenerateTerrainMesh(height, t.heightMultiplier, t.heightCurve, 1);
            Require(mesh.vertices.Length == 121 * 121 && mesh.triangles.Length == 120 * 120 * 6, "generated mesh size");
            float min = float.MaxValue, max = float.MinValue;
            foreach (var vertex in mesh.vertices) { Require(!float.IsNaN(vertex.y) && !float.IsInfinity(vertex.y), "finite heights"); min = Mathf.Min(min, vertex.y); max = Mathf.Max(max, vertex.y); }
            Require(max > min, "non-flat mesh");
            var originalMaterial = desert.stages[0].triplanarMaterial;
            var oldTop = originalMaterial.GetTexture("_MainTex"); var oldWall = originalMaterial.GetTexture("_MainTexSide");
            var ground = new Texture2D(2, 2); var wall = new Texture2D(2, 2);
            try
            {
                using var both = OpenBonkMeshMapBuild.Create(desert, recipe, icon, ground, wall);
                var material = both.Map.stages[0].triplanarMaterial;
                Require(material != originalMaterial && material.GetTexture("_MainTex") == ground && material.GetTexture("_BelowYTexture") == ground && material.GetTexture("_MainTexSide") == wall, "mesh texture overrides");
                Require(desert.stages[0].triplanarMaterial == originalMaterial && originalMaterial.GetTexture("_MainTex") == oldTop && originalMaterial.GetTexture("_MainTexSide") == oldWall, "original material unchanged");
                using var groundOnly = OpenBonkMeshMapBuild.Create(desert, recipe, icon, ground);
                Require(groundOnly.Map.stages[0].triplanarMaterial.GetTexture("_MainTexSide") == oldWall, "mesh ground-only override");
                using var wallOnly = OpenBonkMeshMapBuild.Create(desert, recipe, icon, null, wall);
                Require(wallOnly.Map.stages[0].triplanarMaterial.GetTexture("_MainTex") == oldTop, "mesh wall-only override");
            }
            finally { UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(wall); }
            bool rejected = false; try { OpenBonkMeshMapBuild.Create(forest, recipe, icon); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "forest template rejected");
            recipe.noiseScale = float.NaN; rejected = false; try { OpenBonkMeshMapBuild.Create(desert, recipe, icon); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "nonfinite recipe rejected");
            Debug.Log($"OpenBonk mesh map recipe PASS: 3 independent stages, {mesh.vertices.Length} vertices, heights {min:F2}..{max:F2}, invalid inputs rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(icon); }
    }
}
#endif
