using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEditor;
using UnityEngine;

public static class AchievementTargetValuePortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("AchievementTargetValuePortCheck: " + message); }
    public static void Run()
    {
        var integer = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_hatPilot.asset");
        var fractional = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_luckTome.asset");
        var zero = AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_bush.asset");
        Require(integer != null && integer.targetValue == 1000 && integer.IsUsingTargetValue(), "original integer target");
        Require(fractional != null && fractional.targetValue == 0 && fractional.targetValueFloat == 0.01f && fractional.IsUsingTargetValue(), "original fractional target");
        Require(zero != null && zero.targetValue == 0 && zero.targetValueFloat == 0 && string.IsNullOrEmpty(zero.targetValueString) && !zero.IsUsingTargetValue(), "original absent target");
        var copy = UnityEngine.Object.Instantiate(fractional);
        try
        {
            copy.targetValue = 0; copy.targetValueFloat = 0; copy.targetValueString = "target";
            Require(copy.IsUsingTargetValue(), "string target");
            var getKeys = typeof(MyAchievement).GetMethod("GetKeys", BindingFlags.NonPublic | BindingFlags.Instance);
            var keys = (Dictionary<string, string>)getKeys.Invoke(copy, null); Require(keys.TryGetValue("value", out var value) && value == "target", "description includes string target");
            copy.targetValueString = ""; Require(!copy.IsUsingTargetValue(), "empty string excluded");
            keys = (Dictionary<string, string>)getKeys.Invoke(copy, null); Require(!keys.ContainsKey("value"), "description omits absent target");
            Debug.Log("AchievementTargetValuePortCheck passed: original integer, fractional and absent target assets and string description key");
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
    }
}
