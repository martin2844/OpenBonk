using System;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RequirementTextTestUnlockable : UnlockableBase
{
    public MyAchievement achievement;
    public UnlockableBase prerequisite;
    public override Texture GetIcon() => null;
    public override MyAchievement GetUnlockRequirement() => achievement;
    public override UnlockableBase GetUnlockableRequirement() => prerequisite;
    public override string GetUnlockableTypeDisplayString() => "test";
    public override string GetInternalName() => "test";
}

public static class UnlockRequirementTextPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception("UnlockRequirementTextPortCheck: " + message); }
    public static void Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.SelectedLocale = Locale.CreateLocale("es"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var state = new GameObject("requirement test"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[]{save});
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");
        var candidate = ScriptableObject.CreateInstance<RequirementTextTestUnlockable>(); candidate.isEnabled = true;
        try
        {
            Check(weapon != null && weapon.GetUnlockRequirement() != null && weapon.GetPrice() > 0, "real Revolver and achievement");
            candidate.achievement = weapon.GetUnlockRequirement(); candidate.prerequisite = weapon;
            string description = candidate.achievement.GetUnlockRequirement();
            string purchase = weapon.GetUnlockableTypeDisplayString() + " - " + weapon.GetName();
            Check(!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(weapon.GetName()), "Spanish asset text");
            string text;
            Check(!MyAchievements.IsUnlocked(candidate, out text), "both missing locked");
            Check(text == "<color=" + MyColorUtility.requirementMissingColor + ">* " + description + "\n<color=" + MyColorUtility.requirementMissingColor + ">* Purchase " + purchase + "\n", "missing lines: " + text);
            save.progression.achievements.Add(candidate.achievement.internalName);
            Check(!MyAchievements.IsUnlocked(candidate, out text) && text.StartsWith("<color=" + MyColorUtility.requirementCompletedColor) && text.Contains("\n<color=" + MyColorUtility.requirementMissingColor), "completed achievement and missing purchase");
            save.progression.purchases.Add(weapon.GetInternalName());
            Check(MyAchievements.IsUnlocked(candidate, out text) && !text.Contains(MyColorUtility.requirementMissingColor), "both complete");
            candidate.achievement = null; candidate.prerequisite = null;
            Check(MyAchievements.IsUnlocked(candidate, out text) && text == "", "no requirements");
            candidate.isEnabled = false;
            Check(!MyAchievements.IsUnlocked(candidate, out text) && text == "Available in the full release", "disabled asset");
            var clank = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Clank.asset");
            var fox = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset");
            Check(clank != null && fox != null && clank.weapon == weapon, "original Clank, Fox and Revolver assets");
            Check(clank.GetUnlockableRequirement() == weapon, "Clank requires its unlockable starting weapon");
            Check(fox.GetUnlockableRequirement() == null, "Fox's starting weapon has no unlock requirement");
            if (clank.GetUnlockRequirement() != null) save.progression.achievements.Add(clank.GetUnlockRequirement().internalName);
            save.progression.purchases.Remove(weapon.GetInternalName());
            Check(!MyAchievements.IsUnlocked(clank, out text) && text.Contains("Purchase"), "Clank remains locked before weapon purchase");
            save.progression.purchases.Add(weapon.GetInternalName());
            Check(MyAchievements.IsUnlocked(clank, out text), "Clank unlocks once requirements are met");
            Debug.Log("UnlockRequirementTextPortCheck passed: real Spanish achievement and weapon text, both color states, no requirements and disabled asset");
        }
        finally { UnityEngine.Object.DestroyImmediate(candidate); UnityEngine.Object.DestroyImmediate(state); }
    }
}
