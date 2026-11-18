using System.Collections.Generic;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;

// The identity is separate from the original map used to choose inherited enemies.
public sealed class OpenBonkExternalMapData : MapData
{
    public string externalId, displayName, description;
    public string luaSource;
    public bool luaStageSpawnMultiplier;
    public bool luaStageEnemyHealthMultiplier;
    public bool luaStageEnemySpeedMultiplier, luaStageEnemyDamageMultiplier;
    public bool luaEnemyHealthMultiplier, luaEnemySpeedMultiplier, luaEnemyDamageMultiplier;
    public EMap enemyPoolMap = EMap.Forest;
    internal readonly Dictionary<StageData, (GameObject normal, GameObject silver)> potVisuals =
        new Dictionary<StageData, (GameObject normal, GameObject silver)>();
    internal GameObject GetPotVisual(StageData stage, bool silver) =>
        stage != null && potVisuals.TryGetValue(stage, out var visuals) ? (silver ? visuals.silver : visuals.normal) : null;
    internal readonly Dictionary<StageData, GameObject> potEffects = new Dictionary<StageData, GameObject>();
    internal readonly Dictionary<StageData, GameObject> arenaLandmarks = new Dictionary<StageData, GameObject>();
    internal readonly Dictionary<StageData, EItem> bossRewards = new Dictionary<StageData, EItem>();
    internal readonly Dictionary<StageData, OpenBonkRoofLayout.Profile> rooftops =
        new Dictionary<StageData, OpenBonkRoofLayout.Profile>();
    internal OpenBonkRoofLayout.Profile GetRooftops(StageData stage) =>
        stage != null && rooftops.TryGetValue(stage, out var profile) ? profile : null;
    internal readonly Dictionary<StageData, OpenBonkStageEncounter> encounters =
        new Dictionary<StageData, OpenBonkStageEncounter>();
    internal static OpenBonkStageEncounter GetEncounters(MapData map, StageData stage) =>
        map is OpenBonkExternalMapData external && stage != null && external.encounters.TryGetValue(stage, out var encounter)
            ? encounter : null;
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
    public override string GetInternalName() => externalId ?? string.Empty;
    public static EMap EnemyPoolMap(MapData map) => map is OpenBonkExternalMapData external ? external.enemyPoolMap : map.eMap;
}
