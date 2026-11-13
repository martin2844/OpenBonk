#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable emulator check; expected multiplier is supplied by intent.
internal sealed class AndroidMapHealthMultiplierProbe : MonoBehaviour
{
    private float expected;
    private bool checkTransition;
    private bool checkRestart;
    private bool checkStats;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_map_health_probe", false)) return;
        var root = new GameObject("Map health probe"); DontDestroyOnLoad(root);
        var probe = root.AddComponent<AndroidMapHealthMultiplierProbe>();
        probe.expected = intent.Call<float>("getFloatExtra", "openbonk_map_health_expected", 2f);
        probe.checkTransition = intent.Call<bool>("getBooleanExtra", "openbonk_map_health_transition_probe", false);
        probe.checkRestart = intent.Call<bool>("getBooleanExtra", "openbonk_map_health_restart_probe", false);
        probe.checkStats = intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_stats_probe", false);
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MapGenerationController.isGenerating || EnemyManager.Instance == null || DataManager.Instance == null || !(MapController.currentMap is OpenBonkExternalMapData))) yield return null;
        var map = MapController.currentMap as OpenBonkExternalMapData;
        if (map == null || MyPlayer.Instance == null || EnemyManager.Instance == null || !map.luaStageEnemyHealthMultiplier) { Debug.LogError("Map health probe INCONCLUSIVE: map unavailable"); yield break; }
        int stage = MapController.index;
        var player = MyPlayer.Instance;
        if (!Measure(map, expected, checkStats)) yield break;
        if (!checkTransition) yield break;
        while (Time.realtimeSinceStartup < deadline && (MapController.index != stage + 1 || MyPlayer.Instance == null || MyPlayer.Instance == player || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null)) yield return null;
        if (MapController.index != stage + 1 || MyPlayer.Instance == null || MyPlayer.Instance == player || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null) { Debug.LogError("Map health probe INCONCLUSIVE: next stage unavailable"); yield break; }
        if (!Measure(map, 3f, checkStats) || !checkRestart) yield break;
        // Let the independent stage-transition diagnostic finish its checks first.
        yield return new WaitForSecondsRealtime(5f);
        var stageTwoPlayer = MyPlayer.Instance;
        MapController.RestartRun();
        while (Time.realtimeSinceStartup < deadline && (MapController.index != 0 || MyPlayer.Instance == null || MyPlayer.Instance == stageTwoPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null)) yield return null;
        if (MapController.index != 0 || MyPlayer.Instance == null || MyPlayer.Instance == stageTwoPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null) { Debug.LogError("Map health probe INCONCLUSIVE: restarted run unavailable"); yield break; }
        bool restartPass = Measure(map, 2f, checkStats);
        Debug.Log($"Map health restart probe {(restartPass ? "PASS" : "FAIL")}");
    }
    private static bool Measure(OpenBonkExternalMapData map, float expected, bool checkStats)
    {
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = data == null ? null : EnemyManager.Instance.SpawnEnemy(data, MyPlayer.Instance.transform.position + Vector3.right * 10f, -901, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Map health probe INCONCLUSIVE: enemy unavailable"); return false; }
        float spawned = enemy.hp;
        map.luaStageEnemyHealthMultiplier = false; float baseline = EnemyStats.GetHp(enemy);
        map.luaStageEnemyHealthMultiplier = true; float first = EnemyStats.GetHp(enemy); float repeated = EnemyStats.GetHp(enemy);
        var boss = EnemyManager.Instance.SpawnBoss(EEnemy.Ent1, -902, Assets.Scripts.Actors.Enemies.EEnemyFlag.StageBoss, MyPlayer.Instance.transform.position + Vector3.right * 20f);
        float bossBase = 0f, bossScaled = 0f;
        if (boss != null)
        {
            map.luaStageEnemyHealthMultiplier = false; bossBase = EnemyStats.GetHp(boss);
            map.luaStageEnemyHealthMultiplier = true; bossScaled = EnemyStats.GetHp(boss);
        }
        var ghost = DataManager.Instance.GetEnemyData(EEnemy.GhostInvincible);
        var setter = typeof(Assets.Scripts.Actors.Enemies.Enemy).GetProperty("enemyData").GetSetMethod(true);
        var savedData = enemy.enemyData; float ghostHp = 0f;
        try { if (ghost != null) { setter.Invoke(enemy, new object[] { ghost }); ghostHp = EnemyStats.GetHp(enemy); } }
        finally { setter.Invoke(enemy, new object[] { savedData }); }
        bool pass = baseline > 0f && Mathf.Abs(first / baseline - expected) < .001f && Mathf.Abs(repeated / baseline - expected) < .001f && Mathf.Abs(spawned / baseline - expected) < .01f && bossBase > 0f && Mathf.Abs(bossScaled / bossBase - expected) < .001f && ghostHp == float.MaxValue;
        Debug.Log($"Map health probe {(pass ? "PASS" : "FAIL")}: stage={MapController.index + 1} baseline={baseline:F3} spawned={spawned:F3} first={first:F3} repeated={repeated:F3} bossBase={bossBase:F3} bossScaled={bossScaled:F3} ghostPreserved={ghostHp == float.MaxValue} expected={expected:F3}");
        if (checkStats)
        {
            bool declared = map.luaStageEnemySpeedMultiplier && map.luaStageEnemyDamageMultiplier;
            float actualMove = enemy.GetSpeed();
            float actualContact = DamageUtility.GetPlayerDamage(enemy, Vector3.forward, DcFlags.IgnoreArmor).damage;
            var special = new EnemySpecialAttack { damageMultiplier = 1.7f };
            float actualSpecial = special.GetDamage(enemy);
            map.luaStageEnemySpeedMultiplier = false; map.luaStageEnemyDamageMultiplier = false;
            float speedBase = EnemyStats.GetSpeed(enemy.enemyData), damageBase = EnemyStats.GetDamage(enemy);
            float moveBase = enemy.GetSpeed();
            float contactBase = DamageUtility.GetPlayerDamage(enemy, Vector3.forward, DcFlags.IgnoreArmor).damage;
            float specialBase = special.GetDamage(enemy);
            map.luaStageEnemySpeedMultiplier = declared; map.luaStageEnemyDamageMultiplier = declared;
            float speed = EnemyStats.GetSpeed(enemy.enemyData), damage = EnemyStats.GetDamage(enemy);
            float speedAgain = EnemyStats.GetSpeed(enemy.enemyData), damageAgain = EnemyStats.GetDamage(enemy);
            bool statsPass = declared && speedBase > 0f && damageBase > 0f && moveBase > 0f && contactBase > 1f && specialBase > 0f && Mathf.Abs(speed / speedBase - .5f) < .001f && Mathf.Abs(speedAgain / speedBase - .5f) < .001f && Mathf.Abs(actualMove / moveBase - .5f) < .001f && Mathf.Abs(damage / damageBase - 1.5f) < .001f && Mathf.Abs(damageAgain / damageBase - 1.5f) < .001f && Mathf.Abs(actualContact / contactBase - 1.5f) < .001f && Mathf.Abs(actualSpecial / specialBase - 1.5f) < .001f;
            Debug.Log($"Map enemy stats probe {(statsPass ? "PASS" : "FAIL")}: stage={MapController.index + 1} declared={declared} speedBase={speedBase:F3} speed={speed:F3} repeat={speedAgain:F3} moveBase={moveBase:F3} move={actualMove:F3} damageBase={damageBase:F3} damage={damage:F3} repeat={damageAgain:F3} contactBase={contactBase:F3} contact={actualContact:F3} specialBase={specialBase:F3} special={actualSpecial:F3}");
            pass &= statsPass;
        }
        enemy.ReleaseToPool();
        if (boss != null) boss.ReleaseToPool();
        return pass;
    }
}
#endif
