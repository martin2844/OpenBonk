using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class CameraFadeObjectPortCheck
{
    public static void Run()
    {
        var previousEvent = PlayerCamera.A_CameraFadeObjectEnter;
        float previousTime = MyTime.time;
        GameObject instance = null, other = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Boulder.prefab");
            if (prefab == null) throw new Exception("Original boulder prefab missing");
            instance = UnityEngine.Object.Instantiate(prefab);
            var fade = instance.GetComponent<CameraFadeObject>();
            if (fade == null || fade.renderer == null || fade.fadeMaterial == null || !instance.CompareTag("CameraFade"))
                throw new Exception("Boulder fade wiring missing");
            var original = fade.renderer.sharedMaterial;
            var type = typeof(CameraFadeObject);
            var start = type.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            var update = type.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            start.Invoke(fade, null);
            other = new GameObject("unrelated obstruction");
            MyTime.time = 1f;
            PlayerCamera.A_CameraFadeObjectEnter?.Invoke(other);
            if (fade.renderer.sharedMaterial != original) throw new Exception("Unrelated object faded boulder");
            PlayerCamera.A_CameraFadeObjectEnter?.Invoke(instance);
            if (fade.renderer.sharedMaterial != fade.fadeMaterial) throw new Exception("Boulder did not switch material");
            MyTime.time = 1.07f; update.Invoke(fade, null);
            if (fade.renderer.sharedMaterial != fade.fadeMaterial) throw new Exception("Fade stopped too early");
            PlayerCamera.A_CameraFadeObjectEnter?.Invoke(instance);
            MyTime.time = 1.09f; update.Invoke(fade, null);
            if (fade.renderer.sharedMaterial != fade.fadeMaterial) throw new Exception("Repeated hit did not extend fade");
            MyTime.time = 1.16f; update.Invoke(fade, null);
            if (fade.renderer.sharedMaterial != original) throw new Exception("Original material was not restored");
            type.GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fade, null);
            if (PlayerCamera.A_CameraFadeObjectEnter != previousEvent) throw new Exception("Event handler was not removed");
            Debug.Log("CameraFadeObjectPortCheck passed: actual boulder, event filtering, refresh, restore and cleanup");
        }
        finally
        {
            PlayerCamera.A_CameraFadeObjectEnter = previousEvent;
            MyTime.time = previousTime;
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            if (other != null) UnityEngine.Object.DestroyImmediate(other);
        }
    }
}
