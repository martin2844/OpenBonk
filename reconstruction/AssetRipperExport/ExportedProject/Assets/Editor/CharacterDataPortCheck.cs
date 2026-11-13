using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

public static class CharacterDataPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    public static void Run()
    {
        var root = new GameObject("CharacterDataCheck");
        var manager = root.AddComponent<SaveManager>();
        var previousManager = SaveManager.Instance;
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var achievement = ScriptableObject.CreateInstance<MyAchievement>();
        var icon = new Texture2D(2, 2);
        var originalXpTable = Inventory__Items__Pickups.XpUtility.xpForLevelsTable;
        try
        {
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { manager });
            manager.progression = new ProgressionSaveFile();
            Inventory__Items__Pickups.XpUtility.Init();
            character.eCharacter = ECharacter.Calcium;
            character.icon = icon;
            character.achievementRequirement = achievement;
            achievement.isEnabled = false;
            character.numQuestsRequiredForVisibilityInCharacterSelection = 100;
            Require(character.GetIcon() == icon && character.GetUnlockRequirement() == achievement, "character asset references");
            Require(character.GetInternalName() == ECharacter.Calcium.ToString(), "character identity");
            Require(achievement.IsUnlocked() && !character.IsBlackedOutInCharacterSelectionScreen(), "unlocked character visibility");
            Require(character.GetDisplayRank() == manager.progression.GetCharacterProgression(character.eCharacter).GetRank(), "saved character rank");
            Debug.Log("CharacterDataPortCheck passed: icon, requirement, identity, visibility, and saved rank");
        }
        finally
        {
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousManager });
            Inventory__Items__Pickups.XpUtility.xpForLevelsTable = originalXpTable;
            UnityEngine.Object.DestroyImmediate(icon);
            UnityEngine.Object.DestroyImmediate(achievement);
            UnityEngine.Object.DestroyImmediate(character);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
