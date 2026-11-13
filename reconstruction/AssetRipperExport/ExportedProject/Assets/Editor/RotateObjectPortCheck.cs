using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;

public static class RotateObjectPortCheck
{
    public static void Run()
    {
        float oldDelta = MyTime.deltaTime;
        var parent = new GameObject("rotation fixture");
        GameObject instance = null, defaults = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/AxeProjectile.prefab");
            if (prefab == null) throw new Exception("Original axe projectile unavailable");
            instance = UnityEngine.Object.Instantiate(prefab, parent.transform);
            var rotate = instance.GetComponentInChildren<RotateObject>(true);
            if (rotate == null || !rotate.useMeTime || rotate.speed != 1000f || rotate.axis != Vector3.right)
                throw new Exception("Original axe rotation settings differ");
            var tick = typeof(RotateObject).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            parent.transform.rotation = Quaternion.Euler(20, 50, 30);
            Quaternion start = rotate.transform.localRotation;
            MyTime.deltaTime = .025f;
            tick.Invoke(rotate, null);
            Quaternion expected = start * Quaternion.AngleAxis(25f, Vector3.right);
            if (Quaternion.Angle(rotate.transform.localRotation, expected) > .01f) throw new Exception("Axe local rotation differs");
            MyTime.deltaTime = 0f;
            Quaternion paused = rotate.transform.rotation;
            tick.Invoke(rotate, null);
            if (Quaternion.Angle(paused, rotate.transform.rotation) > .01f) throw new Exception("Paused game clock still rotated axe");
            defaults = new GameObject("rotation defaults");
            var plain = defaults.AddComponent<RotateObject>();
            if (!plain.useMeTime || plain.axis != Vector3.up) throw new Exception("Original defaults differ");
            Debug.Log("RotateObjectPortCheck passed: actual axe local rotation, paused clock and default values");
        }
        finally
        {
            MyTime.deltaTime = oldDelta;
            UnityEngine.Object.DestroyImmediate(parent);
            if (defaults != null) UnityEngine.Object.DestroyImmediate(defaults);
        }
    }
}

[InitializeOnLoad]
public static class RotateObjectWorldPlayCheck
{
    const string Pending = "RotateObjectWorldPlayCheck.pending";
    static RotateObjectWorldPlayCheck() { EditorApplication.playModeStateChanged += StateChanged; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("world rotation test").AddComponent<RotateObjectWorldRunner>().Begin();
    }
}

public class RotateObjectWorldRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Check()); }
    IEnumerator Check()
    {
        yield return null;
        try
        {
            var parent = new GameObject("rotated parent");
            parent.transform.rotation = Quaternion.Euler(35, 40, 25);
            var child = new GameObject("world-rotated object");
            child.transform.SetParent(parent.transform, false);
            var rotate = child.AddComponent<RotateObject>();
            rotate.enabled = false;
            rotate.useMeTime = false; rotate.axis = Vector3.right; rotate.speed = 100f;
            float dt = Time.deltaTime;
            if (dt <= 0f) throw new Exception("No advancing frame time");
            Quaternion before = child.transform.rotation;
            typeof(RotateObject).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rotate, null);
            Quaternion expected = Quaternion.AngleAxis(100f * dt, Vector3.right) * before;
            if (Quaternion.Angle(child.transform.rotation, expected) > .01f) throw new Exception("World-space rotation differs");
            Debug.Log("RotateObjectWorldPlayCheck passed: advancing Unity time and world-space rotation");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
