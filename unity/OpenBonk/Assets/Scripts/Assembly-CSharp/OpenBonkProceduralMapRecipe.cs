using System;
using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.MapGeneration;
using UnityEngine;
using Object = UnityEngine.Object;

// Experimental Forest-based recipe; this does not support arbitrary terrain assets.
[Serializable]
public sealed class OpenBonkProceduralMapRecipe
{
    public sealed class StageEnvironment
    {
        public int stage;
        public Texture groundTexture, wallTexture;
        public Material architectureMaterial, skyMaterial;
        public GameObject potVisual, silverPotVisual, arenaLandmark, potBreakFx, moaiVisual;
        public OpenBonkRoofLayout.Profile rooftops;
        public OpenBonkStreetLayout.Profile streetscape;
        public bool replaceDecorations;
    }

    public string name, description;
    public int tileGridSize;
    public float volatility, slopeStrength, flatMapBias, stageDuration;
    public StageEnvironment[] stageEnvironments;
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
        var environments = new OpenBonkProceduralMapRecipe.StageEnvironment[template.stages.Length];
        if (recipe.stageEnvironments != null)
        {
            if (recipe.stageEnvironments.Length == 0 || recipe.stageEnvironments.Length > environments.Length)
                throw new ArgumentException("Invalid stage environment count");
            foreach (var environment in recipe.stageEnvironments)
            {
                if (environment == null || environment.stage < 1 || environment.stage > environments.Length ||
                    environments[environment.stage - 1] != null ||
                    (environment.arenaLandmark != null && (environment.rooftops != null || !environment.replaceDecorations)) ||
                    (environment.rooftops != null && (!environment.replaceDecorations || !environment.rooftops.IsValid())) ||
                    (environment.streetscape != null && (!environment.replaceDecorations || environment.rooftops != null ||
                        environment.architectureMaterial == null || environment.skyMaterial == null)) ||
                    ((environment.architectureMaterial != null || environment.skyMaterial != null) &&
                        (environment.architectureMaterial == null || environment.skyMaterial == null)) ||
                    (!environment.replaceDecorations && environment.groundTexture == null && environment.wallTexture == null &&
                        environment.moaiVisual == null && environment.potVisual == null && environment.silverPotVisual == null && environment.potBreakFx == null &&
                        environment.architectureMaterial == null))
                    throw new ArgumentException("Invalid stage environment");
                environments[environment.stage - 1] = environment;
            }
        }
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
            var materials = new Dictionary<(Material, Texture), Material>();
            for (int i = 0; i < map.stages.Length; i++)
            {
                var stage = result.Own(Object.Instantiate(template.stages[i]));
                var parameters = result.Own(Object.Instantiate(stage.mapParameters));
                var environment = environments[i];
                if (environment?.streetscape != null)
                {
                    // Set dimensions before material-grid setup. Prefab geometry
                    // uses the matching opt-in nonuniform transform at generation.
                    parameters.scaledTileWidth = environment.streetscape.tileWidth;
                    parameters.scaledTileHeight = environment.streetscape.stepHeight;
                    map.streetscapes.Add(stage, environment.streetscape);
                }
                if (environment != null && (environment.potVisual != null || environment.silverPotVisual != null))
                    map.potVisuals.Add(stage, (environment.potVisual, environment.silverPotVisual));
                if (environment?.moaiVisual != null) map.moaiVisuals.Add(stage, environment.moaiVisual);
                if (environment?.arenaLandmark != null) map.arenaLandmarks.Add(stage, environment.arenaLandmark);
                if (environment?.potBreakFx != null) map.potEffects.Add(stage, environment.potBreakFx);
                if (environment?.rooftops != null)
                {
                    if (stage.flatMaterials == null || stage.flatMaterials.Length == 0 || stage.flatMaterials[0] == null ||
                        stage.m_fillMiddle == null || stage.m_fillTop == null || stage.mapParameters.scaledTileWidth < 10)
                        throw new ArgumentException("Missing rooftop template materials or tile size");
                    map.rooftops.Add(stage, environment.rooftops);
                }
                var ground = environment?.groundTexture ?? groundTexture;
                var wall = environment?.wallTexture ?? wallTexture;
                if (environment?.architectureMaterial != null)
                {
                    if (ground == null || wall == null || !environment.architectureMaterial.HasProperty("_MainTex"))
                        throw new ArgumentException("Missing architecture textures");
                    var deck = result.Own(new Material(environment.architectureMaterial) { mainTexture = ground });
                    var facade = result.Own(new Material(environment.architectureMaterial) { mainTexture = wall });
                    if (environment.rooftops != null)
                        map.rooftops[stage] = environment.rooftops.WithArchitecture(deck, facade, environment.architectureMaterial);
                    else
                        SetTerrainSurfaces(stage, deck, facade, parameters.scaledTileWidth, recipe.tileGridSize);
                    stage.skybox = environment.skyMaterial;
                    stage.fogColor = new Color(.75f, .86f, .89f);
                    stage.fogIntensity = .0035f;
                    stage.ambienceColor = new Color(.86f, .89f, .94f);
                    stage.lightColor = new Color(1f, .95f, .83f);
                    stage.lightIntensity = 1f;
                }
                if (ground != null && environment?.architectureMaterial == null)
                {
                    if (stage.flatMaterials == null || !Array.Exists(stage.flatMaterials, material => material != null))
                        throw new ArgumentException("No ground materials in stage template");
                    stage.flatMaterials = (Material[])stage.flatMaterials.Clone();
                    for (int materialIndex = 0; materialIndex < stage.flatMaterials.Length; materialIndex++)
                    {
                        stage.flatMaterials[materialIndex] = result.ReplaceTexture(stage.flatMaterials[materialIndex], ground, materials);
                    }
                }
                if (wall != null && environment?.architectureMaterial == null)
                {
                    if (stage.m_fillMiddle == null && stage.m_fillTop == null && stage.m_fillMiddleEdge == null &&
                        stage.m_fillTopEdge == null && stage.m_stairs == null)
                        throw new ArgumentException("No wall materials in stage template");
                    stage.m_fillMiddle = result.ReplaceTexture(stage.m_fillMiddle, wall, materials);
                    stage.m_fillTop = result.ReplaceTexture(stage.m_fillTop, wall, materials);
                    stage.m_fillMiddleEdge = result.ReplaceTexture(stage.m_fillMiddleEdge, wall, materials);
                    stage.m_fillTopEdge = result.ReplaceTexture(stage.m_fillTopEdge, wall, materials);
                    stage.m_stairs = result.ReplaceTexture(stage.m_stairs, wall, materials);
                }
                if (environment?.replaceDecorations == true)
                {
                    stage.grassMaterial = null;
                    stage.grassPerChunk = 0;
                    stage.particles = null;
                    stage.randomMapObjects = Array.Empty<RandomMapObject>();
                    stage.stageTilePrefabs = new StageTilePrefabs
                    {
                        flatTilePrefabs = Array.Empty<GameObject>(),
                        mapSpecificTilesPrefabs = Array.Empty<GameObject>(),
                    };
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
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    private Material ReplaceTexture(Material original, Texture texture, Dictionary<(Material, Texture), Material> replacements)
    {
        if (original == null) return null;
        var key = (original, texture);
        if (replacements.TryGetValue(key, out var existing)) return existing;
        var replacement = Own(Object.Instantiate(original));
        bool changed = false;
        foreach (var property in new[] { "_MainTex", "_BaseMap", "_AlbedoMap" })
            if (replacement.HasProperty(property)) { replacement.SetTexture(property, texture); changed = true; }
        if (!changed) throw new ArgumentException("Terrain material has no supported texture property");
        replacements.Add(key, replacement);
        return replacement;
    }

    private static void SetTerrainSurfaces(StageData stage, Material deck, Material facade, float tileWidth, int gridSize)
    {
        float gridOffset = gridSize % 2 == 0 ? tileWidth * .5f : 0;
        if (deck.HasProperty("_TileWidth")) deck.SetFloat("_TileWidth", tileWidth);
        if (facade.HasProperty("_TileWidth")) facade.SetFloat("_TileWidth", tileWidth);
        if (deck.HasProperty("_GridOffset")) deck.SetFloat("_GridOffset", gridOffset);
        if (facade.HasProperty("_GridOffset")) facade.SetFloat("_GridOffset", gridOffset);
        if (facade.HasProperty("_Facade")) facade.SetFloat("_Facade", 1);
        stage.flatMaterials = new[] { deck };
        stage.m_fillMiddle = stage.m_fillMiddleEdge = facade;
        stage.m_fillTop = stage.m_fillTopEdge = facade;
        stage.m_stairs = null;
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
