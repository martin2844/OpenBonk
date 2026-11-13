using System;
using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;
using Object = UnityEngine.Object;

// Experimental recipe based on Desert's procedural mesh generator.
[Serializable]
public sealed class OpenBonkMeshMapRecipe
{
    public string name, description;
    public float noiseScale, heightMultiplier, mapScale, stageDuration;
}

public sealed class OpenBonkMeshMapBuild : IDisposable
{
    private readonly List<Object> owned = new List<Object>();
    public OpenBonkExternalMapData Map { get; private set; }
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    private OpenBonkMeshMapBuild() { }

    public static OpenBonkMeshMapBuild Create(MapData template, OpenBonkMeshMapRecipe recipe, Texture icon, Texture groundTexture = null, Texture wallTexture = null)
    {
        if (template == null || template.eMap != EMap.Desert || template.mapType != EMapType.ProceduralMesh ||
            template.stages == null || template.stages.Length != 3 || recipe == null || icon == null ||
            !Text(recipe.name, 80) || !Text(recipe.description, 500) || !Range(recipe.noiseScale, 25, 250) ||
            !Range(recipe.heightMultiplier, 1, 20) || !Range(recipe.mapScale, .75f, 1.25f) || !Range(recipe.stageDuration, 60, 1800))
            throw new ArgumentException("Invalid mesh map recipe");
        foreach (var stage in template.stages)
            if (stage == null || stage.proceduralTerrainData == null || stage.proceduralNoiseData == null || stage.triplanarMaterial == null ||
                stage.proceduralTerrainData.heightCurve == null || stage.stageTimeline == null || !Range(stage.stageTimeline.stageTime, 1, 100000))
                throw new ArgumentException("Incomplete mesh map template");
        var result = new OpenBonkMeshMapBuild();
        try
        {
            var map = result.Own(ScriptableObject.CreateInstance<OpenBonkExternalMapData>());
            result.Map = map; map.isEnabled = true; map.displayName = recipe.name; map.description = recipe.description; map.enemyPoolMap = template.eMap;
            map.icon = icon; map.mapIconBig = icon; map.mapType = template.mapType; map.unlockOrder = template.unlockOrder;
            map.shrines = template.shrines; map.randomObjectsOverride = template.randomObjectsOverride;
            map.numShrinesMultiplier = template.numShrinesMultiplier; map.numChestsMultiplier = template.numChestsMultiplier;
            map.numShrinesPotsAndOtherMultiplier = template.numShrinesPotsAndOtherMultiplier; map.stageDuration = recipe.stageDuration;
            map.ambience = template.ambience; map.musicTracks = template.musicTracks; map.bossTrack = template.bossTrack;
            map.isWaterDamaging = template.isWaterDamaging; map.finalStageMaterial = template.finalStageMaterial;
            map.stages = new StageData[template.stages.Length];
            var materials = new Dictionary<Material, Material>();
            for (int i = 0; i < map.stages.Length; i++)
            {
                var stage = result.Own(Object.Instantiate(template.stages[i]));
                var terrain = result.Own(Object.Instantiate(stage.proceduralTerrainData));
                var noise = result.Own(Object.Instantiate(stage.proceduralNoiseData));
                terrain.heightMultiplier = recipe.heightMultiplier; noise.noiseScale = recipe.noiseScale;
                stage.proceduralTerrainData = terrain; stage.proceduralNoiseData = noise; stage.proceduralMapScale = recipe.mapScale;
                if (groundTexture != null || wallTexture != null)
                {
                    var original = stage.triplanarMaterial;
                    if (!materials.TryGetValue(original, out var replacement))
                    {
                        replacement = result.Own(Object.Instantiate(original));
                        if (groundTexture != null)
                        {
                            if (!replacement.HasProperty("_MainTex") || !replacement.HasProperty("_BelowYTexture")) throw new ArgumentException("Unsupported mesh ground shader");
                            replacement.SetTexture("_MainTex", groundTexture); replacement.SetTexture("_BelowYTexture", groundTexture);
                        }
                        if (wallTexture != null)
                        {
                            if (!replacement.HasProperty("_MainTexSide")) throw new ArgumentException("Unsupported mesh wall shader");
                            replacement.SetTexture("_MainTexSide", wallTexture);
                        }
                        materials.Add(original, replacement);
                    }
                    stage.triplanarMaterial = replacement;
                }
                stage.challenges = Array.Empty<ChallengeData>();
                float scale = recipe.stageDuration / stage.stageTimeline.stageTime;
                stage.stageTimeline.stageTime = recipe.stageDuration; stage.stageTimeline.checkNewEnemyInterval *= scale;
                if (stage.stageTimeline.events != null)
                    foreach (var ev in stage.stageTimeline.events) if (ev != null) { ev.timeMinutes *= scale; ev.duration *= scale; }
                map.stages[i] = stage;
            }
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    private static bool Text(string value, int limit) => !string.IsNullOrWhiteSpace(value) && value.Length <= limit && value.IndexOf('\0') < 0;
    private static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    public void Dispose()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) { if (Application.isPlaying) Object.Destroy(owned[i]); else Object.DestroyImmediate(owned[i]); }
        owned.Clear(); Map = null;
    }
}
