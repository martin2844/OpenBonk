using System;
using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;
using Object = UnityEngine.Object;

// Experimental Forest-based recipe; this does not support arbitrary terrain assets.
[Serializable]
public sealed class OpenBonkProceduralMapRecipe
{
    public string name, description;
    public int tileGridSize;
    public float volatility, slopeStrength, flatMapBias, stageDuration;
}

// Owns cloned map objects and materials, never shared assets or supplied textures.
public sealed class OpenBonkProceduralMapBuild : IDisposable
{
    private readonly List<Object> owned = new List<Object>();
    public OpenBonkExternalMapData Map { get; private set; }
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    private OpenBonkProceduralMapBuild() { }

    public static OpenBonkProceduralMapBuild Create(MapData template, OpenBonkProceduralMapRecipe recipe, Texture icon, Texture groundTexture = null, Texture wallTexture = null)
    {
        if (template == null || template.eMap != EMap.Forest || template.mapType != EMapType.Tiles ||
            template.stages == null || template.stages.Length != 3 || recipe == null || icon == null ||
            !ValidText(recipe.name, 80) || !ValidText(recipe.description, 500) ||
            recipe.tileGridSize < 7 || recipe.tileGridSize > 20 || !Range(recipe.volatility, 0, 2) ||
            !Range(recipe.slopeStrength, -1, 1) || !Range(recipe.flatMapBias, -1, 1) || !Range(recipe.stageDuration, 60, 1800))
            throw new ArgumentException("Invalid procedural map recipe");
        foreach (var stage in template.stages)
            if (stage == null || stage.mapParameters == null || stage.stageTilePrefabs == null || stage.stageTimeline == null || !Range(stage.stageTimeline.stageTime, 1, 100000))
                throw new ArgumentException("Incomplete procedural map template");
        var result = new OpenBonkProceduralMapBuild();
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
            var groundMaterials = new Dictionary<Material, Material>();
            var wallMaterials = new Dictionary<Material, Material>();
            for (int i = 0; i < map.stages.Length; i++)
            {
                var stage = result.Own(Object.Instantiate(template.stages[i]));
                var parameters = result.Own(Object.Instantiate(stage.mapParameters));
                if (groundTexture != null && stage.flatMaterials != null)
                {
                    stage.flatMaterials = (Material[])stage.flatMaterials.Clone();
                    for (int materialIndex = 0; materialIndex < stage.flatMaterials.Length; materialIndex++)
                    {
                        stage.flatMaterials[materialIndex] = result.ReplaceTexture(stage.flatMaterials[materialIndex], groundTexture, groundMaterials);
                    }
                }
                if (wallTexture != null)
                {
                    stage.m_fillMiddle = result.ReplaceTexture(stage.m_fillMiddle, wallTexture, wallMaterials);
                    stage.m_fillTop = result.ReplaceTexture(stage.m_fillTop, wallTexture, wallMaterials);
                    stage.m_fillMiddleEdge = result.ReplaceTexture(stage.m_fillMiddleEdge, wallTexture, wallMaterials);
                    stage.m_fillTopEdge = result.ReplaceTexture(stage.m_fillTopEdge, wallTexture, wallMaterials);
                    stage.m_stairs = result.ReplaceTexture(stage.m_stairs, wallTexture, wallMaterials);
                }
                // Forest's challenge definitions explicitly target Forest, not this map.
                stage.challenges = Array.Empty<ChallengeData>();
                // StageTimeline is serialized inline, so Instantiate creates independent events.
                float timeScale = recipe.stageDuration / stage.stageTimeline.stageTime;
                stage.stageTimeline.stageTime = recipe.stageDuration;
                stage.stageTimeline.checkNewEnemyInterval *= timeScale;
                if (stage.stageTimeline.events != null)
                    foreach (var timelineEvent in stage.stageTimeline.events)
                        if (timelineEvent != null) { timelineEvent.timeMinutes *= timeScale; timelineEvent.duration *= timeScale; }
                parameters.size = recipe.tileGridSize; parameters.volatility = recipe.volatility;
                parameters.slopeStrength = recipe.slopeStrength; parameters.flatMapBias = recipe.flatMapBias;
                parameters.testStageData = stage; stage.mapParameters = parameters; map.stages[i] = stage;
            }
            if (groundTexture != null && groundMaterials.Count == 0) throw new ArgumentException("No ground materials in template");
            if (wallTexture != null && wallMaterials.Count == 0) throw new ArgumentException("No wall materials in template");
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    private Material ReplaceTexture(Material original, Texture texture, Dictionary<Material, Material> replacements)
    {
        if (original == null) return null;
        if (replacements.TryGetValue(original, out var existing)) return existing;
        var replacement = Own(Object.Instantiate(original));
        bool changed = false;
        foreach (var property in new[] { "_MainTex", "_BaseMap", "_AlbedoMap" })
            if (replacement.HasProperty(property)) { replacement.SetTexture(property, texture); changed = true; }
        if (!changed) throw new ArgumentException("Terrain material has no supported texture property");
        replacements.Add(original, replacement);
        return replacement;
    }

    private static bool ValidText(string text, int limit) => !string.IsNullOrWhiteSpace(text) && text.Length <= limit && text.IndexOf('\0') < 0;
    private static bool Range(float value, float min, float max) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    public void Dispose()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
            if (owned[i] != null) { if (Application.isPlaying) Object.Destroy(owned[i]); else Object.DestroyImmediate(owned[i]); }
        owned.Clear(); Map = null;
    }
}
