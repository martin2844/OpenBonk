#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in observer for a disposable external-map run.
internal sealed class AndroidExternalMapEnemyPoolProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "openbonk_map_enemy_pool_probe", false)) return;
        var go = new GameObject("External map enemy pool probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalMapEnemyPoolProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (!(MapController.currentMap is OpenBonkExternalMapData) ||
            MapController.currentStage == null || EnemyManager.Instance == null || EnemyManager.Instance.summonerController == null ||
            EnemyManager.Instance.summonerController.stageSummoner == null)) yield return null;
        if (!(MapController.currentMap is OpenBonkExternalMapData map) || EnemyManager.Instance?.summonerController?.stageSummoner == null)
        { Debug.LogError("External map enemy pool probe INCONCLUSIVE: map or summoner unavailable"); yield break; }
        var actual = (List<EEnemy>)typeof(StageSummoner).GetField("enemyPool", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(EnemyManager.Instance.summonerController.stageSummoner);
        var expected = new HashSet<EEnemy>(); int oldCount = 0;
        var defaults = MapController.currentStage.stageTimeline.startEnemies;
        foreach (var enemy in DataManager.Instance.unsortedEnemies)
        {
            if (enemy.minStage > MapController.GetStageIndex() || defaults.Contains(enemy.enemyName)) continue;
            if ((enemy.maps & EMap.Forest) == EMap.Forest) expected.Add(enemy.enemyName);
            if ((enemy.maps & map.eMap) == map.eMap) oldCount++;
        }
        var actualSet = new HashSet<EEnemy>(actual);
        bool passed = expected.Count > 0 && actualSet.SetEquals(expected) && oldCount == 0;
        Debug.Log($"External map enemy pool probe {(passed ? "PASS" : "FAIL")}: actual={actualSet.Count}, expectedForest={expected.Count}, oldIdMaskMatches={oldCount}, stage={MapController.GetStageIndex()}");
        var summoner = EnemyManager.Instance.summonerController.stageSummoner;
        float baseline = (1f + MyTime.stageTimer / 60f * .5f) * PlayerStats.GetStat(EStat.EnemyAmountMultiplier);
        float expectedMultiplier = map.luaStageSpawnMultiplier ? 2f : 1f;
        bool rateOk = Mathf.Approximately(summoner.MapSpawnMultiplier, expectedMultiplier) && Mathf.Approximately(summoner.GetBaseCreditsPerSecondUncapped(), baseline * expectedMultiplier);
        Debug.Log($"External map Lua spawn probe {(rateOk ? "PASS" : "FAIL")}: declared={map.luaStageSpawnMultiplier}, multiplier={summoner.MapSpawnMultiplier}, baseline={baseline}, rate={summoner.GetBaseCreditsPerSecondUncapped()}");
    }
}
#endif
