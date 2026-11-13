#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using Inventory__Items__Pickups;
using UnityEngine;

internal sealed class AndroidLuaStatsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_lua_stats_probe", false)) return;
        var go = new GameObject("Lua stats probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidLuaStatsProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory?.playerStats == null)) yield return null;
        if (MyPlayer.Instance == null || MyPlayer.Instance.inventory?.playerStats == null) { Debug.LogError("Lua stats probe INCONCLUSIVE: player unavailable"); yield break; }
        yield return null;
        var stats = MyPlayer.Instance.inventory.playerStats;
        Debug.Log($"Lua stats probe: movement={stats.GetStat(EStat.MoveSpeedMultiplier):F3}, damage={stats.GetStat(EStat.DamageMultiplier):F3}, xp={stats.GetStat(EStat.XpIncreaseMultiplier):F3}, gold={stats.GetStat(EStat.GoldIncreaseMultiplier):F3}, maxHealth={stats.GetStat(EStat.MaxHealth):F3}, duration={stats.GetStat(EStat.DurationMultiplier):F3}, extraJumps={stats.GetStat(EStat.ExtraJumps):F3}");
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (intent.Call<bool>("getBooleanExtra", "megabonk_lua_level_refresh_probe", false))
        {
            var xp = MyPlayer.Instance.inventory.playerXp;
            float[] health = new float[4], duration = new float[4];
            health[0] = stats.GetStat(EStat.MaxHealth); duration[0] = stats.GetStat(EStat.DurationMultiplier);
            for (int level = 1; level <= 3; level++)
            {
                xp.AddXp(XpUtility.LevelToXp(level) - xp.xp);
                MyPlayer.Instance.inventory.statInventory.Tick();
                stats.ForceUpdateStats();
                health[level] = stats.GetStat(EStat.MaxHealth); duration[level] = stats.GetStat(EStat.DurationMultiplier);
            }
            // Fox also gains one vanilla max-health point per level in this fixture.
            bool pass = xp.level == 3 && Mathf.Abs(health[0] - 100) < .01f && Mathf.Abs(health[1] - 111) < .01f && Mathf.Abs(health[2] - 112) < .01f && Mathf.Abs(health[3] - 133) < .01f && Mathf.Abs(duration[0] - 1) < .01f && Mathf.Abs(duration[1] - 1.1f) < .01f && Mathf.Abs(duration[2] - 1.1f) < .01f && Mathf.Abs(duration[3] - 1.3f) < .01f;
            Debug.Log($"Lua level refresh {(pass ? "PASS" : "FAIL")}: health={string.Join(",", health)} duration={string.Join(",", duration)} level={xp.level}");
        }
        Destroy(gameObject);
    }
}
#endif
