using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class ArmorAchievementPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ArmorAchievementPortCheck: " + message); }
    public static void Run()
    {
        var oldSave = SaveManager.Instance; var oldData = DataManager.Instance; var oldPlayer = MyPlayer.Instance; bool oldLoaded = SaveManager.loaded;
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var started = typeof(MyAchievements).GetField("startedTracking", flags); var trackers = typeof(MyAchievements).GetField("statTrackers", flags); var queue = typeof(MyAchievements).GetField("queuedStatNames", flags); var next = typeof(MyAchievements).GetField("nextStatTrackersCheck", flags);
        object oldStarted = started.GetValue(null), oldTrackers = trackers.GetValue(null), oldQueue = queue.GetValue(null), oldNext = next.GetValue(null);
        var root = new GameObject("armor achievement fixture"); root.SetActive(false);
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data; var aegis = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_aegis.asset"); Require(aegis != null && aegis.isEnabled && aegis.statName == "damageReductionArmorAsKnight" && aegis.targetValue == 500, "original Aegis asset");
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); dict.Add(aegis.internalName, aegis); data.unsortedAchievements = new List<MyAchievement> { aegis };
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.character = ECharacter.Fox;
            started.SetValue(null, false); trackers.SetValue(null, new Dictionary<string, List<MyAchievement>>()); queue.SetValue(null, new HashSet<MyStat>()); next.SetValue(null, -1f); SaveManager.loaded = true;
            MyAchievements.Init(); TrackStats.Init(); TrackStats.Init();
            var damage = new DamageContainer(1f, "fixture") { damageBlockedByArmor = 20 }; PlayerHealth.A_TakeDamage?.Invoke(null, damage, false);
            Require(MyStats.GetStat("damageReductionArmor") == 20 && MyStats.GetStat("damageReductionArmorAsKnight") == 0, "other character counts only total armor damage once");
            player.character = ECharacter.SirOofie; damage.damageBlockedByArmor = 499; PlayerHealth.A_TakeDamage?.Invoke(null, damage, false); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat("damageReductionArmorAsKnight") == 499 && !save.progression.achievements.Contains("a_aegis"), "499 blocked does not unlock");
            damage.damageBlockedByArmor = 1; PlayerHealth.A_TakeDamage?.Invoke(null, damage, false); next.SetValue(null, -1f); MyAchievements.Update();
            Require(MyStats.GetStat("damageReductionArmor") == 520 && MyStats.GetStat("damageReductionArmorAsKnight") == 500 && save.progression.achievements.Contains("a_aegis"), "500 blocked unlocks Aegis");
            TrackStats.Cleanup(); damage.damageBlockedByArmor = 1; PlayerHealth.A_TakeDamage?.Invoke(null, damage, false); Require(MyStats.GetStat("damageReductionArmor") == 520, "cleanup removes handler");
            Debug.Log("ArmorAchievementPortCheck passed: armor stats and original Aegis achievement at 500");
        }
        finally { TrackStats.Cleanup(); MyAchievements.Cleanup(); started.SetValue(null, oldStarted); trackers.SetValue(null, oldTrackers); queue.SetValue(null, oldQueue); next.SetValue(null, oldNext); SaveManager.loaded = oldLoaded; MyPlayer.Instance = oldPlayer; DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
