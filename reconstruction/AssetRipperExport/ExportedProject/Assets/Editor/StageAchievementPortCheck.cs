using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class StageAchievementPortCheck
{
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    static void Require(bool b, string s) { if (!b) throw new Exception("StageAchievementPortCheck: " + s); }
    static FieldInfo Field(string name) => typeof(AchievementTracker).GetField(name, Flags);
    public static void Run()
    {
        var root = new GameObject("stage achievement check"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldData = DataManager.Instance; var oldSave = SaveManager.Instance;
        var oldMap = MapController.currentMap; var oldIndex = MapController.index;
        var map = ScriptableObject.CreateInstance<MapData>();
        var setMap = typeof(MapController).GetProperty("currentMap").GetSetMethod(true);
        var asset = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_desert.asset");
        try
        {
            Require(asset != null, "exported desert achievement");
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            ((Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data)).Add("a_desert", asset);
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            map.eMap = EMap.Forest; setMap.Invoke(null, new object[] { map });
            AchievementTracker.Init(); AchievementTracker.Init();
            foreach (string name in new[] { "consecutiveIceCrystalCooks", "consecutiveMoldyCheeseCooks", "numBoomboxes" }) Field(name).SetValue(null, 3);
            player.character = ECharacter.Fox; MapController.index = 1; GameManager.A_StageStarted?.Invoke();
            foreach (string name in new[] { "consecutiveIceCrystalCooks", "consecutiveMoldyCheeseCooks", "numBoomboxes" }) Require((int)Field(name).GetValue(null) == 0, name + " reset");
            Require(!MyAchievements.IsAchievementDone("a_desert"), "different character");
            player.character = ECharacter.Cl4nk; MapController.index = 0; GameManager.A_StageStarted?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_desert"), "first stage");
            MapController.index = 1; map.eMap = EMap.Desert; GameManager.A_StageStarted?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_desert"), "different map");
            map.eMap = EMap.Forest; GameManager.A_StageStarted?.Invoke(); Require(MyAchievements.IsAchievementDone("a_desert"), "Cl4nk in second Forest stage");
            foreach (string name in new[] { "runChestsBought", "chargedShrines", "chargedShrinesNoInterruptions", "totalChargeShrines" }) Field(name).SetValue(null, 4);
            foreach (string name in new[] { "hasTakenDamageThisRun", "hasDealtDamageThisRun", "hasSpawnedLuckTomeQuest" }) Field(name).SetValue(null, true);
            GameManager.A_RunStarted?.Invoke();
            foreach (string name in new[] { "runChestsBought", "chargedShrines", "chargedShrinesNoInterruptions", "totalChargeShrines" }) Require((int)Field(name).GetValue(null) == 0, name + " reset");
            foreach (string name in new[] { "hasTakenDamageThisRun", "hasDealtDamageThisRun", "hasSpawnedLuckTomeQuest" }) Require(!(bool)Field(name).GetValue(null), name + " reset");
            save.progression.achievements.Clear(); AchievementTracker.Cleanup(); GameManager.A_StageStarted?.Invoke(); Require(!MyAchievements.IsAchievementDone("a_desert"), "cleanup unsubscribes");
            Debug.Log("StageAchievementPortCheck passed: reset fields, original Desert gates and event cleanup");
        }
        finally { AchievementTracker.Cleanup(); MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); setMap.Invoke(null, new object[] { oldMap }); MapController.index = oldIndex; UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root); }
    }
}
