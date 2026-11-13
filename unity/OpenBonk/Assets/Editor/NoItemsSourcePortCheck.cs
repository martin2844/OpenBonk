using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NoItemsSourcePortCheck
{
    public static void Run()
    {
        var namesField = typeof(ChallengesTracker).GetField("modifierNames", BindingFlags.Static | BindingFlags.NonPublic);
        var previous = (HashSet<string>)namesField.GetValue(null);
        var start = typeof(BaseInteractable).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShrineMaoi.prefab");
        if (prefab == null) throw new Exception("Original Moai prefab missing");
        try
        {
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity");
            namesField.SetValue(null, new HashSet<string>());
            CheckSource(false, true, "normal gameplay");
            namesField.SetValue(null, new HashSet<string> { ChallengesTracker.MODIFIER_NO_ITEMS });
            CheckSource(false, false, "challenge blocks item source");
            CheckSource(true, true, "challenge keeps non-item source");
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
            CheckSource(false, true, "menu is exempt");
            Debug.Log("NoItemsSourcePortCheck passed: real Moai prefab, challenge gate, root, non-source and menu exemption");
        }
        finally { namesField.SetValue(null, previous); }
        void CheckSource(bool forceNonSource, bool expectedActive, string label)
        {
            var instance = (GameObject)UnityEngine.Object.Instantiate(prefab);
            try
            {
                var source = instance.GetComponentInChildren<BaseInteractable>(true);
                if (source == null || !source.isItemSource) throw new Exception("Moai asset is not an item source");
                if (forceNonSource) source.isItemSource = false;
                start.Invoke(source, null);
                if (instance.activeSelf != expectedActive) throw new Exception(label + ": root active=" + instance.activeSelf);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
