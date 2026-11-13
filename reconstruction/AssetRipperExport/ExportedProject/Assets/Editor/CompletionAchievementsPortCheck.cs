using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class CompletionAchievementsPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("CompletionAchievementsPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("completion achievement fixture"); root.SetActive(false);
        var oldData = DataManager.Instance; var oldSave = SaveManager.Instance;
        var created = new List<UnityEngine.Object>();
        try
        {
            var save = root.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            var dict = (Dictionary<string, MyAchievement>)typeof(DataManager).GetField("achievementsData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data);
            MyAchievement Load(string name) { var original = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/" + name + ".asset"); Check(original != null, "asset " + name); var copy = UnityEngine.Object.Instantiate(original); created.Add(copy); copy.isEnabled = true; dict[name] = copy; return copy; }
            var all = Load("a_allQuests"); var everything = Load("a_unlockEverything"); var first = Load("a_noelle"); var second = Load("a_poisonGloves");
            data.unsortedAchievements = new List<MyAchievement> { all, first, second };
            var revolver = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset")); created.Add(revolver); revolver.isEnabled = true; revolver.price = 10;
            var sniper = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSniper.asset")); created.Add(sniper); sniper.isEnabled = true; sniper.price = 10;
            var disabled = UnityEngine.Object.Instantiate(sniper); created.Add(disabled); disabled.isEnabled = false; disabled.price = 10;
            data.unsortedWeapons = new List<WeaponData> { revolver, sniper, disabled };
            int events = 0; Action<string> onTry = _ => events++; MyAchievements.A_TryUnlock += onTry;
            try
            {
                AchievementTracker.Init(); AchievementTracker.Init();
                Check(MyAchievements.TryUnlock(first.internalName), "first achievement unlocked");
                Check(!MyAchievements.IsAchievementDone(all.internalName), "one unfinished quest blocks all-quests");
                Check(MyAchievements.TryUnlock(second.internalName), "last achievement unlocked");
                Check(MyAchievements.IsAchievementDone(all.internalName) && events == 3, "last quest triggers all-quests once without recursion");
                save.progression.purchases.Add(revolver.GetInternalName()); UnlocksFooter.A_Purchased?.Invoke(revolver);
                Check(!MyAchievements.IsAchievementDone(everything.internalName), "unpurchased enabled weapon blocks completion");
                save.progression.purchases.Add(sniper.GetInternalName()); UnlocksFooter.A_Purchased?.Invoke(sniper);
                Check(MyAchievements.IsAchievementDone(everything.internalName) && events == 4, "all enabled purchasables triggers unlock and skips disabled");
                AchievementTracker.Cleanup(); save.progression.achievements.Remove(all.internalName); save.progression.achievements.Remove(everything.internalName);
                MyAchievements.A_Unlocked?.Invoke(second); UnlocksFooter.A_Purchased?.Invoke(sniper);
                Check(!MyAchievements.IsAchievementDone(all.internalName) && !MyAchievements.IsAchievementDone(everything.internalName), "cleanup removes both callbacks");
                Debug.Log("CompletionAchievementsPortCheck passed: quest and purchase gates, disabled item, recursion, duplicate init and cleanup");
            }
            finally { AchievementTracker.Cleanup(); MyAchievements.A_TryUnlock -= onTry; }
        }
        finally
        {
            DataManager.Instance = oldData; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            foreach (var obj in created) UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
