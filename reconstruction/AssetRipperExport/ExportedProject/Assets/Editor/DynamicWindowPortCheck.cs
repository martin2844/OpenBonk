using System;
using System.Reflection;
using Assets.Scripts.UI.DynamicWindows;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class DynamicWindowPortCheck
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        LocalizationSettings.Instance = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        var root = new GameObject("DynamicWindowCheck", typeof(RectTransform));
        try
        {
            var manager = root.AddComponent<DynamicWindows>();
            manager.contentParent = root.transform;
            var overlay = new GameObject("Overlay", typeof(RectTransform));
            overlay.transform.SetParent(root.transform);
            overlay.SetActive(false);
            manager.overlay = overlay.transform;
            manager.dynamicWindowPromptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DynamicWindowPrompt.prefab");
            Require(manager.dynamicWindowPromptPrefab != null, "original prompt prefab not found");
            Require(!manager.HasWindows(), "overlay alone should not count as a window");
            int accepted = 0;
            manager.NewWindowPrompt("Test heading", "Test content", () => accepted++);
            Require(manager.HasWindows() && root.transform.childCount == 2, "prompt was not instantiated");
            var prompt = root.transform.GetChild(1).GetComponent<DWindowPrompt>();
            Require(prompt != null && prompt.t_header.text == "Test heading" && prompt.t_content.text == "Test content", "prompt text not set");
            typeof(DynamicWindows).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
            Require(overlay.activeSelf, "overlay not enabled for prompt");
            var callback = (Action)typeof(DWindowPrompt).GetField("A_Accept", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(prompt);
            callback();
            Require(accepted == 1, "accept callback not retained");
            UnityEngine.Object.DestroyImmediate(prompt.gameObject);
            typeof(DynamicWindows).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, null);
            Require(!manager.HasWindows() && !overlay.activeSelf, "overlay remained after prompt removal");
            Debug.Log("DynamicWindowPortCheck passed: original prompt prefab, text, callback, overlay and cleanup");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
