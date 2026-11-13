using System.Collections.Generic;
using Actors.Enemies;

// One map Lua state per run; invalid scripts fall back to normal spawning.
internal static class OpenBonkMapLuaHooks
{
    private static OpenBonkExternalMapData activeMap;
    private static OpenBonkLuaItemSession session;
    private static int healthStage = -1;
    private static float healthMultiplier = 1f;
    private static int speedStage = -1, damageStage = -1;
    private static float speedMultiplier = 1f, damageMultiplier = 1f;
    private static readonly Dictionary<(int stage, EEnemy enemy, int kind), float> enemyMultipliers = new Dictionary<(int, EEnemy, int), float>();

    internal static void Reset()
    {
        session?.Dispose(); session = null; activeMap = null;
        healthStage = speedStage = damageStage = -1;
        healthMultiplier = speedMultiplier = damageMultiplier = 1f;
        enemyMultipliers.Clear();
    }

    internal static float StageSpawnMultiplier(MapData map, int stage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!(map is OpenBonkExternalMapData external) || !external.luaStageSpawnMultiplier) return 1f;
        EnsureSession(external);
        return session != null && session.Invoke("stage_spawn_multiplier", stage, out float value) ? value : 1f;
#else
        return 1f;
#endif
    }

    internal static float StageEnemyHealthMultiplier(MapData map, int stage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!(map is OpenBonkExternalMapData external) || !external.luaStageEnemyHealthMultiplier) return 1f;
        EnsureSession(external);
        if (session == null || !session.IsAvailable) return 1f;
        if (healthStage != stage)
        {
            healthStage = stage;
            healthMultiplier = session != null && session.Invoke("stage_enemy_health_multiplier", stage, out float value) ? value : 1f;
        }
        return healthMultiplier;
#else
        return 1f;
#endif
    }
    internal static float StageEnemySpeedMultiplier(MapData map, int stage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!(map is OpenBonkExternalMapData external) || !external.luaStageEnemySpeedMultiplier) return 1f;
        EnsureSession(external);
        if (session == null || !session.IsAvailable) return 1f;
        if (speedStage != stage)
        {
            speedStage = stage;
            speedMultiplier = session != null && session.Invoke("stage_enemy_speed_multiplier", stage, out float value) ? value : 1f;
        }
        return speedMultiplier;
#else
        return 1f;
#endif
    }
    internal static float StageEnemyDamageMultiplier(MapData map, int stage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!(map is OpenBonkExternalMapData external) || !external.luaStageEnemyDamageMultiplier) return 1f;
        EnsureSession(external);
        if (session == null || !session.IsAvailable) return 1f;
        if (damageStage != stage)
        {
            damageStage = stage;
            damageMultiplier = session != null && session.Invoke("stage_enemy_damage_multiplier", stage, out float value) ? value : 1f;
        }
        return damageMultiplier;
#else
        return 1f;
#endif
    }
    // Cached by type and stage, not by individual enemy; kind: 0 health, 1 speed, 2 damage.
    internal static float EnemyMultiplier(MapData map, int stage, EEnemy enemy, int kind)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!(map is OpenBonkExternalMapData external)) return 1f;
        float stageValue = kind == 0 ? StageEnemyHealthMultiplier(map, stage) : kind == 1 ? StageEnemySpeedMultiplier(map, stage) : StageEnemyDamageMultiplier(map, stage);
        bool declared = kind == 0 ? external.luaEnemyHealthMultiplier : kind == 1 ? external.luaEnemySpeedMultiplier : external.luaEnemyDamageMultiplier;
        if (!declared) return stageValue;
        EnsureSession(external);
        if (session == null || !session.IsAvailable) return 1f;
        var key = (stage, enemy, kind);
        if (!enemyMultipliers.TryGetValue(key, out float value))
        {
            string hook = kind == 0 ? "enemy_health_multiplier" : kind == 1 ? "enemy_speed_multiplier" : "enemy_damage_multiplier";
            value = session.InvokeMapEnemy(hook, stage, enemy.ToString(), out float candidate) ? candidate : 1f;
            if (!session.IsAvailable) return 1f;
            enemyMultipliers.Add(key, value);
        }
        return stageValue * value;
#else
        return 1f;
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    private static void EnsureSession(OpenBonkExternalMapData map)
    {
        if (activeMap == map) return;
        Reset(); activeMap = map; session = new OpenBonkLuaItemSession(map.externalId, map.luaSource);
    }
#endif
}
