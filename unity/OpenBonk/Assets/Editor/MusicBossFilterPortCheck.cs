using System;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public static class MusicBossFilterPortCheck
{
    public static void Run()
    {
        var root = new GameObject("music boss filter test"); root.SetActive(false);
        var map = ScriptableObject.CreateInstance<MapData>();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var statics = BindingFlags.NonPublic | BindingFlags.Static;
        var currentMap = typeof(MapController).GetProperty("currentMap");
        var oldMap = MapController.currentMap;
        var bossStage = typeof(MapController).GetField("isFinalBossStage", statics);
        bool oldBossStage = (bool)bossStage.GetValue(null), oldPause = MyTime.paused;
        var oldGame = GameManager.Instance; var oldEnemy = EnemyManager.Instance; var oldRsg = RsgController.Instance;
        try
        {
            var music = root.AddComponent<MusicController>();
            var calculate = typeof(MusicController).GetMethod("UpdateDesiredLowpass", flags);
            var next = typeof(MusicController).GetField("nextCheckTime", flags);
            var desired = typeof(MusicController).GetField("desiredLowpass", flags);
            Action<float, string> check = (expected, message) => { next.SetValue(music, -1f); calculate.Invoke(music, null); if ((float)desired.GetValue(music) != expected) throw new Exception(message + ": " + desired.GetValue(music)); };
            GameManager.Instance = root.AddComponent<GameManager>();
            var enemyManager = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemyManager;
			enemyManager.summonerController = (SummonerController)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SummonerController));
            var swarm = typeof(SummonerController).GetField("isFinalSwarmStarted", flags);
            var rsg = root.AddComponent<RsgController>(); RsgController.Instance = rsg;
            var graveBoss = root.AddComponent<GraveyardBossRoom>(); rsg.roomBoss = graveBoss;
            map.eMap = EMap.Graveyard; currentMap.GetSetMethod(true).Invoke(null, new object[] { map });
            bossStage.SetValue(null, false); MyTime.paused = false;
            graveBoss.isBossDefeated = false; check(22000f, "Graveyard before defeat");
            graveBoss.isBossDefeated = true; check(600f, "Graveyard after defeat");
            swarm.SetValue(enemyManager.summonerController, true); check(22000f, "Final swarm must keep the filter open");
            MyTime.paused = true; check(600f, "Pause must filter during final swarm");
            MyTime.paused = false; swarm.SetValue(enemyManager.summonerController, false);
            map.eMap = EMap.Forest; bossStage.SetValue(null, true);
            var fight = root.AddComponent<FinalFightController>();
            typeof(MusicController).GetField("finalFightController", flags).SetValue(music, fight);
            fight.boss = root.AddComponent<Enemy>(); check(22000f, "Final boss alive");
            fight.boss = null; check(600f, "Final boss defeated");
            swarm.SetValue(enemyManager.summonerController, true); check(22000f, "Final boss and final swarm");
            Debug.Log("MusicBossFilterPortCheck passed");
        }
        finally
        {
            currentMap.GetSetMethod(true).Invoke(null, new object[] { oldMap }); bossStage.SetValue(null, oldBossStage); MyTime.paused = oldPause;
            GameManager.Instance = oldGame; EnemyManager.Instance = oldEnemy; RsgController.Instance = oldRsg;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(map);
        }
    }
}
