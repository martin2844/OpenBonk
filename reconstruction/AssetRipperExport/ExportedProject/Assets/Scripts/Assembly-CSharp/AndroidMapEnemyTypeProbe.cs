#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable, opt-in Android probe for type-specific map multipliers.
internal sealed class AndroidMapEnemyTypeProbe : MonoBehaviour
{
    private bool transition;
    private bool restart;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_type_probe", false)) return;
        var root = new GameObject("Map enemy type probe"); DontDestroyOnLoad(root);
        var probe = root.AddComponent<AndroidMapEnemyTypeProbe>();
        probe.transition = intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_type_transition_probe", false);
        probe.restart = intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_type_restart_probe", false);
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 240f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MapGenerationController.isGenerating || EnemyManager.Instance == null || !(MapController.currentMap is OpenBonkExternalMapData))) yield return null;
        var map = MapController.currentMap as OpenBonkExternalMapData;
        if (map == null || !map.luaStageEnemyHealthMultiplier || !map.luaEnemyHealthMultiplier || !map.luaEnemySpeedMultiplier || !map.luaEnemyDamageMultiplier) { Debug.LogError("Map enemy type probe INCONCLUSIVE: map or hooks unavailable"); yield break; }
        var firstPlayer = MyPlayer.Instance;
        if (!Measure(map, 6f) || !transition) yield break;
        while (Time.realtimeSinceStartup < deadline && (MapController.index != 1 || MyPlayer.Instance == null || MyPlayer.Instance == firstPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null)) yield return null;
        if (MapController.index != 1 || MyPlayer.Instance == null || MyPlayer.Instance == firstPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null) { Debug.LogError("Map enemy type probe INCONCLUSIVE: next stage unavailable"); yield break; }
        if (!Measure(map, 8f) || !restart) yield break;
        yield return new WaitForSecondsRealtime(5f);
        var secondPlayer = MyPlayer.Instance;
        MapController.RestartRun();
        while (Time.realtimeSinceStartup < deadline && (MapController.index != 0 || MyPlayer.Instance == null || MyPlayer.Instance == secondPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null)) yield return null;
        if (MapController.index != 0 || MyPlayer.Instance == null || MyPlayer.Instance == secondPlayer || !MyPlayer.Instance.hasStarted || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || EnemyManager.Instance == null) { Debug.LogError("Map enemy type probe INCONCLUSIVE: restart unavailable"); yield break; }
        bool resetPass = Measure(map, 6f);
        Debug.Log($"Map enemy type restart {(resetPass ? "PASS" : "FAIL")}");
    }
    private static bool Measure(OpenBonkExternalMapData map, float expectedGoblinHealth)
    {
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var goblin = EnemyManager.Instance.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), MyPlayer.Instance.transform.position + Vector3.right * 10f, -901, true, canBeElite: false);
        var skeleton = EnemyManager.Instance.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Skeleton), MyPlayer.Instance.transform.position + Vector3.left * 10f, -902, true, canBeElite: false);
        if (goblin == null || skeleton == null) { Debug.LogError("Map enemy type probe INCONCLUSIVE: enemy unavailable"); return false; }
        float gh = EnemyStats.GetHp(goblin), sh = EnemyStats.GetHp(skeleton), gs = EnemyStats.GetSpeed(goblin.enemyData), ss = EnemyStats.GetSpeed(skeleton.enemyData), gd = EnemyStats.GetDamage(goblin), sd = EnemyStats.GetDamage(skeleton);
        float ghRepeat = EnemyStats.GetHp(goblin), gsRepeat = EnemyStats.GetSpeed(goblin.enemyData), gdRepeat = EnemyStats.GetDamage(goblin);
        map.luaStageEnemyHealthMultiplier = map.luaEnemyHealthMultiplier = map.luaEnemySpeedMultiplier = map.luaEnemyDamageMultiplier = false;
        float ghBase = EnemyStats.GetHp(goblin), shBase = EnemyStats.GetHp(skeleton), gsBase = EnemyStats.GetSpeed(goblin.enemyData), ssBase = EnemyStats.GetSpeed(skeleton.enemyData), gdBase = EnemyStats.GetDamage(goblin), sdBase = EnemyStats.GetDamage(skeleton);
        map.luaStageEnemyHealthMultiplier = map.luaEnemyHealthMultiplier = map.luaEnemySpeedMultiplier = map.luaEnemyDamageMultiplier = true;
        bool Near(float x, float y) => Mathf.Abs(x - y) < .001f;
        bool pass = ghBase > 0 && shBase > 0 && gsBase > 0 && ssBase > 0 && gdBase > 0 && sdBase > 0 &&
            Near(gh / ghBase, expectedGoblinHealth) && Near(sh / shBase, 2f) && Near(gs / gsBase, .5f) && Near(ss / ssBase, 1f) && Near(gd / gdBase, 1.5f) && Near(sd / sdBase, 1f) &&
            Near(ghRepeat, gh) && Near(gsRepeat, gs) && Near(gdRepeat, gd);
        Debug.Log($"Map enemy type probe {(pass ? "PASS" : "FAIL")}: stage={MapController.index + 1} gh={gh / ghBase:F3} sh={sh / shBase:F3} gs={gs / gsBase:F3} ss={ss / ssBase:F3} gd={gd / gdBase:F3} sd={sd / sdBase:F3} cache={Near(ghRepeat, gh) && Near(gsRepeat, gs) && Near(gdRepeat, gd)}");
        goblin.ReleaseToPool(); skeleton.ReleaseToPool();
        return pass;
    }
}
#endif
