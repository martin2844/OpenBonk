using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.Hats;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class BossProgressionPortCheck
{
    static void Require(bool b, string s) { if (!b) throw new Exception("BossProgressionPortCheck: " + s); }
    public static void Run()
    {
        var root = new GameObject("boss progression check"); root.SetActive(false);
        var oldData = DataManager.Instance; var oldSave = SaveManager.Instance; var oldPlayer = MyPlayer.Instance; var oldMap = MapController.currentMap; int oldIndex = MapController.index;
        var finalField = typeof(MapController).GetField("isFinalBossStage", BindingFlags.Static | BindingFlags.NonPublic); bool oldFinal = (bool)finalField.GetValue(null);
        var setMap = typeof(MapController).GetProperty("currentMap").GetSetMethod(true); var map = ScriptableObject.CreateInstance<MapData>(); var hat = ScriptableObject.CreateInstance<HatData>();
        try
        {
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);
            foreach (string name in new[] { "a_hatHeadset", "a_skin_megachadKevin" }) { var a = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Require(a != null, name); dict.Add(name, a); }
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerRenderer = root.AddComponent<PlayerRenderer>();
            typeof(PlayerRenderer).GetField("currentHat", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player.playerRenderer, hat);
            setMap.Invoke(null, new object[] { map }); AchievementTracker.Init(); AchievementTracker.Init();
            map.eMap = EMap.Desert; player.character = ECharacter.Ninja; MapController.index = 0;
            InteractableBossSpawner.A_NumBossesDefeated?.Invoke(2); Require(!MyAchievements.IsAchievementDone("a_hatHeadset"), "boss count below");
            MapController.index = 1; InteractableBossSpawner.A_NumBossesDefeated?.Invoke(3); Require(!MyAchievements.IsAchievementDone("a_hatHeadset"), "wrong stage");
            MapController.index = 0; player.character = ECharacter.Fox; InteractableBossSpawner.A_NumBossesDefeated?.Invoke(3); Require(!MyAchievements.IsAchievementDone("a_hatHeadset"), "wrong character");
            player.character = ECharacter.Ninja; map.eMap = EMap.Forest; InteractableBossSpawner.A_NumBossesDefeated?.Invoke(3); Require(!MyAchievements.IsAchievementDone("a_hatHeadset"), "wrong map");
            map.eMap = EMap.Desert; InteractableBossSpawner.A_NumBossesDefeated?.Invoke(3); Require(MyAchievements.IsAchievementDone("a_hatHeadset"), "third boss");
            map.eMap = EMap.Forest; player.character = ECharacter.Megachad; hat.eHat = EHat.Kevin; finalField.SetValue(null, true);
            FinalFightController.A_BossDefeatedTime?.Invoke(60.01f); Require(!MyAchievements.IsAchievementDone("a_skin_megachadKevin"), "too slow");
            hat.eHat = EHat.Pot; FinalFightController.A_BossDefeatedTime?.Invoke(60f); Require(!MyAchievements.IsAchievementDone("a_skin_megachadKevin"), "wrong hat");
            hat.eHat = EHat.Kevin; finalField.SetValue(null, false); FinalFightController.A_BossDefeatedTime?.Invoke(60f); Require(!MyAchievements.IsAchievementDone("a_skin_megachadKevin"), "not final stage");
            finalField.SetValue(null, true); FinalFightController.A_BossDefeatedTime?.Invoke(60f); Require(MyAchievements.IsAchievementDone("a_skin_megachadKevin"), "inclusive time");
            save.progression.achievements.Clear(); AchievementTracker.Cleanup(); FinalFightController.A_BossDefeatedTime?.Invoke(1f); Require(!MyAchievements.IsAchievementDone("a_skin_megachadKevin"), "cleanup");
            Debug.Log("BossProgressionPortCheck passed: boss count, character, map, stage, hat and time gates");
        }
        finally { AchievementTracker.Cleanup(); DataManager.Instance = oldData; MyPlayer.Instance = oldPlayer; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); setMap.Invoke(null, new object[] { oldMap }); MapController.index = oldIndex; finalField.SetValue(null, oldFinal); UnityEngine.Object.DestroyImmediate(hat); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root); }
    }
}
