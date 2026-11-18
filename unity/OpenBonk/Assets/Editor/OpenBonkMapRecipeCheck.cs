#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using Assets.Scripts._Data.MapsAndStages;
using UnityEditor;
using UnityEngine;

public static class OpenBonkMapRecipeCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static int Count(NodeTree root)
    {
        var pending = new Stack<NodeTree>(); pending.Push(root); int count = 0;
        while (pending.Count != 0) { var node = pending.Pop(); count++; foreach (var child in node.children) pending.Push(child); }
        return count;
    }
    public static void Run()
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var desert = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset");
        Require(forest != null && desert != null && forest.stages.Length == 3, "map assets");
        int[] oldSizes = new int[3]; float[] oldVolatility = new float[3];
        for (int i = 0; i < 3; i++) { oldSizes[i] = forest.stages[i].mapParameters.size; oldVolatility[i] = forest.stages[i].mapParameters.volatility; }
        var recipe = new OpenBonkProceduralMapRecipe { name = "Custom Forest", description = "Test", tileGridSize = 9, volatility = 1.1f, slopeStrength = .25f, flatMapBias = .1f, stageDuration = 300 };
        var icon = new Texture2D(2, 2);
        try
        {
            using var build = OpenBonkProceduralMapBuild.Create(forest, recipe, icon);
            Require(build.Map != null && build.Map.stages.Length == 3 && build.Map.stageDuration == 300 && build.Map.icon == icon, "map metadata");
            build.Map.eMap = (EMap)OpenBonkContentIds.Map("test.package", "custom-forest");
            Require(OpenBonkExternalMapData.EnemyPoolMap(build.Map) == EMap.Forest && build.Map.eMap != EMap.Forest && OpenBonkExternalMapData.EnemyPoolMap(desert) == EMap.Desert, "enemy pool template and independent identity");
            Require(MapTitle.StageTitle(build.Map, build.Map.stages[0]) == recipe.name && MapTitle.StageDescription(build.Map, build.Map.stages[0]) == recipe.description, "custom stage title");
            for (int i = 0; i < 3; i++)
            {
                var original = forest.stages[i]; var stage = build.Map.stages[i]; var parameters = stage.mapParameters;
                Require(stage.challenges != null && stage.challenges.Length == 0 && !MapStatsWindow.HasEnabledChallenges(stage), "no inherited Forest challenges");
                Require(stage != original && parameters != original.mapParameters && parameters.testStageData == stage, "independent stage and parameters");
                Require(parameters.size == 9 && Mathf.Approximately(parameters.volatility, 1.1f) && Mathf.Approximately(parameters.slopeStrength, .25f) && Mathf.Approximately(parameters.flatMapBias, .1f), "recipe values");
                Require(original.mapParameters.size == oldSizes[i] && original.mapParameters.volatility == oldVolatility[i], "original mutation");
                Require(stage.stageTimeline != original.stageTimeline && stage.stageTimeline.stageTime == 300, "independent timeline duration");
                float scale = 300f / original.stageTimeline.stageTime;
                Require(Mathf.Approximately(stage.stageTimeline.checkNewEnemyInterval, original.stageTimeline.checkNewEnemyInterval * scale), "enemy interval");
                if (original.stageTimeline.events != null)
                    for (int e = 0; e < original.stageTimeline.events.Count; e++)
                    {
                        var before = original.stageTimeline.events[e]; var after = stage.stageTimeline.events[e];
                        if (before != null) Require(after != before && Mathf.Approximately(after.timeMinutes, before.timeMinutes * scale) && Mathf.Approximately(after.duration, before.duration * scale), "independent scaled event");
                    }
                Require(stage.stageTilePrefabs != null, "tile configuration");
                Require(stage.stageTilePrefabs.flatTilePrefabs.Length == original.stageTilePrefabs.flatTilePrefabs.Length, "tile prefab count");
                for (int tile = 0; tile < original.stageTilePrefabs.flatTilePrefabs.Length; tile++)
                    Require(stage.stageTilePrefabs.flatTilePrefabs[tile] == original.stageTilePrefabs.flatTilePrefabs[tile], "tile prefab reference");
                Require(stage.waterMaterial == original.waterMaterial && stage.skybox == original.skybox, "base visual resources");
                Require(Count(Maze.Generate(parameters.size, parameters.size, 42)) == 81, "recipe maze count");
            }
            bool rejected = false; try { OpenBonkProceduralMapBuild.Create(desert, recipe, icon); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "mesh template must not use tile recipe");
            var originalMaterial = forest.stages[0].flatMaterials[0];
            var originalTexture = originalMaterial.HasProperty("_MainTex") ? originalMaterial.GetTexture("_MainTex") : null;
            var ground = new Texture2D(2, 2); var wall = new Texture2D(2, 2);
            try
            {
                using var textured = OpenBonkProceduralMapBuild.Create(forest, recipe, icon, ground, wall);
                var replacement = textured.Map.stages[0].flatMaterials[0];
                Require(replacement != originalMaterial && forest.stages[0].flatMaterials[0] == originalMaterial, "independent ground material");
                foreach (var property in new[] { "_MainTex", "_BaseMap", "_AlbedoMap" })
                    if (replacement.HasProperty(property)) Require(replacement.GetTexture(property) == ground, "ground texture property");
                if (originalMaterial.HasProperty("_MainTex")) Require(originalMaterial.GetTexture("_MainTex") == originalTexture, "original ground texture unchanged");
                var originalWall = forest.stages[0].m_fillMiddle;
                var newWall = textured.Map.stages[0].m_fillMiddle;
                Require(originalWall != null && newWall != originalWall && forest.stages[0].m_fillMiddle == originalWall, "independent wall material");
                foreach (var property in new[] { "_MainTex", "_BaseMap", "_AlbedoMap" })
                    if (newWall.HasProperty(property)) Require(newWall.GetTexture(property) == wall, "wall texture property");
                using var wallOnly = OpenBonkProceduralMapBuild.Create(forest, recipe, icon, null, wall);
                Require(wallOnly.Map.stages[0].flatMaterials[0] == originalMaterial && wallOnly.Map.stages[0].m_fillMiddle != originalWall, "wall-only recipe");
                using var groundOnly = OpenBonkProceduralMapBuild.Create(forest, recipe, icon, ground);
                Require(groundOnly.Map.stages[0].flatMaterials[0] != originalMaterial && groundOnly.Map.stages[0].m_fillMiddle == originalWall, "ground-only recipe");

                var repeatedTemplate = UnityEngine.Object.Instantiate(forest);
                try
                {
                    var source = forest.stages[0];
                    repeatedTemplate.stages = new[] { source, source, source };
                    recipe.stageEnvironments = new[]
                    {
                        new OpenBonkProceduralMapRecipe.StageEnvironment { stage = 2, groundTexture = wall, wallTexture = ground, replaceDecorations = true },
                        new OpenBonkProceduralMapRecipe.StageEnvironment { stage = 1, replaceDecorations = true },
                    };
                    using var district = OpenBonkProceduralMapBuild.Create(repeatedTemplate, recipe, icon, ground, wall);
                    var stages = district.Map.stages;
                    Require(stages[0].flatMaterials[0] == stages[2].flatMaterials[0] && stages[1].flatMaterials[0] != stages[0].flatMaterials[0], "reused base material needs distinct variants for distinct texture files");
                    Require(stages[0].flatMaterials[0].mainTexture == ground && stages[1].flatMaterials[0].mainTexture == wall, "stage ground and global fallback");
                    Require(stages[1].m_fillMiddle.mainTexture == ground && stages[2].m_fillMiddle.mainTexture == wall, "stage wall and global fallback");
                    for (int i = 0; i < 2; i++)
                        Require(stages[i].grassMaterial == null && stages[i].grassPerChunk == 0 && stages[i].particles == null &&
                            stages[i].randomMapObjects.Length == 0 && stages[i].stageTilePrefabs.flatTilePrefabs.Length == 0 &&
                            stages[i].stageTilePrefabs.mapSpecificTilesPrefabs.Length == 0, "only the mod's decorations should populate the selected stage");
                    Require(source.randomMapObjects.Length > 0 && source.stageTilePrefabs.flatTilePrefabs.Length > 0 &&
                        stages[2].randomMapObjects.Length == source.randomMapObjects.Length &&
                        stages[2].stageTilePrefabs.flatTilePrefabs.Length == source.stageTilePrefabs.flatTilePrefabs.Length, "untouched stage and Forest template retain their decorations");
                    recipe.stageEnvironments[1].stage = 2;
                    rejected = false;
                    try { OpenBonkProceduralMapBuild.Create(repeatedTemplate, recipe, icon); } catch (ArgumentException) { rejected = true; }
                    Require(rejected, "duplicate stage environments must fail");
                }
                finally { recipe.stageEnvironments = null; UnityEngine.Object.DestroyImmediate(repeatedTemplate); }
            }
            finally { UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(wall); }
            recipe.tileGridSize = 0; rejected = false; try { OpenBonkProceduralMapBuild.Create(forest, recipe, icon); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "invalid size");
            recipe.tileGridSize = 9; recipe.volatility = float.NaN; rejected = false; try { OpenBonkProceduralMapBuild.Create(forest, recipe, icon); } catch (ArgumentException) { rejected = true; }
            Require(rejected, "nonfinite recipe");
            Debug.Log("OpenBonk map recipe PASS: independent parameters and stage environments, preserved Forest assets, 81-cell maze, invalid and incompatible input rejection");
        }
        finally { UnityEngine.Object.DestroyImmediate(icon); }
    }
}
#endif
