using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class InteractablePromptPositionCheck
{
    public static void Run()
    {
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShrineMaoi.prefab");
            if (prefab == null) throw new Exception("Moai prefab missing");
            var target = UnityEngine.Object.Instantiate(prefab);
            var interactable = target.GetComponent<BaseInteractable>();
            var host = new GameObject("detector");
            var prompt = new GameObject("prompt");
            var detector = host.AddComponent<DetectInteractables>();
            detector.uiParent = prompt.transform;
            target.transform.position = new Vector3(5f, 2f, -7f);
            typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, interactable);
            typeof(DetectInteractables).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(detector, null);
            var expected = target.transform.position + interactable.GetOffset() + Vector3.up * 3f;
            if (Vector3.Distance(prompt.transform.position, expected) > 0.001f) throw new Exception("Prompt position mismatch");
            typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, null);
            var fake = new GameObject("controlled interactable");
            fake.AddComponent<PromptPositionTestInteractable>();
            var start = typeof(DetectInteractables).GetMethod("StartHovering", BindingFlags.NonPublic | BindingFlags.Instance);
            var stop = typeof(DetectInteractables).GetMethod("StopHovering", BindingFlags.NonPublic | BindingFlags.Instance);
            var update = typeof(DetectInteractables).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            start.Invoke(detector, new object[] { fake });
            if (!prompt.activeSelf || prompt.transform.localScale != Vector3.zero) throw new Exception("Prompt should start at zero scale");
            MyTime.deltaTime = 0.15f;
            update.Invoke(detector, null);
            if (Mathf.Abs(prompt.transform.localScale.x - 0.5f) > 0.001f) throw new Exception("Prompt should be half open halfway through");
            MyTime.deltaTime = 0f;
            update.Invoke(detector, null);
            if (Mathf.Abs(prompt.transform.localScale.x - 0.5f) > 0.001f) throw new Exception("Prompt animation should respect paused game time");
            MyTime.deltaTime = 0.15f;
            update.Invoke(detector, null);
            if (Vector3.Distance(prompt.transform.localScale, Vector3.one) > 0.001f) throw new Exception("Prompt should finish opening");
            stop.Invoke(detector, null);
            if (!prompt.activeSelf) throw new Exception("Prompt should remain active while closing");
            update.Invoke(detector, null);
            if (Mathf.Abs(prompt.transform.localScale.x - 0.5f) > 0.001f) throw new Exception("Prompt should be half closed halfway through");
            update.Invoke(detector, null);
            if (prompt.activeSelf || prompt.transform.localScale != Vector3.zero) throw new Exception("Prompt should finish closing and deactivate");
            start.Invoke(detector, new object[] { fake });
            update.Invoke(detector, null);
            typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(detector, null);
            stop.Invoke(detector, null);
            update.Invoke(detector, null);
            update.Invoke(detector, null);
            if (prompt.activeSelf) throw new Exception("Prompt should close even if its interactable has already disappeared");
            MyTime.deltaTime = 0f;
            UnityEngine.Object.DestroyImmediate(fake);
            Debug.Log("InteractablePromptPositionCheck passed: original position, eased opening and closing, pause and deactivation");
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(prompt);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

public class PromptPositionTestInteractable : BaseInteractable
{
    public override bool Interact() => true;
    public override string GetInteractString() => "test";
}
