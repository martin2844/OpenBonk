using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class ChestNavigationBindingCheck {
    const string Pending = "ChestNavigationBindingCheck.pending";
    static ChestNavigationBindingCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return; SessionState.SetBool(Pending, false); try { Validate(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ChestNavigationBindingCheck: " + message); }
    static void Validate() {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
        var axes = new SerializedObject(asset).FindProperty("m_Axes");
        var normal = new List<SerializedProperty>(); var chest = new List<SerializedProperty>();
        for (int i = 0; i < axes.arraySize; i++) { var a = axes.GetArrayElementAtIndex(i); string name = a.FindPropertyRelative("m_Name").stringValue; if (name == "Horizontal") normal.Add(a.Copy()); if (name == "NativeChestHorizontal") chest.Add(a.Copy()); }
        Check(normal.Count > 0 && normal.Count == chest.Count, "axis definitions copied");
        string[] fields = {"negativeButton", "positiveButton", "gravity", "dead", "sensitivity", "snap", "invert", "type", "axis", "joyNum"};
        for (int i = 0; i < normal.Count; i++) {
            foreach (string field in fields) Check(SerializedProperty.DataEquals(normal[i].FindPropertyRelative(field), chest[i].FindPropertyRelative(field)), "axis " + i + " changed " + field);
            Check(chest[i].FindPropertyRelative("altNegativeButton").stringValue != "a" && chest[i].FindPropertyRelative("altPositiveButton").stringValue != "d", "A/D disabled");
        }
        var go = new GameObject("event system", typeof(EventSystem), typeof(StandaloneInputModule)); var module = go.GetComponent<StandaloneInputModule>(); EventSystem.current = go.GetComponent<EventSystem>();
        var refresh = typeof(AndroidNativeInput).GetMethod("RefreshChestHorizontalNavigation", BindingFlags.NonPublic | BindingFlags.Static);
        refresh.Invoke(null, new object[] { true, false }); Check(module.horizontalAxis == "NativeChestHorizontal", "chest disables A/D axis");
        refresh.Invoke(null, new object[] { true, true }); Check(module.horizontalAxis == "Horizontal", "setting allows A/D");
        refresh.Invoke(null, new object[] { true, false }); refresh.Invoke(null, new object[] { false, false }); Check(module.horizontalAxis == "Horizontal", "closing chest restores axis");
        UnityEngine.Object.DestroyImmediate(go); Debug.Log("ChestNavigationBindingCheck PASSED: A/D gating and unchanged gamepad and arrow axes");
    }
}
