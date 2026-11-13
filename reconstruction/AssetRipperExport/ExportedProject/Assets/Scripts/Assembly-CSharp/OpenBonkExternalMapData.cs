using Assets.Scripts._Data.MapsAndStages;

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
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
    public override string GetInternalName() => externalId ?? string.Empty;
    public static EMap EnemyPoolMap(MapData map) => map is OpenBonkExternalMapData external ? external.enemyPoolMap : map.eMap;
}
