#if UNITY_EDITOR
using System;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

public static class OpenBonkUnlockPolicyCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var challenge = ScriptableObject.CreateInstance<ChallengeData>();
        try
        {
            OpenBonkUnlockPolicy.SetEnabled(false);
            Require(!MyAchievements.IsPurchased(null), "disabled policy must use original purchase check");
            OpenBonkUnlockPolicy.SetEnabled(true);
            Require(MyAchievements.IsAvailable(null), "availability");
            Require(MyAchievements.IsPurchased(null), "purchase");
            Require(MyAchievements.IsAchievementDone("not-an-achievement"), "achievement");
            Require(MyAchievements.IsUnlocked((Assets.Scripts.Saves___Serialization.Progression.Achievements.MyAchievement)null), "unlock");
            Require(character.CanBuy(), "purchase eligibility");
            Require(!character.IsBlackedOutInCharacterSelectionScreen(), "character visibility");
            Require(challenge.CanShow(), "challenge visibility");
            OpenBonkUnlockPolicy.SetEnabled(false);
            Require(!MyAchievements.IsPurchased(null), "disable must restore original purchase check");
            Debug.Log("OpenBonkUnlockPolicyCheck PASS");
        }
        finally
        {
            OpenBonkUnlockPolicy.SetEnabled(false);
            UnityEngine.Object.DestroyImmediate(character);
            UnityEngine.Object.DestroyImmediate(challenge);
        }
    }
}
#endif
