#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using UnityEngine;

// Runs only with the explicit isolated-emulator intent; no gameplay is started.
internal static class AndroidMapLuaFailureProbe
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Run()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_map_failure_probe", false)) return;
        var map = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        try
        {
            map.externalId = "isolated-failure-check";
            map.luaStageSpawnMultiplier = map.luaStageEnemyHealthMultiplier = map.luaStageEnemySpeedMultiplier = map.luaStageEnemyDamageMultiplier = true;
            for (int failed = 0; failed < 4; failed++)
            {
                OpenBonkMapLuaHooks.Reset();
                string[] names = { "stage_spawn_multiplier", "stage_enemy_health_multiplier", "stage_enemy_speed_multiplier", "stage_enemy_damage_multiplier" };
                string[] values = { "4", "2", "0.5", "1.5" };
                values[failed] = "100";
                map.luaSource = "return {";
                for (int i = 0; i < names.Length; i++) map.luaSource += names[i] + "=function(stage) return " + values[i] + " end,";
                map.luaSource += "}";
                float Read(int index, int stage) => index == 0 ? OpenBonkMapLuaHooks.StageSpawnMultiplier(map, stage) : index == 1 ? OpenBonkMapLuaHooks.StageEnemyHealthMultiplier(map, stage) : index == 2 ? OpenBonkMapLuaHooks.StageEnemySpeedMultiplier(map, stage) : OpenBonkMapLuaHooks.StageEnemyDamageMultiplier(map, stage);
                bool pass = true;
                float[] expected = { 4f, 2f, .5f, 1.5f };
                for (int i = 0; i < names.Length; i++) if (i != failed) pass &= Mathf.Approximately(Read(i, 1), expected[i]);
                pass &= Mathf.Approximately(Read(failed, 1), 1f);
                for (int i = 0; i < names.Length; i++) pass &= Mathf.Approximately(Read(i, 1), 1f) && Mathf.Approximately(Read(i, 2), 1f);
                Debug.Log("Map Lua cached failure probe " + (pass ? "PASS " : "FAIL ") + names[failed]);
            }
        }
        finally { OpenBonkMapLuaHooks.Reset(); Object.Destroy(map); }
    }
}
#endif
