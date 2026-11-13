using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEditor;
using UnityEngine;

public static class GraveyardSpawnGateCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception("GraveyardSpawnGateCheck: " + message); }
    static void Set(object instance, string name, bool value) => instance.GetType().GetProperty(name).GetSetMethod(true).Invoke(instance, new object[] { value });
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldRsg = RsgController.Instance; var oldMap = MapController.currentMap;
        var setMap = typeof(MapController).GetProperty("currentMap").GetSetMethod(true);
        var namesField = typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic);
        var oldNames = namesField.GetValue(null); var names = new HashSet<string>();
        var root = new GameObject("inactive spawn gate fixture"); root.SetActive(false);
        MapData map = null;
        try
        {
            GameManager.Instance = root.AddComponent<GameManager>();
            RsgController.Instance = root.AddComponent<RsgController>();
            RsgController.Instance.roomBoss = root.AddComponent<GraveyardBossRoom>();
            namesField.SetValue(null, names);
            map = ScriptableObject.CreateInstance<MapData>(); map.eMap = EMap.Graveyard; setMap.Invoke(null, new object[] { map });
            var summoner = (StageSummoner)FormatterServices.GetUninitializedObject(typeof(StageSummoner));
            var check = typeof(StageSummoner).GetMethod("IsGainingCredits", P);
            Func<bool> gaining = () => (bool)check.Invoke(summoner, null);
            Set(GameManager.Instance, "isCrypt", false); Set(GameManager.Instance, "isDungeonTimerStarted", false); Set(GameManager.Instance, "isDungeonOvertime", false);
            Check(gaining(), "outside crypt gains credits");
            Set(GameManager.Instance, "isCrypt", true);
            Check(!gaining(), "inside crypt before timer does not gain credits");
            Set(GameManager.Instance, "isDungeonTimerStarted", true);
            Check(gaining(), "ordinary crypt while timer active gains credits");
            names.Add(ChallengesTracker.MODIFIER_CRYPT);
            Check(!gaining(), "crypt challenge disables ordinary credits");
            names.Clear(); Set(GameManager.Instance, "isDungeonOvertime", true);
            Check(!gaining(), "overtime disables ordinary credits");
            Check(summoner.GetBaseCreditsPerSecond() == 0f, "overtime prevents credit accumulation");
            Set(GameManager.Instance, "isDungeonOvertime", false);
            Set(RsgController.Instance.roomBoss, "isFightingBoss", true);
            Check(!gaining(), "active graveyard boss fight disables credits");
            Set(RsgController.Instance.roomBoss, "isFightingBoss", false);
            Set(GameManager.Instance, "isCrypt", false); Set(GameManager.Instance, "isDungeonOvertime", true);
            Check(gaining(), "outdoor progress is not blocked by stale overtime flag");
            map.eMap = EMap.Forest;
            Set(GameManager.Instance, "isCrypt", true); Set(GameManager.Instance, "isDungeonTimerStarted", false);
            Set(RsgController.Instance.roomBoss, "isFightingBoss", true); names.Add(ChallengesTracker.MODIFIER_CRYPT);
            Check(gaining(), "other maps bypass graveyard-specific restrictions");
            Debug.Log("GraveyardSpawnGateCheck passed: original crypt timer, challenge, overtime and boss-room gate");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally
        {
            namesField.SetValue(null, oldNames); setMap.Invoke(null, new object[] { oldMap });
            GameManager.Instance = oldManager; RsgController.Instance = oldRsg;
            UnityEngine.Object.DestroyImmediate(root); if (map != null) UnityEngine.Object.DestroyImmediate(map);
        }
    }
}
